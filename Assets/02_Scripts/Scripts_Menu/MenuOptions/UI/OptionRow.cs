using System;
using Core.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UI.Options
{
    /// <summary>
    /// Base de uma linha do painel (liga/desliga, escolha, slider, botão...).
    /// Cuida de: aparecer em cascata, foco (entorta de leve + inverte a cor), indicador lateral e clique.
    /// NÃO reage a "mouse em cima" (de propósito).
    ///
    /// Não é MonoBehaviour: é uma classe comum que o OptionsSection cria e atualiza.
    /// </summary>
    public abstract class OptionRow
    {
        private const float AppearSlide = 70f;
        private const float FocusIndent = 16f;
        private const float FocusTilt = 0.8f;

        private CanvasGroup _group;
        private Image _background;
        private Image _strip;
        private TMP_Text _label;
        private MenuSpring _x;
        private MenuSpring _tilt;
        private float _focusT;
        private bool _focused;
        private bool _shown;
        private float _delay;
        private float _present;

        protected OptionsMenu Menu { get; }
        protected string LabelText { get; }
        protected RectTransform Visual { get; private set; }
        protected OptionsStyle Style => Menu.Style;

        public RectTransform Root { get; private set; }
        public float Height { get; }
        public bool Focusable { get; }
        public string Description { get; set; }
        public Func<bool> EnabledWhen { get; set; }

        /// <summary>0 = invisível, 1 = totalmente na tela.</summary>
        public float Present => _present;

        public bool IsEnabled => EnabledWhen == null || EnabledWhen();

        /// <summary>True enquanto a linha quer ler o teclado sozinha (ex.: trocando tecla).</summary>
        public virtual bool ConsumesInput => false;

        /// <summary>Clicar na linha chama Activate()? (slider não.)</summary>
        public virtual bool ActivatesOnClick => true;

        /// <summary>Esquerda/Direita ajustam o valor? Se não, "esquerda" volta pras abas.</summary>
        public virtual bool HandlesHorizontal => true;

        protected virtual bool UsesStandardLabel => true;
        protected virtual float LabelWidthFraction => 0.5f;

        public event Action<OptionRow> FocusGained;

        protected OptionRow(OptionsMenu menu, string label, float height, bool focusable = true)
        {
            Menu = menu;
            LabelText = label;
            Height = height;
            Focusable = focusable;
        }

        // ---------- Construção ----------

        public void Build(Transform parent, float widthFraction)
        {
            Root = OptionsUI.Rect("Row_" + LabelText, parent);
            Root.anchorMin = new Vector2(0f, 1f);
            Root.anchorMax = new Vector2(widthFraction, 1f);
            Root.pivot = new Vector2(0.5f, 1f);
            Root.sizeDelta = new Vector2(0f, Height);

            Visual = OptionsUI.Rect("Visual", Root);
            OptionsUI.Fill(Visual);
            _group = Visual.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            if (Focusable)
            {
                _background = OptionsUI.Image(Visual, "Bg", Style.InkSoft, true);
                OptionsUI.Fill(_background.rectTransform);
                OptionsUI.Skew(_background, Menu.Theme.skew);

                OptionsPointerRelay relay = _background.gameObject.AddComponent<OptionsPointerRelay>();
                relay.Click = _ => Menu.RowClicked(this);

                _strip = OptionsUI.Image(Visual, "Strip", Menu.Accent);
                OptionsUI.Place(_strip.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(-6f, 0f), new Vector2(10f, Height * 0.72f));
                OptionsUI.Skew(_strip, Menu.Theme.skew);
            }

            if (UsesStandardLabel)
            {
                _label = OptionsUI.Text(Visual, "Label", LabelText, Style.Display, Style.Size(30), Style.Text,
                    TextAlignmentOptions.MidlineLeft);
                RectTransform rt = _label.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(LabelWidthFraction, 1f);
                rt.offsetMin = new Vector2(34f, 0f);
                rt.offsetMax = new Vector2(0f, 0f);
            }

            BuildControls(Visual);
            Refresh();
            Apply(0f);
        }

        protected abstract void BuildControls(RectTransform visual);

        // ---------- Estado ----------

        public void SetFocused(bool focused)
        {
            if (!Focusable) focused = false;
            if (focused && !_focused) FocusGained?.Invoke(this);
            _focused = focused;
        }

        /// <summary>Faz a linha entrar (ou sair) depois de 'delay' segundos. Gera a cascata.</summary>
        public void SetShown(bool shown, float delay)
        {
            _shown = shown;
            _delay = shown ? Mathf.Max(0f, delay) : 0f;
        }

        public void SnapShown(bool shown)
        {
            _shown = shown;
            _delay = 0f;
            _present = shown ? 1f : 0f;
            _focusT = _focused ? 1f : 0f;
            _x.Snap(_focused ? FocusIndent : 0f);
            _tilt.Snap(_focused ? FocusTilt : 0f);
            Apply(0f);
        }

        // ---------- Interação (sobrescreva o que precisar) ----------

        /// <summary>Atualiza o que a linha mostra lendo o valor atual.</summary>
        public virtual void Refresh() { }

        /// <summary>Esquerda (-1) ou direita (+1).</summary>
        public virtual void Adjust(int direction) { }

        /// <summary>Enter / clique.</summary>
        public virtual void Activate() { }

        /// <summary>Chamado todo frame enquanto ConsumesInput for true.</summary>
        public virtual void UpdateInput() { }

        /// <summary>Se a linha estava esperando uma tecla, cancela.</summary>
        public virtual void CancelInput() { }

        protected virtual void OnVisualUpdate(float focus, bool enabled, float dt) { }

        // ---------- Animação ----------

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
                _present = reduce ? target : Mathf.MoveTowards(_present, target, dt / (_shown ? 0.28f : 0.12f));
            }

            if (reduce)
            {
                _focusT = _focused ? 1f : 0f;
                _x.Snap(_focused ? FocusIndent : 0f);
                _tilt.Snap(_focused ? FocusTilt : 0f);
            }
            else
            {
                _focusT = Mathf.MoveTowards(_focusT, _focused ? 1f : 0f, dt * 8f);
                _x.Step(_focused ? FocusIndent : 0f, 320f, 26f, dt);
                _tilt.Step(_focused ? FocusTilt : 0f, 320f, 26f, dt);
            }

            Apply(dt);
        }

        private void Apply(float dt)
        {
            if (Visual == null) return;

            float e = MenuEasing.OutCubic(_present);
            float slide = MenuMotion.ReduceMotion ? 0f : (1f - e) * AppearSlide;

            _group.alpha = e;
            _group.blocksRaycasts = _present > 0.5f;
            Visual.anchoredPosition = new Vector2(_x.Value + slide, 0f);
            Visual.localRotation = Quaternion.Euler(0f, 0f, _tilt.Value);

            float t = Mathf.SmoothStep(0f, 1f, _focusT);
            bool enabled = IsEnabled;

            if (_background != null) _background.color = Color.Lerp(Style.InkSoft, Style.Paper, t);

            if (_strip != null)
            {
                _strip.color = Menu.Accent;
                Vector3 s = _strip.rectTransform.localScale;
                s.x = t;
                _strip.rectTransform.localScale = s;
            }

            if (_label != null)
            {
                Color c = Color.Lerp(Style.Text, Style.TextOnPaper, t);
                if (!enabled) c.a *= 0.4f;
                _label.color = c;
            }

            OnVisualUpdate(t, enabled, dt);
        }
    }
}
