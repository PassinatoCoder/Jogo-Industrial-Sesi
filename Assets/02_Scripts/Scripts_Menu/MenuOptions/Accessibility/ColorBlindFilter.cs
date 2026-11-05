using Core.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Core.Accessibility
{
    /// <summary>
    /// Filtro de daltonismo SEM shader e SEM Renderer Feature.
    ///
    /// Como funciona: o código calcula uma "tabela de cores" (LUT 32x32x32: pra cada cor de entrada, qual cor sai)
    /// e entrega pro efeito "Color Lookup" do pós-processamento do URP, dentro de um Volume global criado sozinho.
    /// Por isso funciona com o Renderer 2D, e a câmera só precisa estar com "Post Processing" ligado.
    ///
    /// A conta: simula como a pessoa enxerga (matrizes de Machado et al., 2009) e leva a diferença que ela NÃO percebe
    /// para os canais de cor que ela percebe (técnica "daltonização").
    /// "Anomalia" (pouco vermelho / verde / azul) usa a mesma matriz com severidade menor (aproximação).
    /// O "Alto contraste" entra na mesma tabela (mais contraste na imagem do jogo).
    ///
    /// Não vai em objeto nenhum: o GameAccessibility chama Apply() sozinho quando algo muda.
    /// </summary>
    public static class ColorBlindFilter
    {
        private const int LutSize = 32;
        private const float AnomalySeverity = 0.6f;

        // Matrizes 3x3 (linha por linha) de severidade 1,0 — Machado, Oliveira e Fernandes (2009)
        private static readonly float[] Protan =
        {
             0.152286f,  1.052583f, -0.204868f,
             0.114503f,  0.786281f,  0.099216f,
            -0.003882f, -0.048116f,  1.051998f
        };

        private static readonly float[] Deutan =
        {
             0.367322f,  0.860646f, -0.227968f,
             0.280085f,  0.672501f,  0.047413f,
            -0.011820f,  0.042940f,  0.968881f
        };

        private static readonly float[] Tritan =
        {
             1.255528f, -0.076749f, -0.178779f,
            -0.078411f,  0.930809f,  0.147602f,
             0.004733f,  0.691367f,  0.303900f
        };

        private static readonly float[] Gray =
        {
            0.2126f, 0.7152f, 0.0722f,
            0.2126f, 0.7152f, 0.0722f,
            0.2126f, 0.7152f, 0.0722f
        };

        private static readonly float[] Identity =
        {
            1f, 0f, 0f,
            0f, 1f, 0f,
            0f, 0f, 1f
        };

        // Onde jogar a informação que se perde (vermelho/verde vai pra verde+azul; azul vai pra vermelho+verde)
        private static readonly float[] ShiftRedGreen =
        {
            0f,    0f, 0f,
            0.7f,  1f, 0f,
            0.7f,  0f, 1f
        };

        private static readonly float[] ShiftBlue =
        {
            1f, 0f, 0.7f,
            0f, 1f, 0.7f,
            0f, 0f, 0f
        };

        private static readonly float[] ShiftNone = new float[9];

        private static GameObject _host;
        private static Volume _volume;
        private static ColorLookup _lookup;
        private static Texture2D _lut;
        private static Color32[] _pixels;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _host = null;
            _volume = null;
            _lookup = null;
            _lut = null;
            _pixels = null;
        }

        public static void Apply()
        {
            ColorBlindMode mode = AccessibilitySettings.ColorBlind;
            float strength = mode == ColorBlindMode.Off ? 0f : AccessibilitySettings.ColorBlindStrength;
            bool preview = AccessibilitySettings.ColorBlindPreview;

            // Acromatopsia não tem cor pra "devolver": o que ajuda é mais contraste de brilho.
            float contrast = AccessibilitySettings.WorldContrast;
            if (mode == ColorBlindMode.Achromatopsia) contrast *= 1f + 0.35f * strength;

            bool active = strength > 0.0001f || Mathf.Abs(contrast - 1f) > 0.001f;

            if (!active)
            {
                if (_volume != null) _volume.weight = 0f; // desligado: não custa nada
                return;
            }

            if (!EnsureVolume()) return;

            BuildLut(mode, strength, preview, contrast);
            _volume.weight = 1f;
        }

        // ---------- Volume + textura ----------

        private static bool EnsureVolume()
        {
            if (_volume != null && _lookup != null && _lut != null) return true;

            if (_host != null) Object.Destroy(_host);

            _host = new GameObject("[ColorBlindVolume]");
            Object.DontDestroyOnLoad(_host);

            _volume = _host.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 1000f;
            _volume.weight = 0f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _lookup = profile.Add<ColorLookup>(true);
            _volume.sharedProfile = profile;

            _lut = new Texture2D(LutSize * LutSize, LutSize, TextureFormat.RGBA32, false, true)
            {
                name = "ColorBlindLUT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _pixels = new Color32[LutSize * LutSize * LutSize];

            _lookup.texture.Override(_lut);
            _lookup.contribution.Override(1f);
            return true;
        }

        private static void BuildLut(ColorBlindMode mode, float strength, bool preview, float contrast)
        {
            float[] full = Identity;
            float[] shift = ShiftNone;
            float severity = 1f;

            switch (mode)
            {
                case ColorBlindMode.Protanopia: full = Protan; shift = ShiftRedGreen; break;
                case ColorBlindMode.Protanomaly: full = Protan; shift = ShiftRedGreen; severity = AnomalySeverity; break;
                case ColorBlindMode.Deuteranopia: full = Deutan; shift = ShiftRedGreen; break;
                case ColorBlindMode.Deuteranomaly: full = Deutan; shift = ShiftRedGreen; severity = AnomalySeverity; break;
                case ColorBlindMode.Tritanopia: full = Tritan; shift = ShiftBlue; break;
                case ColorBlindMode.Tritanomaly: full = Tritan; shift = ShiftBlue; severity = AnomalySeverity; break;
                case ColorBlindMode.Achromatopsia: full = Gray; break;
            }

            float[] sim = new float[9];
            for (int i = 0; i < 9; i++) sim[i] = Mathf.Lerp(Identity[i], full[i], severity);

            bool useContrast = Mathf.Abs(contrast - 1f) > 0.001f;
            float max = LutSize - 1f;

            // Layout da tabela do URP: fatias de azul lado a lado; dentro de cada fatia, x = vermelho, y = verde
            for (int b = 0; b < LutSize; b++)
            {
                for (int g = 0; g < LutSize; g++)
                {
                    for (int r = 0; r < LutSize; r++)
                    {
                        float cr = r / max;
                        float cg = g / max;
                        float cb = b / max;

                        float sr = sim[0] * cr + sim[1] * cg + sim[2] * cb;
                        float sg = sim[3] * cr + sim[4] * cg + sim[5] * cb;
                        float sb = sim[6] * cr + sim[7] * cg + sim[8] * cb;

                        float outR, outG, outB;
                        if (strength <= 0.0001f)
                        {
                            outR = cr; outG = cg; outB = cb;
                        }
                        else if (preview)
                        {
                            // Modo teste: mostra como um daltônico enxerga
                            outR = Mathf.Lerp(cr, sr, strength);
                            outG = Mathf.Lerp(cg, sg, strength);
                            outB = Mathf.Lerp(cb, sb, strength);
                        }
                        else
                        {
                            // Correção: a diferença que ele não enxerga é jogada nos canais que enxerga
                            float lr = cr - sr;
                            float lg = cg - sg;
                            float lb = cb - sb;

                            outR = cr + (shift[0] * lr + shift[1] * lg + shift[2] * lb) * strength;
                            outG = cg + (shift[3] * lr + shift[4] * lg + shift[5] * lb) * strength;
                            outB = cb + (shift[6] * lr + shift[7] * lg + shift[8] * lb) * strength;
                        }

                        if (useContrast)
                        {
                            outR = (outR - 0.5f) * contrast + 0.5f;
                            outG = (outG - 0.5f) * contrast + 0.5f;
                            outB = (outB - 0.5f) * contrast + 0.5f;
                        }

                        int x = b * LutSize + r;
                        _pixels[g * (LutSize * LutSize) + x] = new Color32(ToByte(outR), ToByte(outG), ToByte(outB), 255);
                    }
                }
            }

            _lut.SetPixels32(_pixels);
            _lut.Apply(false);
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
        }
    }
}