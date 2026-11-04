using System;
using Core.Settings;
using Core.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Options
{
    /// <summary>
    /// Janela de confirmação com contagem regressiva (ex.: "Manter esta resolução?").
    /// Se o tempo acabar, escolhe "Reverter" sozinha. Teclado: A/D escolhem, Enter confirma, Esc reverte.
    /// </summary>
    public sealed class OptionsModal
    {
        private readonly OptionsMenu _menu;
        private CanvasGroup _group;
        private RectTransform _card;
        private TMP_Text _title;
        private TMP_Text _message;
        private TMP_Text _timer;
        private Image _keepBackground;
        private Image _revertBackground;
        private TMP_Text _keepLabel;
        private TMP_Text _revertLabel;
        private Action _onKeep;
        private Action _onRevert;
        private float _remaining;
        private float _appear;
        private int _choice;

        public bool Active { get; private set; }

        public OptionsModal(OptionsMenu menu)
        {
            _menu = menu;
        }

        public void Build(Transform parent)
        {
            OptionsStyle style = _menu.Style;

            RectTransform root = OptionsUI.Rect("Modal", parent);
            OptionsUI.Fill(root);
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            Image dim = OptionsUI.Image(root, "Dim", new Color(0f, 0f, 0f, 0.72f), true);
            OptionsUI.Fill(dim.rectTransform);

            _card = OptionsUI.Rect("Card", root);
            OptionsUI.Place(_card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 420f));

            Image shadow = OptionsUI.Image(_card, "Shadow", _menu.Accent);
            OptionsUI.Fill(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(12f, -12f);
            OptionsUI.Skew(shadow, _menu.Theme.skew);

            Image bg = OptionsUI.Image(_card, "Bg", style.Ink, true);
            OptionsUI.Fill(bg.rectTransform);
            OptionsUI.Skew(bg, _menu.Theme.skew);

            _title = OptionsUI.Text(_card, "Title", "", style.Display, style.Size(54), style.Paper, TextAlignmentOptions.TopLeft);
            OptionsUI.Fill(_title.rectTransform, 60f, 0f, 60f, 36f);

            _message = OptionsUI.Text(_card, "Message", "", style.Body, style.Size(28), style.Muted,
                TextAlignmentOptions.TopLeft, true);
            OptionsUI.Fill(_message.rectTransform, 60f, 150f, 200f, 120f);

            _timer = OptionsUI.Text(_card, "Timer", "", style.Display, style.Size(120), _menu.Accent, TextAlignmentOptions.TopRight);
            OptionsUI.Fill(_timer.rectTransform, 0f, 0f, 60f, 90f);

            BuildButton("Keep", new Vector2(-190f, 70f), 0, out _keepBackground, out _keepLabel);
            BuildButton("Revert", new Vector2(150f, 70f), 1, out _revertBackground, out _revertLabel);
        }

        private void BuildButton(string name, Vector2 position, int choice, out Image background, out TMP_Text label)
        {
            OptionsStyle style = _menu.Style;

            RectTransform rt = OptionsUI.Rect(name, _card);
            OptionsUI.Place(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(300f, 80f));

            background = OptionsUI.Image(rt, "Bg", style.InkSoft, true);
            OptionsUI.Fill(background.rectTransform);
            OptionsUI.Skew(background, _menu.Theme.skew);

            OptionsPointerRelay relay = background.gameObject.AddComponent<OptionsPointerRelay>();
            relay.Click = _ =>
            {
                _choice = choice;
                Resolve(choice == 0);
            };

            label = OptionsUI.Text(rt, "Label", "", style.Display, style.Size(30), style.Text, TextAlignmentOptions.Center);
            OptionsUI.Fill(label.rectTransform);
        }

        public void ShowConfirm(string title, string message, float seconds, Action onKeep, Action onRevert)
        {
            _title.text = _menu.Style.Caps(title);
            _message.text = message;
            _keepLabel.text = _menu.Style.Caps(GameLanguage.T("Manter", "Keep"));
            _revertLabel.text = _menu.Style.Caps(GameLanguage.T("Reverter", "Revert"));
            _onKeep = onKeep;
            _onRevert = onRevert;
            _remaining = Mathf.Max(1f, seconds);
            _choice = 0;
            Active = true;
            _group.blocksRaycasts = true;
            _appear = MenuMotion.ReduceMotion ? 1f : 0f;
            _menu.PlayConfirm();
            Apply();
        }

        /// <summary>Fecha sem escolher nada (ex.: menu fechou).</summary>
        public void Dismiss(bool revert)
        {
            if (!Active) return;
            Resolve(!revert);
        }

        private void Resolve(bool keep)
        {
            if (!Active) return;

            Active = false;
            _group.blocksRaycasts = false;

            Action action = keep ? _onKeep : _onRevert;
            _onKeep = null;
            _onRevert = null;
            action?.Invoke();
            _menu.PlayConfirm();
        }

        public void Tick(float dt)
        {
            if (Active)
            {
                _remaining -= dt;
                if (_remaining <= 0f)
                {
                    Resolve(false);
                }
                else
                {
                    HandleInput();
                    _timer.text = Mathf.CeilToInt(_remaining).ToString();
                }
            }

            float target = Active ? 1f : 0f;
            _appear = MenuMotion.ReduceMotion ? target : Mathf.MoveTowards(_appear, target, dt / (Active ? 0.25f : 0.15f));
            Apply();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) { _choice = 0; _menu.PlayTick(); }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) { _choice = 1; _menu.PlayTick(); }
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)) Resolve(false);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                     Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0)) Resolve(_choice == 0);
        }

        private void Apply()
        {
            OptionsStyle style = _menu.Style;
            float e = MenuEasing.OutCubic(_appear);

            _group.alpha = e;
            if (_card != null)
            {
                float scale = MenuMotion.ReduceMotion ? 1f : 0.94f + 0.06f * e;
                _card.localScale = new Vector3(scale, scale, 1f);
            }

            StyleButton(_keepBackground, _keepLabel, _choice == 0, style);
            StyleButton(_revertBackground, _revertLabel, _choice == 1, style);
        }

        private static void StyleButton(Image background, TMP_Text label, bool selected, OptionsStyle style)
        {
            if (background == null) return;
            background.color = selected ? style.Paper : style.InkSoft;
            label.color = selected ? style.TextOnPaper : style.Text;
        }
    }
}
