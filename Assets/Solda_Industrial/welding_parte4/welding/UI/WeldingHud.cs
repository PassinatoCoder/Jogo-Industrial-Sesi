using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.Settings;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// HUD do minigame: checklist, vitais do personagem, dica contextual e avisos.
    /// Cada grupo ocupa uma faixa própria; nenhum elemento visual registra hitbox.
    /// </summary>
    public sealed class WeldingHud
    {
        public enum StepState { Pending, Current, Done }

        private static readonly Color Ink = new Color(0.06f, 0.07f, 0.09f, 0.88f);
        private static readonly Color Paper = new Color(0.96f, 0.94f, 0.88f, 1f);
        private static readonly Color Muted = new Color(0.72f, 0.72f, 0.72f, 1f);
        private static readonly Color Accent = new Color(1f, 0.82f, 0.1f, 1f);
        private static readonly Color Good = new Color(0.35f, 0.85f, 0.45f, 1f);
        private static readonly Color Bad = new Color(1f, 0.32f, 0.25f, 1f);

        private readonly RectTransform _root;
        private readonly List<Image> _stepBoxes = new List<Image>();
        private readonly List<TextMeshProUGUI> _stepTexts = new List<TextMeshProUGUI>();
        private readonly List<GameObject> _equipment = new List<GameObject>();
        private RectTransform _checklistRoot;
        private CanvasGroup _checklistGroup;
        private TextMeshProUGUI _hint;
        private TextMeshProUGUI _toast;
        private CanvasGroup _group;
        private readonly Image[] _vitalFills = new Image[3];
        private readonly TextMeshProUGUI[] _vitalValues = new TextMeshProUGUI[3];
        private float _toastTimer;

        public WeldingHud(RectTransform parent)
        {
            _root = WeldUI.MakeRect("Hud", parent);
            WeldUI.Fill(_root);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            BuildVitals();
            BuildHint();
            BuildToast();
        }

        public void SetVisible(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }

        // ---------- Checklist ----------

        public void BuildChecklist(IList<string> titles)
        {
            if (_checklistRoot != null) Object.Destroy(_checklistRoot.gameObject);
            _stepBoxes.Clear();
            _stepTexts.Clear();

            float rowHeight = 43f;
            float height = 58f + titles.Count * rowHeight;
            _checklistRoot = WeldUI.MakeRect("Checklist", _root);
            WeldUI.Place(_checklistRoot, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(455f, height),
                new Vector2(0f, 1f));

            Image bg = WeldUI.MakeImage(_checklistRoot, "Bg", null, Ink);
            WeldUI.Fill(bg.rectTransform);
            Image bar = WeldUI.MakeImage(_checklistRoot, "Bar", null, Accent);
            bar.rectTransform.anchorMin = new Vector2(0f, 1f);
            bar.rectTransform.anchorMax = new Vector2(1f, 1f);
            bar.rectTransform.pivot = new Vector2(0.5f, 1f);
            bar.rectTransform.offsetMin = new Vector2(0f, -6f);
            bar.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI header = WeldUI.MakeText(_checklistRoot, "Header",
                GameLanguage.T("PROCEDIMENTO", "PROCEDURE"), 25f, Accent, TextAlignmentOptions.TopLeft);
            WeldUI.Fill(header.rectTransform, 18f, 0f, 16f, 12f);

            for (int i = 0; i < titles.Count; i++)
            {
                float y = -50f - i * rowHeight;
                Image box = WeldUI.MakeImage(_checklistRoot, "Box" + i, null, Muted);
                WeldUI.Place(box.rectTransform, new Vector2(0f, 1f), new Vector2(18f, y), new Vector2(22f, 22f),
                    new Vector2(0f, 1f));

                TextMeshProUGUI label = WeldUI.MakeText(_checklistRoot, "Step" + i, titles[i], 22f, Muted,
                    TextAlignmentOptions.TopLeft);
                WeldUI.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(54f, y + 2f), new Vector2(385f, 34f),
                    new Vector2(0f, 1f));
                _stepBoxes.Add(box);
                _stepTexts.Add(label);
            }

            _checklistGroup = _checklistRoot.gameObject.AddComponent<CanvasGroup>();
        }

        public void SetChecklistVisible(bool visible)
        {
            if (_checklistGroup == null) return;
            _checklistGroup.alpha = visible ? 1f : 0f;
            _checklistGroup.interactable = visible;
            _checklistGroup.blocksRaycasts = visible;
        }

        public void SetStep(int index, StepState state)
        {
            if (index < 0 || index >= _stepBoxes.Count) return;
            switch (state)
            {
                case StepState.Done:
                    _stepBoxes[index].color = Good;
                    _stepTexts[index].color = Good;
                    break;
                case StepState.Current:
                    _stepBoxes[index].color = Accent;
                    _stepTexts[index].color = Paper;
                    break;
                default:
                    _stepBoxes[index].color = Muted;
                    _stepTexts[index].color = Muted;
                    break;
            }
        }

        // ---------- Vitais: integridade, stamina e vida ----------

        private void BuildVitals()
        {
            RectTransform panel = WeldUI.MakeRect("Vitals", _root);
            WeldUI.Place(panel, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(390f, 174f),
                new Vector2(1f, 1f));
            Image bg = WeldUI.MakeImage(panel, "Bg", null, Ink);
            WeldUI.Fill(bg.rectTransform);

            TextMeshProUGUI header = WeldUI.MakeText(panel, "Header", GameLanguage.T("ESTADO", "STATUS"), 24f,
                Accent, TextAlignmentOptions.TopLeft);
            WeldUI.Fill(header.rectTransform, 16f, 0f, 14f, 10f);

            string[] names =
            {
                GameLanguage.T("INTEGRIDADE", "INTEGRITY"),
                GameLanguage.T("STAMINA", "STAMINA"),
                GameLanguage.T("VIDA", "HEALTH")
            };
            Color[] colors = { new Color(0.25f, 0.78f, 0.48f, 1f), new Color(0.3f, 0.7f, 1f, 1f), new Color(0.9f, 0.28f, 0.25f, 1f) };

            for (int i = 0; i < 3; i++)
            {
                float y = -46f - i * 39f;
                TextMeshProUGUI label = WeldUI.MakeText(panel, "VitalLabel" + i, names[i], 18f, Muted,
                    TextAlignmentOptions.MidlineLeft);
                WeldUI.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(16f, y), new Vector2(116f, 28f),
                    new Vector2(0f, 1f));

                Image barBg = WeldUI.MakeImage(panel, "VitalBg" + i, null, new Color(0f, 0f, 0f, 0.65f));
                WeldUI.Place(barBg.rectTransform, new Vector2(0f, 1f), new Vector2(132f, y - 2f), new Vector2(190f, 20f),
                    new Vector2(0f, 1f));
                Image fill = WeldUI.MakeImage(barBg.transform, "Fill", null, colors[i]);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = Vector2.one;
                fill.rectTransform.offsetMin = new Vector2(2f, 2f);
                fill.rectTransform.offsetMax = new Vector2(-2f, -2f);
                _vitalFills[i] = fill;

                TextMeshProUGUI value = WeldUI.MakeText(panel, "VitalValue" + i, "100%", 18f, Paper,
                    TextAlignmentOptions.MidlineRight);
                WeldUI.Place(value.rectTransform, new Vector2(1f, 1f), new Vector2(-14f, y), new Vector2(54f, 28f),
                    new Vector2(1f, 1f));
                _vitalValues[i] = value;
            }
        }

        public void SetVitals(int integrity, int stamina, int health)
        {
            SetVital(0, integrity);
            SetVital(1, stamina);
            SetVital(2, health);
        }

        private void SetVital(int index, int value)
        {
            value = Mathf.Clamp(value, 0, 100);
            RectTransform fill = _vitalFills[index].rectTransform;
            fill.anchorMax = new Vector2(value / 100f, 1f);
            _vitalValues[index].text = value + "%";
            if (index == 0) _vitalFills[index].color = value > 60 ? Good : (value > 25 ? Accent : Bad);
            else if (index == 1) _vitalFills[index].color = value > 35 ? new Color(0.3f, 0.7f, 1f, 1f) : Accent;
            else _vitalFills[index].color = value > 35 ? Good : Bad;
        }

        // ---------- Dica e avisos ----------

        private void BuildHint()
        {
            RectTransform box = WeldUI.MakeRect("Hint", _root);
            WeldUI.Place(box, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1460f, 76f),
                new Vector2(0.5f, 0f));
            Image bg = WeldUI.MakeImage(box, "Bg", null, Ink);
            WeldUI.Fill(bg.rectTransform);
            _hint = WeldUI.MakeText(box, "Text", "", 27f, Paper, TextAlignmentOptions.Center, true);
            WeldUI.Fill(_hint.rectTransform, 20f, 5f, 20f, 5f);
        }

        public void SetHint(string text)
        {
            _hint.text = text;
        }

        private void BuildToast()
        {
            _toast = WeldUI.MakeText(_root, "Toast", "", 30f, Bad, TextAlignmentOptions.Center, true);
            WeldUI.Place(_toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(850f, 66f),
                new Vector2(0.5f, 1f));
            _toast.alpha = 0f;
        }

        public void Toast(string text, Color color, float seconds = 2.6f)
        {
            _toast.text = text;
            _toast.color = color;
            _toast.alpha = 1f;
            _toastTimer = seconds;
        }

        public void Tick(float dt)
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= dt;
                _toast.alpha = Mathf.Clamp01(_toastTimer / 0.5f);
            }
        }

        // ---------- EPIs vestidos ----------

        public void AddEquipment(Sprite sprite, Color placeholder, string label)
        {
            const float size = 72f;
            float x = 24f + _equipment.Count * (size + 8f);
            Image icon = WeldUI.MakeProp(_root, "Equip" + _equipment.Count, sprite, Vector2.zero,
                new Vector2(size, size), placeholder, label, 15f);
            WeldUI.Place(icon.rectTransform, new Vector2(0f, 0f), new Vector2(x, 124f), new Vector2(size, size),
                new Vector2(0f, 0f));
            _equipment.Add(icon.gameObject);
        }

        public void ClearEquipment()
        {
            for (int i = 0; i < _equipment.Count; i++)
                if (_equipment[i] != null) Object.Destroy(_equipment[i]);
            _equipment.Clear();
        }
    }
}
