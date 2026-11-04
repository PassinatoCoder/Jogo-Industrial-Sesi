using Core.Settings;
using Core.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Options
{
    /// <summary>
    /// Cores, fontes e tamanhos já "resolvidos" levando em conta a acessibilidade
    /// (tamanho do texto, alto contraste, leitura fácil). Recriado quando essas opções mudam.
    /// </summary>
    public sealed class OptionsStyle
    {
        public TMP_FontAsset Display { get; private set; }
        public TMP_FontAsset Body { get; private set; }
        public Color Ink { get; private set; }
        public Color InkSoft { get; private set; }
        public Color Paper { get; private set; }
        public Color Muted { get; private set; }
        public Color Text { get; private set; }
        public Color TextOnPaper { get; private set; }
        public float Scale { get; private set; } = 1f;
        public bool HighContrast { get; private set; }
        public bool Readable { get; private set; }

        public static OptionsStyle Create(OptionsTheme theme)
        {
            OptionsStyle s = new OptionsStyle
            {
                Scale = AccessibilitySettings.MenuTextScale,
                HighContrast = AccessibilitySettings.HighContrast,
                Readable = MenuReadability.ReadableTitles
            };

            s.Body = s.Readable && theme.readableFont != null ? theme.readableFont : theme.bodyFont;
            s.Display = s.Readable ? s.Body : (theme.displayFont != null ? theme.displayFont : s.Body);

            if (s.HighContrast)
            {
                s.Ink = new Color(0f, 0f, 0f, 1f);
                s.InkSoft = new Color(0.04f, 0.04f, 0.04f, 1f);
                s.Paper = Color.white;
                s.Muted = new Color(0.88f, 0.88f, 0.88f, 1f);
                s.TextOnPaper = Color.black;
            }
            else
            {
                s.Ink = theme.ink;
                s.InkSoft = theme.inkSoft;
                s.Paper = theme.paper;
                s.Muted = theme.muted;
                s.TextOnPaper = new Color(0.06f, 0.065f, 0.08f, 1f);
            }

            s.Text = s.Paper;
            return s;
        }

        /// <summary>Tamanho de fonte já multiplicado pelo "Tamanho do texto" da acessibilidade.</summary>
        public float Size(float basePixels) => Mathf.Round(basePixels * Scale);

        /// <summary>Títulos em MAIÚSCULAS, exceto no modo leitura fácil (maiúsculas cansam quem tem dislexia).</summary>
        public string Caps(string text)
        {
            return Readable || string.IsNullOrEmpty(text) ? text : text.ToUpperInvariant();
        }

        /// <summary>Cor de texto que contrasta com o fundo dado.</summary>
        public Color OnColor(Color background)
        {
            float luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            return luminance > 0.55f ? TextOnPaper : Paper;
        }
    }

    /// <summary>Ferramentas pra criar elementos de UI por código.</summary>
    public static class OptionsUI
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Estica o objeto pra preencher o pai (margens em pixels).</summary>
        public static void Fill(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Posiciona com âncora fixa (não estica).</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static Image Image(Transform parent, string name, Color color, bool raycast = false)
        {
            RectTransform rt = Rect(name, parent);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>Inclina o retângulo (efeito de faixa de corrida).</summary>
        public static void Skew(Image image, float degrees)
        {
            UISkew skew = image.gameObject.AddComponent<UISkew>();
            skew.SkewDegrees = degrees;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, TMP_FontAsset font,
            float size, Color color, TextAlignmentOptions alignment, bool wrap = false)
        {
            RectTransform rt = Rect(name, parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = wrap ? TextOverflowModes.Truncate : TextOverflowModes.Ellipsis;

            // Espaçamento e negrito da acessibilidade (a leitura fácil nunca fica abaixo de "Médio")
            int spacing = AccessibilitySettings.SpacingFor(MenuReadability.ReadableTitles);
            tmp.characterSpacing = AccessibilitySettings.CharacterSpacing(spacing);
            tmp.wordSpacing = AccessibilitySettings.WordSpacing(spacing);
            tmp.lineSpacing = AccessibilitySettings.LineSpacing(spacing);
            if (AccessibilitySettings.BoldText) tmp.fontStyle = FontStyles.Bold;

            return tmp;
        }

        public static void SetAlpha(Graphic graphic, float alpha)
        {
            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }
}