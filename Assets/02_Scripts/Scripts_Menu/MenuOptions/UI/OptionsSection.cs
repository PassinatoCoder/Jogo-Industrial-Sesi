using System;
using System.Collections.Generic;
using Core.UI.Menu;
using UnityEngine;

namespace Core.UI.Options
{
    /// <summary>
    /// Uma seção do menu (Vídeo, Áudio...). Guarda suas linhas, rolagem e foco.
    /// Pra criar uma seção nova: herde esta classe e monte as linhas em Build().
    /// </summary>
    public abstract class OptionsSection
    {
        private readonly List<OptionRow> _rows = new List<OptionRow>();
        private float[] _tops = new float[0];
        private float _contentHeight;
        private float _scroll;
        private float _scrollTarget;
        private bool _shown;
        private CanvasGroup _extrasGroup;
        private float _extrasPresence;

        protected OptionsMenu Menu { get; private set; }
        protected RectTransform Scroll { get; private set; }
        protected RectTransform Extras { get; private set; }

        public RectTransform Container { get; private set; }
        public string Title { get; protected set; } = "";
        public string Subtitle { get; protected set; } = "";
        public Color Accent { get; set; }

        /// <summary>Largura das linhas (0 a 1) dentro do painel. 1 = painel inteiro.</summary>
        public float RowsWidth { get; protected set; } = 1f;

        public IReadOnlyList<OptionRow> Rows => _rows;
        public int FocusIndex { get; private set; } = -1;
        public OptionRow FocusedRow => FocusIndex >= 0 && FocusIndex < _rows.Count ? _rows[FocusIndex] : null;
        public bool HasFocusable => FirstFocusable() >= 0;
        public bool IsVisible => Container != null && Container.gameObject.activeSelf;

        protected static string T(string portuguese, string english) => Core.Settings.GameLanguage.T(portuguese, english);

        // ---------- Montagem ----------

        public void Initialize(OptionsMenu menu, Transform viewport, Transform extrasParent)
        {
            Menu = menu;

            Container = OptionsUI.Rect("Section", viewport);
            OptionsUI.Fill(Container);

            Scroll = OptionsUI.Rect("Scroll", Container);
            Scroll.anchorMin = new Vector2(0f, 1f);
            Scroll.anchorMax = new Vector2(1f, 1f);
            Scroll.pivot = new Vector2(0.5f, 1f);
            Scroll.anchoredPosition = Vector2.zero;
            Scroll.sizeDelta = Vector2.zero;

            Extras = OptionsUI.Rect("Extras", extrasParent);
            OptionsUI.Fill(Extras);
            _extrasGroup = Extras.gameObject.AddComponent<CanvasGroup>();
            _extrasGroup.alpha = 0f;

            Build();
            Layout();

            Container.name = "Section_" + Title;
            Extras.name = "Extras_" + Title;
            FocusIndex = FirstFocusable();

            Container.gameObject.SetActive(false);
            Extras.gameObject.SetActive(false);
        }

        /// <summary>Crie as linhas aqui com AddToggle, AddChoice, AddSlider, AddButton...</summary>
        protected abstract void Build();

        /// <summary>Chamado toda vez que a seção aparece (bom lugar pra reler valores).</summary>
        protected virtual void OnShown() { }

        protected virtual void OnTick(float dt) { }

        // ---------- Criação de linhas ----------

        protected TRow Add<TRow>(TRow row) where TRow : OptionRow
        {
            row.Build(Scroll, RowsWidth);
            _rows.Add(row);
            return row;
        }

        protected ToggleRow AddToggle(string label, string description, Func<bool> get, Action<bool> set)
            => Add(new ToggleRow(Menu, label, description, get, set));

        protected ChoiceRow AddChoice(string label, string description, Func<IReadOnlyList<string>> options,
            Func<int> get, Action<int> set)
            => Add(new ChoiceRow(Menu, label, description, options, get, set));

        protected ChoiceRow AddChoice(string label, string description, string[] options, Func<int> get, Action<int> set)
            => Add(new ChoiceRow(Menu, label, description, () => options, get, set));

        protected SliderRow AddSlider(string label, string description, Func<float> get, Action<float> set,
            float step = 0.05f, Func<float, string> format = null)
            => Add(new SliderRow(Menu, label, description, get, set, step, format));

        protected ButtonRow AddButton(string label, string description, Action action,
            Func<string> rightText = null, bool emphasis = false)
            => Add(new ButtonRow(Menu, label, description, action, rightText, emphasis));

        protected HeaderRow AddHeader(string text) => Add(new HeaderRow(Menu, text));

        protected InfoRow AddInfo(string text, float height) => Add(new InfoRow(Menu, text, height));

        // ---------- Layout e rolagem ----------

        private void Layout()
        {
            float spacing = Menu.Theme.rowSpacing;
            float y = 0f;
            _tops = new float[_rows.Count];

            for (int i = 0; i < _rows.Count; i++)
            {
                _tops[i] = y;
                _rows[i].Root.anchoredPosition = new Vector2(0f, -y);
                y += _rows[i].Height + spacing;
            }

            _contentHeight = Mathf.Max(0f, y - spacing);
            Scroll.sizeDelta = new Vector2(0f, _contentHeight);
        }

        private float MaxScroll => Mathf.Max(0f, _contentHeight - Menu.ViewportHeight);

        public void ScrollBy(float pixels)
        {
            _scrollTarget = Mathf.Clamp(_scrollTarget + pixels, 0f, MaxScroll);
        }

        private void EnsureVisible(int index)
        {
            float viewport = Menu.ViewportHeight;
            if (viewport <= 0f || index < 0 || index >= _rows.Count) return;

            const float margin = 24f;
            float top = _tops[index];
            float bottom = top + _rows[index].Height;

            if (index == FirstFocusable()) _scrollTarget = 0f;
            else if (index == LastFocusable()) _scrollTarget = MaxScroll;
            else if (top - _scrollTarget < margin) _scrollTarget = top - margin;
            else if (bottom - _scrollTarget > viewport - margin) _scrollTarget = bottom - viewport + margin;

            _scrollTarget = Mathf.Clamp(_scrollTarget, 0f, MaxScroll);
        }

        // ---------- Foco ----------

        public int FirstFocusable()
        {
            for (int i = 0; i < _rows.Count; i++)
                if (_rows[i].Focusable) return i;
            return -1;
        }

        private int LastFocusable()
        {
            for (int i = _rows.Count - 1; i >= 0; i--)
                if (_rows[i].Focusable) return i;
            return -1;
        }

        public void SetFocusIndex(int index)
        {
            if (index < 0 || index >= _rows.Count || !_rows[index].Focusable) return;
            FocusIndex = index;
            EnsureVisible(index);
        }

        /// <summary>Anda pro próximo item selecionável (dá a volta no fim da lista).</summary>
        public bool MoveFocus(int direction)
        {
            int count = _rows.Count;
            if (count == 0 || FirstFocusable() < 0) return false;

            int i = FocusIndex < 0 ? 0 : FocusIndex;
            for (int step = 0; step < count; step++)
            {
                i = (i + direction + count) % count;
                if (_rows[i].Focusable)
                {
                    FocusIndex = i;
                    EnsureVisible(i);
                    return true;
                }
            }

            return false;
        }

        public int IndexOf(OptionRow row) => _rows.IndexOf(row);

        /// <summary>Marca visualmente qual linha está em foco (só quando o foco está no painel).</summary>
        public void ApplyFocus(bool panelFocused)
        {
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].SetFocused(panelFocused && i == FocusIndex);
        }

        public void CancelInputs()
        {
            foreach (OptionRow row in _rows) row.CancelInput();
        }

        public void RefreshRows()
        {
            foreach (OptionRow row in _rows) row.Refresh();
        }

        // ---------- Aparecer / sumir ----------

        public void Show(bool shown, bool instant = false, float extraDelay = 0f)
        {
            _shown = shown;

            if (shown)
            {
                Container.gameObject.SetActive(true);
                Extras.gameObject.SetActive(true);
                RefreshRows();
                OnShown();
                EnsureVisible(FocusIndex);

                for (int i = 0; i < _rows.Count; i++)
                {
                    if (instant) _rows[i].SnapShown(true);
                    else _rows[i].SetShown(true, extraDelay + Mathf.Min(0.07f + i * 0.035f, 0.5f));
                }

                if (instant)
                {
                    _extrasPresence = 1f;
                    _scroll = _scrollTarget;
                    _extrasGroup.alpha = 1f;
                }
            }
            else
            {
                foreach (OptionRow row in _rows)
                {
                    if (instant) row.SnapShown(false);
                    else row.SetShown(false, 0f);
                }

                if (instant)
                {
                    _extrasPresence = 0f;
                    Container.gameObject.SetActive(false);
                    Extras.gameObject.SetActive(false);
                }
            }
        }

        public void Tick(float dt)
        {
            if (!IsVisible) return;

            bool anyVisible = false;
            foreach (OptionRow row in _rows)
            {
                row.Tick(dt);
                if (row.Present > 0.001f) anyVisible = true;
            }

            float targetPresence = _shown ? 1f : 0f;
            _extrasPresence = MenuMotion.ReduceMotion
                ? targetPresence
                : Mathf.MoveTowards(_extrasPresence, targetPresence, dt / (_shown ? 0.3f : 0.12f));
            _extrasGroup.alpha = MenuEasing.OutCubic(_extrasPresence);

            _scrollTarget = Mathf.Clamp(_scrollTarget, 0f, MaxScroll);
            _scroll = MenuMotion.ReduceMotion ? _scrollTarget : Mathf.Lerp(_scroll, _scrollTarget, 1f - Mathf.Exp(-14f * dt));
            Scroll.anchoredPosition = new Vector2(0f, _scroll);

            OnTick(dt);

            if (!_shown && !anyVisible && _extrasPresence <= 0.001f)
            {
                Container.gameObject.SetActive(false);
                Extras.gameObject.SetActive(false);
            }
        }
    }
}
