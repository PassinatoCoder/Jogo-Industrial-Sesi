using System.Collections.Generic;
using Core.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Options
{
    /// <summary>Texto que entra deslizando + fade toda vez que o conteúdo muda.</summary>
    public sealed class AnimatedText
    {
        private readonly RectTransform _rect;
        private readonly Vector2 _basePosition;
        private readonly Vector2 _slide;
        private readonly float _duration;
        private float _progress = 1f;
        private Color _color;

        public TMP_Text Text { get; }

        public AnimatedText(TMP_Text text, Vector2 slide, float duration)
        {
            Text = text;
            _rect = text.rectTransform;
            _basePosition = _rect.anchoredPosition;
            _slide = slide;
            _duration = Mathf.Max(0.01f, duration);
            _color = text.color;
        }

        public void SetColor(Color color)
        {
            _color = color;
            Apply();
        }

        public void Set(string value, bool animate)
        {
            bool changed = Text.text != value;
            Text.text = value;

            if (!changed) return;

            // Se trocar no meio de outra troca, recomeça de um ponto já avançado (sem piscar)
            _progress = animate && !MenuMotion.ReduceMotion ? Mathf.Min(_progress, 0.3f) : 1f;
            Apply();
        }

        public void Tick(float dt)
        {
            if (_progress >= 1f) return;

            _progress = Mathf.Min(1f, _progress + dt / _duration);
            Apply();
        }

        private void Apply()
        {
            float e = MenuEasing.OutCubic(_progress);
            _rect.anchoredPosition = _basePosition + _slide * (1f - e);

            Color c = _color;
            c.a = _color.a * e;
            Text.color = c;
        }
    }

    /// <summary>
    /// Toda a "moldura" do menu de opções: fundo escurecido, coluna e cartão inclinados,
    /// título gigante da seção, número grande, cabeçalho, descrição e dicas de tecla.
    /// Também cria os contêineres onde o OptionsMenu coloca abas e painel.
    /// </summary>
    public sealed class OptionsChrome
    {
        private struct IntroItem
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public Vector2 BasePosition;
            public Vector2 Slide;
        }

        private readonly OptionsMenu _menu;
        private readonly OptionsStyle _style;
        private readonly OptionsTheme _theme;
        private readonly List<IntroItem> _intro = new List<IntroItem>();

        private AnimatedText _giantTitle;
        private AnimatedText _number;
        private AnimatedText _numberShadow;
        private AnimatedText _sectionName;
        private AnimatedText _sectionCount;
        private AnimatedText _description;
        private Image _descriptionStrip;
        private Image _bannerBar;
        private TMP_Text _hints;
        private string _lastHints;

        public RectTransform TabsRoot { get; private set; }
        public RectTransform ViewportRoot { get; private set; }
        public RectTransform ExtrasRoot { get; private set; }
        public RectTransform OverlayRoot { get; private set; }
        public RectTransform BackgroundRoot { get; private set; }

        public OptionsChrome(OptionsMenu menu)
        {
            _menu = menu;
            _style = menu.Style;
            _theme = menu.Theme;
        }

        public void Build(Transform root)
        {
            // 1) Fundos (imagens por seção entram aqui)
            BackgroundRoot = OptionsUI.Rect("Backgrounds", root);
            OptionsUI.Fill(BackgroundRoot);

            Image scrim = OptionsUI.Image(root, "Scrim", new Color(_style.Ink.r, _style.Ink.g, _style.Ink.b, 0.8f), true);
            OptionsUI.Fill(scrim.rectTransform);

            // 2) Coluna da esquerda (inclinada)
            Image column = OptionsUI.Image(root, "LeftColumn", _style.Ink);
            OptionsUI.Place(column.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(-90f, 0f), new Vector2(740f, 1400f));
            OptionsUI.Skew(column, 6f);
            AddIntro(column.rectTransform, new Vector2(-160f, 0f));

            // 3) Cartão do painel (inclinado)
            Image card = OptionsUI.Image(root, "PanelCard", new Color(_style.InkSoft.r, _style.InkSoft.g, _style.InkSoft.b, 0.6f));
            OptionsUI.Fill(card.rectTransform, 690f, 175f, 60f, 215f);
            OptionsUI.Skew(card, 4f);
            AddIntro(card.rectTransform, new Vector2(120f, 0f));

            // 4) Título gigante, bem apagado, atrás do conteúdo
            TMP_Text giant = OptionsUI.Text(root, "GiantTitle", "", _style.Display, _style.Size(300),
                new Color(_style.Paper.r, _style.Paper.g, _style.Paper.b, 0.07f), TextAlignmentOptions.BottomRight);
            OptionsUI.Place(giant.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-70f, 130f), new Vector2(1100f, 360f));
            giant.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            giant.fontStyle = FontStyles.Bold;
            giant.enableAutoSizing = true;
            giant.fontSizeMin = 90f;
            giant.fontSizeMax = _style.Size(300);
            _giantTitle = new AnimatedText(giant, new Vector2(140f, 0f), 0.55f);

            // 5) Cabeçalho "OPÇÕES"
            RectTransform header = OptionsUI.Rect("Header", root);
            OptionsUI.Place(header, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -50f), new Vector2(640f, 170f));
            AddIntro(header, new Vector2(-120f, 0f));

            TMP_Text headerTitle = OptionsUI.Text(header, "Title", _style.Caps(Core.Settings.GameLanguage.T("Opções", "Options")),
                _style.Display, _style.Size(104), _style.Paper, TextAlignmentOptions.TopLeft);
            OptionsUI.Fill(headerTitle.rectTransform);
            headerTitle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
            headerTitle.fontStyle = FontStyles.Bold;

            _bannerBar = OptionsUI.Image(header, "Bar", _menu.Accent);
            OptionsUI.Place(_bannerBar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(4f, 8f), new Vector2(380f, 8f));
            OptionsUI.Skew(_bannerBar, _theme.skew);

            // 6) Número grande + nome da seção (canto superior direito)
            RectTransform banner = OptionsUI.Rect("Banner", root);
            OptionsUI.Place(banner, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-90f, -30f), new Vector2(760f, 220f));
            AddIntro(banner, new Vector2(140f, 0f));

            TMP_Text numberShadow = OptionsUI.Text(banner, "NumberShadow", "01", _style.Display, _style.Size(200), _menu.Accent,
                TextAlignmentOptions.TopRight);
            OptionsUI.Fill(numberShadow.rectTransform, 0f, 0f, 0f, 0f);
            numberShadow.rectTransform.anchoredPosition = new Vector2(8f, -8f);
            numberShadow.fontStyle = FontStyles.Bold;
            _numberShadow = new AnimatedText(numberShadow, new Vector2(0f, 60f), 0.45f);

            TMP_Text number = OptionsUI.Text(banner, "Number", "01", _style.Display, _style.Size(200), _style.Paper,
                TextAlignmentOptions.TopRight);
            OptionsUI.Fill(number.rectTransform);
            number.fontStyle = FontStyles.Bold;
            _number = new AnimatedText(number, new Vector2(0f, 60f), 0.45f);

            TMP_Text name = OptionsUI.Text(banner, "SectionName", "", _style.Display, _style.Size(40), _style.Paper,
                TextAlignmentOptions.BottomRight);
            OptionsUI.Place(name.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-190f, 18f), new Vector2(520f, 60f));
            _sectionName = new AnimatedText(name, new Vector2(60f, 0f), 0.4f);

            TMP_Text count = OptionsUI.Text(banner, "SectionCount", "", _style.Body, _style.Size(22), _style.Muted,
                TextAlignmentOptions.TopRight);
            OptionsUI.Place(count.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-190f, -14f), new Vector2(520f, 36f));
            _sectionCount = new AnimatedText(count, new Vector2(60f, 0f), 0.4f);

            // 7) Contêiner das abas
            TabsRoot = OptionsUI.Rect("Tabs", root);
            OptionsUI.Place(TabsRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -250f), new Vector2(_theme.tabWidth, 700f));

            // 8) Painel (rolagem recortada) e extras
            ViewportRoot = OptionsUI.Rect("Viewport", root);
            OptionsUI.Fill(ViewportRoot, 722f, 195f, 82f, 235f);
            ViewportRoot.gameObject.AddComponent<RectMask2D>();

            ExtrasRoot = OptionsUI.Rect("ExtrasHolder", root);
            OptionsUI.Fill(ExtrasRoot, 722f, 195f, 82f, 235f);

            // 9) Descrição (embaixo à esquerda) e dicas de tecla (embaixo à direita)
            RectTransform descRoot = OptionsUI.Rect("Description", root);
            descRoot.anchorMin = new Vector2(0f, 0f);
            descRoot.anchorMax = new Vector2(1f, 0f);
            descRoot.pivot = new Vector2(0.5f, 0f);
            descRoot.offsetMin = new Vector2(722f, 62f);
            descRoot.offsetMax = new Vector2(-650f, 172f);
            AddIntro(descRoot, new Vector2(0f, -60f));

            _descriptionStrip = OptionsUI.Image(descRoot, "Strip", _menu.Accent);
            OptionsUI.Place(_descriptionStrip.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(6f, 70f));

            TMP_Text desc = OptionsUI.Text(descRoot, "Text", "", _style.Body, _style.Size(28), _style.Paper,
                TextAlignmentOptions.MidlineLeft, true);
            OptionsUI.Fill(desc.rectTransform, 22f, 0f, 0f, 0f);
            _description = new AnimatedText(desc, new Vector2(24f, 0f), 0.3f);

            RectTransform hintsRoot = OptionsUI.Rect("Hints", root);
            OptionsUI.Place(hintsRoot, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-70f, 62f), new Vector2(560f, 110f));
            AddIntro(hintsRoot, new Vector2(0f, -60f));

            _hints = OptionsUI.Text(hintsRoot, "Text", "", _style.Body, _style.Size(22), _style.Muted,
                TextAlignmentOptions.MidlineRight, true);
            OptionsUI.Fill(_hints.rectTransform);

            // 10) Camada de cima (modal)
            OverlayRoot = OptionsUI.Rect("Overlay", root);
            OptionsUI.Fill(OverlayRoot);
        }

        // ---------- Entrada ----------

        private void AddIntro(RectTransform rect, Vector2 slide)
        {
            CanvasGroup group = rect.gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = rect.gameObject.AddComponent<CanvasGroup>();

            _intro.Add(new IntroItem
            {
                Rect = rect,
                Group = group,
                BasePosition = rect.anchoredPosition,
                Slide = slide
            });
        }

        /// <summary>0 = tudo fora da tela, 1 = no lugar. Usa curva suave por fora.</summary>
        public void SetIntro(float progress)
        {
            float e = MenuEasing.OutCubic(Mathf.Clamp01(progress));
            bool reduce = MenuMotion.ReduceMotion;

            foreach (IntroItem item in _intro)
            {
                item.Group.alpha = e;
                item.Rect.anchoredPosition = item.BasePosition + (reduce ? Vector2.zero : item.Slide * (1f - e));
            }
        }

        // ---------- Conteúdo ----------

        public void SetSection(int index, int count, string title, Color accent, bool animate)
        {
            _giantTitle.Set(_style.Caps(title), animate);
            _number.Set((index + 1).ToString("00"), animate);
            _numberShadow.Set((index + 1).ToString("00"), animate);
            _sectionName.Set(_style.Caps(title), animate);
            _sectionCount.Set(Core.Settings.GameLanguage.T("Seção ", "Section ") + (index + 1) + " / " + count, animate);
        }

        public void SetDescription(string text, bool animate) => _description.Set(text ?? "", animate);

        public void SetHints(string text)
        {
            if (text == _lastHints) return;
            _lastHints = text;
            _hints.text = text;
        }

        public void Tick(float dt)
        {
            _giantTitle.Tick(dt);
            _number.Tick(dt);
            _numberShadow.Tick(dt);
            _sectionName.Tick(dt);
            _sectionCount.Tick(dt);
            _description.Tick(dt);

            Color accent = _menu.Accent;
            _numberShadow.SetColor(accent);
            _descriptionStrip.color = accent;
            _bannerBar.color = accent;
        }
    }
}
