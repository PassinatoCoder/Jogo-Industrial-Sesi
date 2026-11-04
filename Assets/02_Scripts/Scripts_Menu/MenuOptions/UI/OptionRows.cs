using System;
using System.Collections.Generic;
using Core.Settings;
using Core.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UI.Options
{
    // ======================================================================
    //  LIGA / DESLIGA
    // ======================================================================
    public sealed class ToggleRow : OptionRow
    {
        private readonly Func<bool> _get;
        private readonly Action<bool> _set;
        private Image _pill;
        private TMP_Text _pillText;

        public ToggleRow(OptionsMenu menu, string label, string description, Func<bool> get, Action<bool> set)
            : base(menu, label, menu.RowHeight)
        {
            Description = description;
            _get = get;
            _set = set;
        }

        protected override void BuildControls(RectTransform visual)
        {
            _pill = OptionsUI.Image(visual, "Pill", Style.Muted);
            OptionsUI.Place(_pill.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-30f, 0f), new Vector2(Style.Size(190), Style.Size(40)));
            OptionsUI.Skew(_pill, Menu.Theme.skew);

            _pillText = OptionsUI.Text(_pill.rectTransform, "Text", "", Style.Display, Style.Size(22), Style.Paper,
                TextAlignmentOptions.Center);
            OptionsUI.Fill(_pillText.rectTransform);
        }

        public override void Refresh()
        {
            if (_pillText == null) return;
            _pillText.text = Style.Caps(_get() ? GameLanguage.T("Ligado", "On") : GameLanguage.T("Desligado", "Off"));
        }

        public override void Activate()
        {
            _set(!_get());
            Refresh();
            Menu.PlayConfirm();
        }

        public override void Adjust(int direction)
        {
            bool wanted = direction > 0;
            if (_get() == wanted) return;

            _set(wanted);
            Refresh();
            Menu.PlayTick();
        }

        protected override void OnVisualUpdate(float focus, bool enabled, float dt)
        {
            if (_pill == null) return;

            bool on = _get();
            Color off = Color.Lerp(Style.Muted, Style.Ink, 0.45f);
            Color color = on ? Menu.Accent : off;
            if (!enabled) color.a *= 0.4f;

            _pill.color = color;
            Color text = Style.OnColor(color);
            if (!enabled) text.a *= 0.5f;
            _pillText.color = text;
        }
    }

    // ======================================================================
    //  ESCOLHA  <  valor  >
    // ======================================================================
    public sealed class ChoiceRow : OptionRow
    {
        private readonly Func<IReadOnlyList<string>> _options;
        private readonly Func<int> _get;
        private readonly Action<int> _set;
        private TMP_Text _value;
        private TMP_Text _left;
        private TMP_Text _right;
        private MenuSpring _nudge;

        public ChoiceRow(OptionsMenu menu, string label, string description,
            Func<IReadOnlyList<string>> options, Func<int> get, Action<int> set)
            : base(menu, label, menu.RowHeight)
        {
            Description = description;
            _options = options;
            _get = get;
            _set = set;
        }

        protected override void BuildControls(RectTransform visual)
        {
            float width = Style.Size(340);

            RectTransform control = OptionsUI.Rect("Control", visual);
            OptionsUI.Place(control, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-24f, 0f), new Vector2(width, Height));

            _left = BuildArrow(control, "<", true);
            _right = BuildArrow(control, ">", false);

            _value = OptionsUI.Text(control, "Value", "", Style.Display, Style.Size(28), Style.Text,
                TextAlignmentOptions.Center);
            OptionsUI.Fill(_value.rectTransform, 50f, 0f, 50f, 0f);
        }

        private TMP_Text BuildArrow(RectTransform control, string glyph, bool left)
        {
            // Área clicável transparente + o símbolo
            Image hit = OptionsUI.Image(control, left ? "ArrowLeft" : "ArrowRight", new Color(1f, 1f, 1f, 0.004f), true);
            RectTransform rt = hit.rectTransform;
            rt.anchorMin = new Vector2(left ? 0f : 1f, 0f);
            rt.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rt.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(56f, 0f);

            int direction = left ? -1 : 1;
            OptionsPointerRelay relay = hit.gameObject.AddComponent<OptionsPointerRelay>();
            relay.Click = _ => Menu.RowAdjustClicked(this, direction);

            TMP_Text text = OptionsUI.Text(rt, "Glyph", glyph, Style.Display, Style.Size(34), Style.Muted,
                TextAlignmentOptions.Center);
            OptionsUI.Fill(text.rectTransform);
            return text;
        }

        public override void Refresh()
        {
            if (_value == null) return;

            IReadOnlyList<string> options = _options();
            if (options == null || options.Count == 0)
            {
                _value.text = "-";
                return;
            }

            _value.text = options[Mathf.Clamp(_get(), 0, options.Count - 1)];
        }

        public override void Adjust(int direction)
        {
            IReadOnlyList<string> options = _options();
            if (options == null || options.Count == 0) return;

            int index = (Mathf.Clamp(_get(), 0, options.Count - 1) + direction + options.Count) % options.Count;
            _set(index);
            Refresh();
            _nudge.Velocity += direction * 240f;
            Menu.PlayTick();
        }

        public override void Activate() => Adjust(1);

        protected override void OnVisualUpdate(float focus, bool enabled, float dt)
        {
            if (_value == null) return;

            if (dt > 0f)
            {
                if (MenuMotion.ReduceMotion) _nudge.Snap(0f);
                else _nudge.Step(0f, 420f, 24f, dt);
            }

            _value.rectTransform.anchoredPosition = new Vector2(_nudge.Value, 0f);

            Color text = Color.Lerp(Style.Text, Style.TextOnPaper, focus);
            Color arrow = Color.Lerp(Style.Muted, Menu.Accent, focus);
            if (!enabled)
            {
                text.a *= 0.4f;
                arrow.a *= 0.4f;
            }

            _value.color = text;
            _left.color = arrow;
            _right.color = arrow;
        }
    }

    // ======================================================================
    //  SLIDER
    // ======================================================================
    public sealed class SliderRow : OptionRow
    {
        private readonly Func<float> _get;
        private readonly Action<float> _set;
        private readonly float _step;
        private readonly Func<float, string> _format;
        private Image _track;
        private Image _fill;
        private Image _thumb;
        private TMP_Text _value;
        private float _shown01 = -1f;

        public SliderRow(OptionsMenu menu, string label, string description,
            Func<float> get, Action<float> set, float step, Func<float, string> format)
            : base(menu, label, menu.RowHeight)
        {
            Description = description;
            _get = get;
            _set = set;
            _step = Mathf.Max(0.01f, step);
            _format = format;
        }

        public override bool ActivatesOnClick => false;

        protected override void BuildControls(RectTransform visual)
        {
            float valueWidth = Style.Size(110);

            _value = OptionsUI.Text(visual, "Value", "", Style.Display, Style.Size(28), Style.Text,
                TextAlignmentOptions.MidlineRight);
            OptionsUI.Place(_value.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-30f, 0f), new Vector2(valueWidth, Height));

            // Área de toque (maior que a barra, fica mais fácil de clicar)
            Image hit = OptionsUI.Image(visual, "SliderHit", new Color(1f, 1f, 1f, 0.004f), true);
            OptionsUI.Place(hit.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-30f - valueWidth - 14f, 0f), new Vector2(Style.Size(300), 48f));

            OptionsPointerRelay relay = hit.gameObject.AddComponent<OptionsPointerRelay>();
            relay.Down = e =>
            {
                Menu.RowFocusFromMouse(this);
                SetFromPointer(e);
            };
            relay.Drag = SetFromPointer;

            _track = OptionsUI.Image(hit.rectTransform, "Track", Style.Muted);
            _track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            _track.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _track.rectTransform.anchoredPosition = Vector2.zero;
            _track.rectTransform.sizeDelta = new Vector2(0f, 12f);
            OptionsUI.Skew(_track, Menu.Theme.skew);

            _fill = OptionsUI.Image(_track.rectTransform, "Fill", Menu.Accent);
            _fill.rectTransform.anchorMin = Vector2.zero;
            _fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _fill.rectTransform.offsetMin = Vector2.zero;
            _fill.rectTransform.offsetMax = Vector2.zero;

            _thumb = OptionsUI.Image(hit.rectTransform, "Thumb", Style.Paper);
            _thumb.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _thumb.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            _thumb.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _thumb.rectTransform.sizeDelta = new Vector2(14f, 32f);
            OptionsUI.Skew(_thumb, Menu.Theme.skew);
        }

        private void SetFromPointer(PointerEventData e)
        {
            if (!IsEnabled || _track == null) return;

            RectTransform rt = _track.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out Vector2 local))
                return;

            Rect rect = rt.rect;
            float t = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
            t = Mathf.Clamp01(Mathf.Round(t / _step) * _step);

            if (Mathf.Approximately(t, _get())) return;

            _set(t);
            Refresh();
            Menu.PlayTick();
        }

        public override void Refresh()
        {
            if (_value == null) return;
            _value.text = _format != null ? _format(_get()) : Mathf.RoundToInt(_get() * 100f) + "%";
        }

        public override void Adjust(int direction)
        {
            float current = Mathf.Round(_get() / _step) * _step;
            float next = Mathf.Clamp01(current + direction * _step);
            if (Mathf.Approximately(next, _get())) return;

            _set(next);
            Refresh();
            Menu.PlayTick();
        }

        protected override void OnVisualUpdate(float focus, bool enabled, float dt)
        {
            if (_track == null) return;

            float target = Mathf.Clamp01(_get());
            if (_shown01 < 0f || MenuMotion.ReduceMotion) _shown01 = target;
            else if (dt > 0f) _shown01 = Mathf.Lerp(_shown01, target, 1f - Mathf.Exp(-20f * dt));

            _fill.rectTransform.anchorMax = new Vector2(_shown01, 1f);
            _thumb.rectTransform.anchorMin = new Vector2(_shown01, 0.5f);
            _thumb.rectTransform.anchorMax = new Vector2(_shown01, 0.5f);
            _thumb.rectTransform.anchoredPosition = Vector2.zero;

            Color track = Color.Lerp(Style.Muted * 0.55f, Style.Ink, focus * 0.35f);
            track.a = Mathf.Lerp(0.5f, 0.35f, focus);
            Color fill = Menu.Accent;
            Color thumb = Color.Lerp(Style.Paper, Style.TextOnPaper, focus);
            Color text = Color.Lerp(Style.Text, Style.TextOnPaper, focus);

            if (!enabled)
            {
                track.a *= 0.4f;
                fill.a *= 0.4f;
                thumb.a *= 0.4f;
                text.a *= 0.4f;
            }

            _track.color = track;
            _fill.color = fill;
            _thumb.color = thumb;
            _value.color = text;
        }
    }

    // ======================================================================
    //  BOTÃO
    // ======================================================================
    public sealed class ButtonRow : OptionRow
    {
        private readonly Action _action;
        private readonly Func<string> _rightText;
        private readonly bool _emphasis;
        private TMP_Text _right;
        private string _lastRight;

        public ButtonRow(OptionsMenu menu, string label, string description, Action action,
            Func<string> rightText = null, bool emphasis = false)
            : base(menu, label, menu.RowHeight)
        {
            Description = description;
            _action = action;
            _rightText = rightText;
            _emphasis = emphasis;
        }

        public override bool HandlesHorizontal => false;

        protected override void BuildControls(RectTransform visual)
        {
            _right = OptionsUI.Text(visual, "Right", ">", Style.Display, Style.Size(28), Style.Muted,
                TextAlignmentOptions.MidlineRight);
            OptionsUI.Place(_right.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-30f, 0f), new Vector2(Style.Size(260), Height));
        }

        public override void Activate()
        {
            _action?.Invoke();
            Menu.PlayConfirm();
        }

        protected override void OnVisualUpdate(float focus, bool enabled, float dt)
        {
            if (_right == null) return;

            string text = _rightText != null ? _rightText() : ">";
            if (string.IsNullOrEmpty(text)) text = _rightText != null ? "" : ">";
            if (text != _lastRight)
            {
                _lastRight = text;
                _right.text = Style.Caps(text);
            }

            Color color = Color.Lerp(_emphasis ? Menu.Accent : Style.Muted, Menu.Accent, focus);
            if (!enabled) color.a *= 0.4f;
            _right.color = color;
        }
    }

    // ======================================================================
    //  TECLA (remapear)
    // ======================================================================
    public sealed class KeybindRow : OptionRow
    {
        private static KeyCode[] _candidates;

        private readonly GameAction _action;
        private bool _capturing;
        private Image _pill;
        private TMP_Text _pillText;

        public KeybindRow(OptionsMenu menu, GameAction action, string description)
            : base(menu, GameKeyBindings.GetLabel(action), menu.RowHeight)
        {
            _action = action;
            Description = description;
        }

        public override bool ConsumesInput => _capturing;
        public override bool HandlesHorizontal => false;

        protected override void BuildControls(RectTransform visual)
        {
            _pill = OptionsUI.Image(visual, "Pill", Style.Muted);
            OptionsUI.Place(_pill.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-30f, 0f), new Vector2(Style.Size(230), Style.Size(40)));
            OptionsUI.Skew(_pill, Menu.Theme.skew);

            _pillText = OptionsUI.Text(_pill.rectTransform, "Text", "", Style.Display, Style.Size(22), Style.Paper,
                TextAlignmentOptions.Center);
            OptionsUI.Fill(_pillText.rectTransform);
        }

        public override void Refresh()
        {
            if (_pillText == null) return;
            _pillText.text = _capturing
                ? GameLanguage.T("Aperte uma tecla...", "Press a key...")
                : GameKeyBindings.DisplayName(GameKeyBindings.Get(_action));
        }

        public override void Activate()
        {
            _capturing = true;
            Refresh();
            Menu.PlayConfirm();
        }

        public override void CancelInput()
        {
            if (!_capturing) return;
            _capturing = false;
            Refresh();
        }

        public override void UpdateInput()
        {
            if (!_capturing) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelInput();
                Menu.PlayConfirm();
                return;
            }

            foreach (KeyCode key in Candidates())
            {
                if (!Input.GetKeyDown(key)) continue;

                if (GameKeyBindings.Set(_action, key))
                {
                    _capturing = false;
                    Menu.RefreshCurrentSection(); // outra ação pode ter trocado de tecla junto
                    Menu.PlayConfirm();
                }
                return;
            }
        }

        private static KeyCode[] Candidates()
        {
            if (_candidates != null) return _candidates;

            List<KeyCode> list = new List<KeyCode>();
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                string name = key.ToString();
                if (key == KeyCode.None || key == KeyCode.Escape) continue;
                if (name.StartsWith("Mouse") || name.StartsWith("Joystick")) continue;
                if (!list.Contains(key)) list.Add(key);
            }

            _candidates = list.ToArray();
            return _candidates;
        }

        protected override void OnVisualUpdate(float focus, bool enabled, float dt)
        {
            if (_pill == null) return;

            Color color = _capturing
                ? Color.Lerp(Menu.Accent, Style.Paper, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f))
                : Color.Lerp(Style.Muted, Style.Ink, 0.45f);
            if (!enabled) color.a *= 0.4f;

            _pill.color = color;
            _pillText.color = Style.OnColor(color);
        }
    }

    // ======================================================================
    //  TÍTULO DE GRUPO (não selecionável)
    // ======================================================================
    public sealed class HeaderRow : OptionRow
    {
        private TMP_Text _text;
        private Image _line;

        public HeaderRow(OptionsMenu menu, string text)
            : base(menu, text, Mathf.Round(menu.RowHeight * 0.82f), false) { }

        protected override bool UsesStandardLabel => false;

        protected override void BuildControls(RectTransform visual)
        {
            _text = OptionsUI.Text(visual, "Header", Style.Caps(LabelText), Style.Display, Style.Size(24), Menu.Accent,
                TextAlignmentOptions.BottomLeft);
            OptionsUI.Fill(_text.rectTransform, 10f, 10f, 0f, 0f);

            _line = OptionsUI.Image(visual, "Line", Menu.Accent);
            _line.rectTransform.anchorMin = new Vector2(0f, 0f);
            _line.rectTransform.anchorMax = new Vector2(1f, 0f);
            _line.rectTransform.pivot = new Vector2(0.5f, 0f);
            _line.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            _line.rectTransform.sizeDelta = new Vector2(0f, 3f);
        }

        protected override void OnVisualUpdate(float focus, bool enabled, float dt)
        {
            if (_text == null) return;
            _text.color = Menu.Accent;
            _line.color = new Color(Menu.Accent.r, Menu.Accent.g, Menu.Accent.b, 0.55f);
        }
    }

    // ======================================================================
    //  TEXTO EXPLICATIVO (não selecionável)
    // ======================================================================
    public sealed class InfoRow : OptionRow
    {
        private TMP_Text _text;

        public InfoRow(OptionsMenu menu, string text, float height)
            : base(menu, text, height, false) { }

        protected override bool UsesStandardLabel => false;

        protected override void BuildControls(RectTransform visual)
        {
            _text = OptionsUI.Text(visual, "Info", LabelText, Style.Body, Style.Size(24), Style.Muted,
                TextAlignmentOptions.TopLeft, true);
            OptionsUI.Fill(_text.rectTransform, 12f, 0f, 12f, 6f);
        }

        protected override void OnVisualUpdate(float focus, bool enabled, float dt)
        {
            if (_text != null) _text.color = Style.Muted;
        }
    }
}
