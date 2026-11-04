using Core.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Options
{
    /// <summary>
    /// Uma aba da coluna da esquerda. As abas ficam empilhadas e NÃO pulam nem crescem:
    /// a selecionada só entorta de leve e INVERTE a cor (fundo claro, texto escuro).
    /// Não reage a mouse em cima, só a clique.
    /// </summary>
    public sealed class OptionsTab
    {
        private const float SelectedIndent = 20f;

        private readonly OptionsMenu _menu;
        private RectTransform _root;
        private RectTransform _visual;
        private CanvasGroup _group;
        private Image _background;
        private Image _shadow;
        private TMP_Text _label;
        private TMP_Text _number;
        private MenuSpring _x;
        private MenuSpring _tilt;
        private float _selectT;
        private bool _selected;
        private bool _focused = true;
        private bool _shown;
        private float _delay;
        private float _present;

        public OptionsTab(OptionsMenu menu, Transform parent, int index, string label)
        {
            _menu = menu;
            OptionsTheme theme = menu.Theme;
            OptionsStyle style = menu.Style;

            _root = OptionsUI.Rect("Tab_" + label, parent);
            OptionsUI.Place(_root, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -index * (theme.tabHeight + theme.tabSpacing)),
                new Vector2(theme.tabWidth, theme.tabHeight));

            _visual = OptionsUI.Rect("Visual", _root);
            OptionsUI.Fill(_visual);
            _group = _visual.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            // Sombra colorida que aparece atrás da aba selecionada (cor da seção)
            _shadow = OptionsUI.Image(_visual, "Shadow", menu.Accent);
            OptionsUI.Fill(_shadow.rectTransform);
            _shadow.rectTransform.anchoredPosition = new Vector2(8f, -8f);
            OptionsUI.Skew(_shadow, theme.skew);

            _background = OptionsUI.Image(_visual, "Bg", style.Ink, true);
            OptionsUI.Fill(_background.rectTransform);
            OptionsUI.Skew(_background, theme.skew);
            OptionsPointerRelay relay = _background.gameObject.AddComponent<OptionsPointerRelay>();
            relay.Click = _ => _menu.TabClicked(index);

            _number = OptionsUI.Text(_visual, "Number", (index + 1).ToString("00"), style.Display, style.Size(24),
                style.Muted, TextAlignmentOptions.MidlineLeft);
            OptionsUI.Place(_number.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(38f, 0f), new Vector2(60f, theme.tabHeight));

            _label = OptionsUI.Text(_visual, "Label", style.Caps(label), style.Display, style.Size(36), style.Text,
                TextAlignmentOptions.MidlineLeft);
            OptionsUI.Fill(_label.rectTransform, 108f, 0f, 20f, 0f);

            Apply();
        }

        public void SetState(bool selected, bool focused)
        {
            bool becameSelected = selected && !_selected;
            _selected = selected;
            _focused = focused;

            // A selecionada fica por cima (a sombra colorida não é cortada pela vizinha)
            if (becameSelected) _root.SetAsLastSibling();
        }

        public void SetShown(bool shown, float delay)
        {
            _shown = shown;
            _delay = shown ? Mathf.Max(0f, delay) : 0f;
        }

        public void Snap()
        {
            _shown = true;
            _delay = 0f;
            _present = 1f;
            _selectT = _selected ? (_focused ? 1f : 0.75f) : 0f;
            _x.Snap(_selected ? SelectedIndent : 0f);
            _tilt.Snap(_selected ? _menu.Theme.tabTilt : 0f);
            Apply();
        }

        public void Tick(float dt)
        {
            bool reduce = MenuMotion.ReduceMotion;

            if (_delay > 0f)
            {
                _delay -= dt;
            }
            else
            {
                float target = _shown ? 1f : 0f;
                _present = reduce ? target : Mathf.MoveTowards(_present, target, dt / 0.35f);
            }

            float tiltTarget = _selected ? _menu.Theme.tabTilt : 0f;
            float xTarget = _selected ? SelectedIndent : 0f;
            float selectTarget = _selected ? (_focused ? 1f : 0.75f) : 0f;

            if (reduce)
            {
                _selectT = selectTarget;
                _x.Snap(xTarget);
                _tilt.Snap(tiltTarget);
            }
            else
            {
                _selectT = Mathf.MoveTowards(_selectT, selectTarget, dt * 9f);
                _x.Step(xTarget, 300f, 25f, dt);
                _tilt.Step(tiltTarget, 300f, 25f, dt);
            }

            Apply();
        }

        private void Apply()
        {
            OptionsStyle style = _menu.Style;
            float e = MenuEasing.OutCubic(_present);
            float slide = MenuMotion.ReduceMotion ? 0f : (1f - e) * -140f;

            _group.alpha = e;
            _group.blocksRaycasts = _present > 0.5f;
            _visual.anchoredPosition = new Vector2(_x.Value + slide, 0f);
            _visual.localRotation = Quaternion.Euler(0f, 0f, _tilt.Value);

            float t = Mathf.SmoothStep(0f, 1f, _selectT);

            _background.color = Color.Lerp(style.Ink, style.Paper, t);
            _label.color = Color.Lerp(style.Text, style.TextOnPaper, t);
            _number.color = Color.Lerp(style.Muted, _menu.Accent, t);

            Color shadow = _menu.Accent;
            shadow.a = Mathf.Clamp01(t * 1.4f);
            _shadow.color = shadow;
        }
    }
}
