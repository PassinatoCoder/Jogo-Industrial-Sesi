using Core.UI.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Options
{
    /// <summary>
    /// Fundo que troca de imagem com a seção (opcional): a imagem nova entra deslizando,
    /// com um zoominho e fade, por cima da antiga. Trocas rápidas não piscam.
    /// </summary>
    public sealed class OptionsBackground
    {
        private const float Duration = 0.5f;
        private const float Slide = 140f;
        private const float Zoom = 0.06f;

        private readonly Image _back;
        private readonly Image _front;
        private Sprite _current;
        private float _progress = 1f;

        public OptionsBackground(Transform parent)
        {
            _back = Create(parent, "Back");
            _front = Create(parent, "Front");
        }

        private static Image Create(Transform parent, string name)
        {
            Image image = OptionsUI.Image(parent, name, Color.white);
            // Sobra de borda pra o deslize e o zoom nunca mostrarem buraco
            OptionsUI.Fill(image.rectTransform, -80f, -80f, -80f, -80f);
            image.preserveAspect = false;
            image.enabled = false;
            return image;
        }

        public void Show(Sprite sprite, bool instant)
        {
            if (sprite == null || sprite == _current) return;
            _current = sprite;

            if (instant || MenuMotion.ReduceMotion || !_back.enabled)
            {
                _back.sprite = sprite;
                _back.enabled = true;
                _front.enabled = false;
                _progress = 1f;
                return;
            }

            // Trocou no meio de outra troca: a que estava chegando vira o fundo
            if (_progress < 1f) Commit();

            _front.sprite = sprite;
            _front.enabled = true;
            _progress = 0f;
            Apply();
        }

        public void Tick(float dt)
        {
            if (_progress >= 1f) return;

            _progress = Mathf.Min(1f, _progress + dt / Duration);
            Apply();

            if (_progress >= 1f) Commit();
        }

        private void Apply()
        {
            float e = MenuEasing.OutCubic(_progress);
            RectTransform rt = _front.rectTransform;
            rt.anchoredPosition = new Vector2(Slide * (1f - e), 0f);

            float scale = 1f + Zoom * (1f - e);
            rt.localScale = new Vector3(scale, scale, 1f);

            Color c = _front.color;
            c.a = MenuEasing.OutCubic(Mathf.Clamp01(_progress * 2.2f));
            _front.color = c;
        }

        private void Commit()
        {
            _back.sprite = _front.sprite;
            _back.enabled = true;
            _front.enabled = false;
            _progress = 1f;

            RectTransform rt = _front.rectTransform;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
