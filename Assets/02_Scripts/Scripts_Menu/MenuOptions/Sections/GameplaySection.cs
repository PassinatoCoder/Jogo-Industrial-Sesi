using System.Collections.Generic;
using Core.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Options
{
    /// <summary>
    /// Jogabilidade: dicas na tela + revisão do tutorial.
    /// Lado esquerdo: lista de lições. Lado direito: o conteúdo da lição selecionada
    /// (aparece assim que o foco chega nela). Lições vêm do OptionsMenu (campo "Lessons").
    /// </summary>
    public sealed class GameplaySection : OptionsSection
    {
        private IReadOnlyList<TutorialLesson> _lessons;
        private int _viewed;
        private TMP_Text _title;
        private TMP_Text _body;
        private Image _image;

        protected override void Build()
        {
            Title = T("Jogabilidade", "Gameplay");
            Subtitle = T("Dicas e revisão do tutorial.", "Hints and tutorial review.");
            RowsWidth = 0.52f;
            _lessons = Menu.Lessons;

            AddHeader(T("Dicas", "Hints"));

            AddToggle(T("Dicas na tela", "On-screen hints"),
                T("Mostra avisos curtos que ajudam durante o jogo.", "Shows short notices that help during the game."),
                () => GameplaySettings.ShowHints,
                v => GameplaySettings.ShowHints = v);

            if (_lessons == null || _lessons.Count == 0) return;

            AddHeader(T("Revisar tutorial", "Review tutorial"));
            for (int i = 0; i < _lessons.Count; i++) AddLesson(i);

            BuildViewer();
            ShowLesson(0);
        }

        protected override void OnShown()
        {
            if (_title != null) ShowLesson(_viewed); // atualiza nomes de tecla e idioma
        }

        // ---------- Lista ----------

        private void AddLesson(int index)
        {
            TutorialLesson lesson = _lessons[index];

            ButtonRow row = AddButton(LessonTitle(lesson),
                T("Os detalhes aparecem ao lado.", "The details appear on the right."),
                () => ShowLesson(index),
                () => IsLocked(lesson) ? T("Bloqueada", "Locked") : "");

            row.EnabledWhen = () => !IsLocked(lesson);
            row.FocusGained += _ => ShowLesson(index);
        }

        private bool IsLocked(TutorialLesson lesson)
        {
            return Menu.LockLessons && !TutorialProgress.IsUnlocked(lesson.id);
        }

        private static string LessonTitle(TutorialLesson lesson)
        {
            string title = lesson.Title;
            return string.IsNullOrEmpty(title) ? lesson.id : title;
        }

        // ---------- Visualizador ----------

        private void BuildViewer()
        {
            OptionsStyle style = Menu.Style;

            RectTransform root = OptionsUI.Rect("Viewer", Extras);
            root.anchorMin = new Vector2(0.55f, 0f);
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Image background = OptionsUI.Image(root, "Bg", new Color(style.Ink.r, style.Ink.g, style.Ink.b, 0.85f));
            OptionsUI.Fill(background.rectTransform);
            OptionsUI.Skew(background, Menu.Theme.skew * 0.6f);

            Image bar = OptionsUI.Image(root, "AccentBar", Accent);
            bar.rectTransform.anchorMin = new Vector2(0f, 1f);
            bar.rectTransform.anchorMax = new Vector2(1f, 1f);
            bar.rectTransform.pivot = new Vector2(0.5f, 1f);
            bar.rectTransform.offsetMin = new Vector2(20f, -10f);
            bar.rectTransform.offsetMax = new Vector2(-20f, 0f);

            _title = OptionsUI.Text(root, "Title", "", style.Display, style.Size(38), style.Paper, TextAlignmentOptions.TopLeft);
            Band(_title.rectTransform, 28f, 60f, 34f, 34f);

            _image = OptionsUI.Image(root, "Image", Color.white);
            _image.preserveAspect = true;
            _image.enabled = false;
            Band(_image.rectTransform, 100f, 220f, 34f, 34f);

            _body = OptionsUI.Text(root, "Body", "", style.Body, style.Size(26), style.Text,
                TextAlignmentOptions.TopLeft, true);
            _body.enableAutoSizing = true;
            _body.fontSizeMin = 16f;
            _body.fontSizeMax = style.Size(26);
        }

        /// <summary>Faixa esticada na largura, presa ao topo (top = distância do topo, height = altura).</summary>
        private static void Band(RectTransform rt, float top, float height, float left, float right)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -(top + height));
            rt.offsetMax = new Vector2(-right, -top);
        }

        private void ShowLesson(int index)
        {
            if (_title == null || _lessons == null || _lessons.Count == 0) return;

            _viewed = Mathf.Clamp(index, 0, _lessons.Count - 1);
            TutorialLesson lesson = _lessons[_viewed];
            bool locked = IsLocked(lesson);
            bool hasImage = !locked && lesson.image != null;

            _title.text = Menu.Style.Caps(LessonTitle(lesson));

            _image.sprite = hasImage ? lesson.image : null;
            _image.enabled = hasImage;

            RectTransform body = _body.rectTransform;
            float top = hasImage ? 340f : 100f;
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(34f, 28f);
            body.offsetMax = new Vector2(-34f, -top);

            _body.text = locked
                ? T("Conclua esta parte do tutorial para poder revê-la aqui.",
                    "Complete this part of the tutorial to review it here.")
                : lesson.Body;
        }
    }
}
