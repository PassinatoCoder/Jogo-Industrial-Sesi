using System;
using UnityEngine;

namespace Core.Settings
{
    /// <summary>Tipos de daltonismo que o filtro de cores sabe corrigir.</summary>
    public enum ColorBlindMode
    {
        Off = 0,
        Protanopia = 1,     // sem vermelho
        Protanomaly = 2,    // pouco vermelho
        Deuteranopia = 3,   // sem verde
        Deuteranomaly = 4,  // pouco verde
        Tritanopia = 5,     // sem azul
        Tritanomaly = 6,    // pouco azul
        Achromatopsia = 7   // sem cores (monocromia)
    }

    /// <summary>
    /// Opções de acessibilidade salvas.
    ///
    /// JÁ FUNCIONAM SOZINHAS no jogo todo (scripts da pasta Accessibility, sem configurar cena):
    ///   TextSizeIndex, TextSpacingIndex, BoldText   -> todo TextMeshPro do jogo (GameAccessibility)
    ///   Leitura fácil (MenuReadability)             -> fonte para dislexia + espaçamento (GameAccessibility)
    ///   ColorBlind + ColorBlindStrength + HighContrast -> filtro de tela (ColorBlindFilter + shader)
    ///
    /// O SEU CÓDIGO PRECISA CHAMAR (só estes dois, o resto é automático):
    ///   AccessibilitySettings.ShakeIntensity(valor)  -> use no tremor de câmera
    ///   AccessibilitySettings.FlashIntensity(valor)  -> use em flashes / clarões / piscadas
    /// </summary>
    public static class AccessibilitySettings
    {
        private const string Prefix = "opt_acc_";

        private static readonly float[] TextScales = { 1f, 1.25f, 1.5f, 1.75f };
        private const float MenuMaxScale = 1.4f; // o menu tem linhas de altura fixa, então não passa disso

        // Espaçamento (unidades do TextMeshPro): letras, palavras, linhas
        private static readonly float[] CharSpacings = { 0f, 6f, 12f };
        private static readonly float[] WordSpacings = { 0f, 10f, 20f };
        private static readonly float[] LineSpacings = { 0f, 15f, 30f };

        private const float FlashFactorWhenReduced = 0.25f;
        private const float HighContrastWorld = 1.2f;

        private static bool _loaded;
        private static int _textSizeIndex;
        private static int _spacingIndex;
        private static bool _boldText;
        private static bool _highContrast;
        private static float _cameraShake = 1f;
        private static bool _reduceFlashes;
        private static ColorBlindMode _colorBlind = ColorBlindMode.Off;
        private static float _colorBlindStrength = 1f;
        private static bool _colorBlindPreview;

        public static event Action Changed;

        // ---------- Texto ----------

        public static int TextSizeCount => TextScales.Length;
        public static int SpacingCount => CharSpacings.Length;

        /// <summary>0 = Normal, 1 = Grande, 2 = Muito grande, 3 = Enorme.</summary>
        public static int TextSizeIndex
        {
            get { Load(); return _textSizeIndex; }
            set
            {
                Load();
                value = Mathf.Clamp(value, 0, TextScales.Length - 1);
                if (value == _textSizeIndex) return;
                _textSizeIndex = value;
                Save();
            }
        }

        /// <summary>Multiplicador de tamanho dos textos DO JOGO (1 a 1,75).</summary>
        public static float TextScale => TextScales[TextSizeIndex];

        /// <summary>Multiplicador dos textos do MENU (limitado, pra não quebrar o layout).</summary>
        public static float MenuTextScale => Mathf.Min(TextScale, MenuMaxScale);

        /// <summary>0 = Normal, 1 = Médio, 2 = Grande (letras, palavras e linhas mais afastadas).</summary>
        public static int TextSpacingIndex
        {
            get { Load(); return _spacingIndex; }
            set
            {
                Load();
                value = Mathf.Clamp(value, 0, CharSpacings.Length - 1);
                if (value == _spacingIndex) return;
                _spacingIndex = value;
                Save();
            }
        }

        public static bool BoldText
        {
            get { Load(); return _boldText; }
            set
            {
                Load();
                if (value == _boldText) return;
                _boldText = value;
                Save();
            }
        }

        /// <summary>Índice de espaçamento realmente usado: a leitura fácil (dislexia) nunca fica abaixo de "Médio".</summary>
        public static int SpacingFor(bool easyReading) => Mathf.Max(TextSpacingIndex, easyReading ? 1 : 0);

        public static float CharacterSpacing(int index) => CharSpacings[Mathf.Clamp(index, 0, CharSpacings.Length - 1)];
        public static float WordSpacing(int index) => WordSpacings[Mathf.Clamp(index, 0, WordSpacings.Length - 1)];
        public static float LineSpacing(int index) => LineSpacings[Mathf.Clamp(index, 0, LineSpacings.Length - 1)];

        // ---------- Cores ----------

        public static bool HighContrast
        {
            get { Load(); return _highContrast; }
            set
            {
                Load();
                if (value == _highContrast) return;
                _highContrast = value;
                Save();
            }
        }

        /// <summary>Contraste aplicado na imagem do jogo (1 = normal).</summary>
        public static float WorldContrast => HighContrast ? HighContrastWorld : 1f;

        public static ColorBlindMode ColorBlind
        {
            get { Load(); return _colorBlind; }
            set
            {
                Load();
                if (value == _colorBlind) return;
                _colorBlind = value;
                Save();
            }
        }

        /// <summary>Força do filtro de daltonismo (0 a 1).</summary>
        public static float ColorBlindStrength
        {
            get { Load(); return _colorBlindStrength; }
            set
            {
                Load();
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, _colorBlindStrength)) return;
                _colorBlindStrength = value;
                Save();
            }
        }

        /// <summary>
        /// SÓ PARA TESTE (não é salvo): em vez de corrigir, mostra como um daltônico enxerga.
        /// Serve pra você, que enxerga as cores normalmente, conferir se o jogo ficou legível.
        /// </summary>
        public static bool ColorBlindPreview
        {
            get { return _colorBlindPreview; }
            set
            {
                if (value == _colorBlindPreview) return;
                _colorBlindPreview = value;
                Changed?.Invoke();
            }
        }

        // ---------- Movimento e efeitos ----------

        /// <summary>Força do tremor de câmera (0 a 1).</summary>
        public static float CameraShake
        {
            get { Load(); return _cameraShake; }
            set
            {
                Load();
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, _cameraShake)) return;
                _cameraShake = value;
                Save();
            }
        }

        public static bool ReduceFlashes
        {
            get { Load(); return _reduceFlashes; }
            set
            {
                Load();
                if (value == _reduceFlashes) return;
                _reduceFlashes = value;
                Save();
            }
        }

        /// <summary>Use no tremor de câmera:  força = AccessibilitySettings.ShakeIntensity(forçaOriginal).</summary>
        public static float ShakeIntensity(float value) => value * CameraShake;

        /// <summary>Use em flashes e clarões:  alpha = AccessibilitySettings.FlashIntensity(alphaOriginal).</summary>
        public static float FlashIntensity(float value) => ReduceFlashes ? value * FlashFactorWhenReduced : value;

        // ---------- Geral ----------

        public static void ResetToDefaults()
        {
            Load();
            _textSizeIndex = 0;
            _spacingIndex = 0;
            _boldText = false;
            _highContrast = false;
            _cameraShake = 1f;
            _reduceFlashes = false;
            _colorBlind = ColorBlindMode.Off;
            _colorBlindStrength = 1f;
            _colorBlindPreview = false;
            Save();
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            _textSizeIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "text", 0), 0, TextScales.Length - 1);
            _spacingIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "spacing", 0), 0, CharSpacings.Length - 1);
            _boldText = PlayerPrefs.GetInt(Prefix + "bold", 0) == 1;
            _highContrast = PlayerPrefs.GetInt(Prefix + "contrast", 0) == 1;
            _cameraShake = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "shake", 1f));
            _reduceFlashes = PlayerPrefs.GetInt(Prefix + "flashes", 0) == 1;
            _colorBlind = (ColorBlindMode)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "colorblind", 0), 0, (int)ColorBlindMode.Achromatopsia);
            _colorBlindStrength = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "cbstrength", 1f));
        }

        private static void Save()
        {
            PlayerPrefs.SetInt(Prefix + "text", _textSizeIndex);
            PlayerPrefs.SetInt(Prefix + "spacing", _spacingIndex);
            PlayerPrefs.SetInt(Prefix + "bold", _boldText ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "contrast", _highContrast ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "shake", _cameraShake);
            PlayerPrefs.SetInt(Prefix + "flashes", _reduceFlashes ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "colorblind", (int)_colorBlind);
            PlayerPrefs.SetFloat(Prefix + "cbstrength", _colorBlindStrength);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = false;
            _colorBlindPreview = false;
            Changed = null;
        }
    }
}
