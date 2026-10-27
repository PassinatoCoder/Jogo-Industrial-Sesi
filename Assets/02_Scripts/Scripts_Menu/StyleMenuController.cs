using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UI.Menu
{
    /// <summary>
    /// Maestro do menu estilizado (coloque no MenuContainer):
    /// - mantém SEMPRE um item selecionado (teclado e controle nunca ficam "sem foco");
    /// - muda a cor do fundo conforme o item selecionado;
    /// - troca a arte lateral (com deslize) conforme o item selecionado.
    /// </summary>
    public class StyleMenuController : MonoBehaviour
    {
        [Header("Itens")]
        [Tooltip("Vazio = pega todos os StyleMenuItem que são filhos deste objeto.")]
        [SerializeField] private StyleMenuItem[] items;
        [Tooltip("CanvasGroup que liga/desliga o menu (o mesmo 'Buttons Group' do MainMenuController). Vazio = procura nos pais.")]
        [SerializeField] private CanvasGroup menuGroup;

        [Header("Fundo colorido (muda de cor com o item selecionado)")]
        [SerializeField] private Graphic colorWash;
        [SerializeField] private float washColorSpeed = 9f;

        [Header("Arte lateral (troca com o item selecionado)")]
        [SerializeField] private Image featuredImage;
        [SerializeField] private float featuredSlide = 120f;
        [SerializeField] private float featuredOutTime = 0.12f;
        [SerializeField] private float featuredInTime = 0.4f;

        private StyleMenuItem _last;
        private Color _washTarget = Color.white;
        private float _washAlpha = 1f;

        private Vector2 _featuredBasePos;
        private float _featuredBaseAlpha = 1f;
        private float _featuredOffset;
        private float _featuredAlpha = 1f;
        private Sprite _featuredCurrent;
        private Coroutine _featuredRoutine;

        // ---------- Ciclo de vida ----------

        private void Awake()
        {
            if (items == null || items.Length == 0) items = GetComponentsInChildren<StyleMenuItem>(true);
            if (menuGroup == null) menuGroup = GetComponentInParent<CanvasGroup>();

            if (colorWash != null) _washAlpha = colorWash.color.a;

            if (featuredImage != null)
            {
                _featuredBasePos = featuredImage.rectTransform.anchoredPosition;
                _featuredBaseAlpha = featuredImage.color.a;
            }
        }

        private void OnEnable()
        {
            foreach (StyleMenuItem item in items)
                if (item != null) item.Selected += OnItemSelected;
        }

        private void OnDisable()
        {
            foreach (StyleMenuItem item in items)
                if (item != null) item.Selected -= OnItemSelected;

            if (_featuredRoutine != null)
            {
                StopCoroutine(_featuredRoutine);
                _featuredRoutine = null;
            }
        }

        private void Start()
        {
            // Usa o primeiro item mesmo com o menu ainda travado (fase "Pressione qualquer tecla"),
            // assim o fundo já nasce na cor certa e não "pisca" na primeira seleção.
            StyleMenuItem first = null;
            foreach (StyleMenuItem item in items)
            {
                if (item != null)
                {
                    first = item;
                    break;
                }
            }
            if (first == null) return;

            _washTarget = first.AccentColor;
            _washTarget.a = _washAlpha;
            if (colorWash != null) colorWash.color = _washTarget;

            ShowFeaturedInstant(first.FeaturedSprite);
        }

        private void LateUpdate()
        {
            if (colorWash != null)
            {
                float k = 1f - Mathf.Exp(-washColorSpeed * Time.unscaledDeltaTime);
                colorWash.color = MenuMotion.ReduceMotion
                    ? _washTarget
                    : Color.Lerp(colorWash.color, _washTarget, k);
            }

            EnsureSelection();
        }

        // ---------- Seleção ----------

        private void OnItemSelected(StyleMenuItem item)
        {
            _last = item;

            _washTarget = item.AccentColor;
            _washTarget.a = _washAlpha;

            if (featuredImage != null && item.FeaturedSprite != null && item.FeaturedSprite != _featuredCurrent)
                SwapFeatured(item.FeaturedSprite);
        }

        /// <summary>Se o menu está ativo e nada dele está selecionado, volta o foco pro último item.</summary>
        private void EnsureSelection()
        {
            EventSystem es = EventSystem.current;
            if (es == null || items == null || items.Length == 0) return;
            if (menuGroup != null && !menuGroup.interactable) return;

            if (IsOwn(es.currentSelectedGameObject)) return;

            StyleMenuItem target = (_last != null && IsUsable(_last)) ? _last : FirstUsable();
            if (target != null) es.SetSelectedGameObject(target.gameObject);
        }

        private bool IsOwn(GameObject go)
        {
            if (go == null) return false;
            foreach (StyleMenuItem item in items)
                if (item != null && item.gameObject == go) return true;
            return false;
        }

        private static bool IsUsable(StyleMenuItem item)
        {
            if (!item.isActiveAndEnabled) return false;
            Selectable selectable = item.GetComponent<Selectable>();
            return selectable != null && selectable.IsInteractable();
        }

        private StyleMenuItem FirstUsable()
        {
            foreach (StyleMenuItem item in items)
                if (item != null && IsUsable(item)) return item;
            return null;
        }

        // ---------- Arte lateral ----------

        private void ShowFeaturedInstant(Sprite sprite)
        {
            _featuredCurrent = sprite;
            if (featuredImage == null) return;

            featuredImage.sprite = sprite;
            featuredImage.enabled = sprite != null;
            SetFeaturedPose(0f, 1f);
        }

        private void SwapFeatured(Sprite next)
        {
            _featuredCurrent = next;

            if (_featuredRoutine != null)
            {
                StopCoroutine(_featuredRoutine);
                _featuredRoutine = null;
            }

            if (!isActiveAndEnabled || MenuMotion.ReduceMotion)
            {
                featuredImage.sprite = next;
                featuredImage.enabled = true;
                SetFeaturedPose(0f, 1f);
                return;
            }

            _featuredRoutine = StartCoroutine(SwapFeaturedRoutine(next));
        }

        private IEnumerator SwapFeaturedRoutine(Sprite next)
        {
            float startOffset = _featuredOffset;
            float startAlpha = _featuredAlpha;

            // Sai: desliza um pouco pra direita e some
            float t = 0f;
            while (t < featuredOutTime)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float x = Mathf.Clamp01(t / featuredOutTime);
                SetFeaturedPose(Mathf.Lerp(startOffset, featuredSlide * 0.5f, x), Mathf.Lerp(startAlpha, 0f, x));
                yield return null;
            }

            // Troca o sprite no escuro
            featuredImage.sprite = next;
            featuredImage.enabled = true;

            // Entra: vem da direita com um pequeno overshoot
            t = 0f;
            while (t < featuredInTime)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float x = Mathf.Clamp01(t / featuredInTime);
                SetFeaturedPose(featuredSlide * (1f - MenuEasing.OutBack(x)), MenuEasing.OutCubic(Mathf.Clamp01(x * 2.5f)));
                yield return null;
            }

            SetFeaturedPose(0f, 1f);
            _featuredRoutine = null;
        }

        private void SetFeaturedPose(float offsetX, float alpha01)
        {
            _featuredOffset = offsetX;
            _featuredAlpha = alpha01;

            if (featuredImage == null) return;

            featuredImage.rectTransform.anchoredPosition = _featuredBasePos + new Vector2(offsetX, 0f);

            Color c = featuredImage.color;
            c.a = _featuredBaseAlpha * alpha01;
            featuredImage.color = c;
        }
    }
}