using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UI.Menu
{
    /// <summary>
    /// Maestro do menu estilizado (coloque no MenuContainer):
    /// - mantém SEMPRE um item selecionado (teclado e controle nunca ficam "sem foco");
    /// - muda a cor do painel colorido (com uma "batida") conforme o item selecionado;
    /// - troca o FUNDO da tela inteira (slide + zoom + fade) conforme o item selecionado;
    /// - troca a arte lateral (com deslize) conforme o item selecionado.
    /// Mouse, setas do teclado e controle disparam a mesma coisa (selecionar = trocar).
    /// </summary>
    public class StyleMenuController : MonoBehaviour
    {
        [Header("Itens")]
        [Tooltip("Vazio = pega todos os StyleMenuItem que são filhos deste objeto.")]
        [SerializeField] private StyleMenuItem[] items;
        [Tooltip("O MESMO CanvasGroup do 'Buttons Group' do MainMenuController (liga/desliga o menu).")]
        [SerializeField] private CanvasGroup menuGroup;

        [Header("Painel colorido (muda de cor com o item selecionado)")]
        [SerializeField] private Graphic colorWash;
        [SerializeField] private float washColorSpeed = 9f;
        [Tooltip("'Batida' do painel a cada troca de item (0 = desligado). Pivô do ColorWash à esquerda.")]
        [SerializeField] private float washPunchImpulse = 1.2f;

        [Header("Fundo dinâmico (troca de imagem com o item selecionado)")]
        [Tooltip("Camada de BAIXO: mostra o fundo atual.")]
        [SerializeField] private Image backgroundBack;
        [Tooltip("Camada de CIMA: recebe o fundo novo e anima por cima. Mesmo tamanho/âncoras da de baixo.")]
        [SerializeField] private Image backgroundFront;
        [Tooltip("Duração da troca (segundos).")]
        [SerializeField] private float backgroundTime = 0.5f;
        [Tooltip("De quantos pixels a imagem nova vem deslizando.")]
        [SerializeField] private float backgroundSlide = 140f;
        [Tooltip("A imagem nova começa X% maior e assenta (0.06 = 6%).")]
        [SerializeField] private float backgroundZoom = 0.06f;
        [Tooltip("'Respiração' lenta do fundo (0 = parado). Ex.: 0.025")]
        [SerializeField] private float backgroundIdleZoom = 0.025f;
        [SerializeField] private float backgroundIdleSpeed = 0.4f;

        [Header("Arte lateral (troca com o item selecionado)")]
        [SerializeField] private Image featuredImage;
        [SerializeField] private float featuredSlide = 120f;
        [SerializeField] private float featuredOutTime = 0.12f;
        [SerializeField] private float featuredInTime = 0.4f;

        private StyleMenuItem _last;

        // painel colorido
        private Color _washTarget = Color.white;
        private float _washAlpha = 1f;
        private Vector3 _washBaseScale = Vector3.one;
        private MenuSpring _washPunch;

        // fundo dinâmico
        private Sprite _backgroundCurrent;
        private Coroutine _backgroundRoutine;
        private Vector2 _backgroundFrontBasePos;
        private float _backgroundTime;
        private float _frontScale = 1f;

        // arte lateral
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

            if (colorWash != null)
            {
                _washAlpha = colorWash.color.a;
                _washBaseScale = colorWash.rectTransform.localScale;
            }

            if (backgroundFront != null)
                _backgroundFrontBasePos = backgroundFront.rectTransform.anchoredPosition;

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

            if (_backgroundRoutine != null)
            {
                StopCoroutine(_backgroundRoutine);
                _backgroundRoutine = null;
                CommitBackground(_backgroundCurrent);
            }

            if (_featuredRoutine != null)
            {
                StopCoroutine(_featuredRoutine);
                _featuredRoutine = null;
            }
        }

        private void Start()
        {
            // Usa o primeiro item mesmo com o menu ainda travado (fase "Pressione qualquer tecla"),
            // assim tudo já nasce na cor/imagem certa e não "pisca" na primeira seleção.
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

            ShowBackgroundInstant(first.BackgroundSprite);
            ShowFeaturedInstant(first.FeaturedSprite);
        }

        private void LateUpdate()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            bool reduce = MenuMotion.ReduceMotion;

            if (colorWash != null)
            {
                float k = 1f - Mathf.Exp(-washColorSpeed * dt);
                colorWash.color = reduce ? _washTarget : Color.Lerp(colorWash.color, _washTarget, k);

                if (reduce) _washPunch.Snap(0f);
                else if (dt > 0f) _washPunch.Step(0f, 220f, 14f, dt);

                Vector3 s = _washBaseScale;
                s.x *= 1f + _washPunch.Value;
                colorWash.rectTransform.localScale = s;
            }

            ApplyBackgroundScales(reduce);
            EnsureSelection();
        }

        // ---------- Seleção ----------

        private void OnItemSelected(StyleMenuItem item)
        {
            _last = item;

            _washTarget = item.AccentColor;
            _washTarget.a = _washAlpha;
            if (!MenuMotion.ReduceMotion) _washPunch.Velocity += washPunchImpulse;

            if (item.BackgroundSprite != null && item.BackgroundSprite != _backgroundCurrent)
                SwapBackground(item.BackgroundSprite);

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

        // ---------- Fundo dinâmico ----------

        private void ShowBackgroundInstant(Sprite sprite)
        {
            _backgroundCurrent = sprite;
            CommitBackground(sprite);
        }

        private void SwapBackground(Sprite next)
        {
            _backgroundCurrent = next;
            if (backgroundBack == null) return;

            // Sem camada de cima, ou "Reduzir movimento": troca direto
            if (backgroundFront == null || !isActiveAndEnabled || MenuMotion.ReduceMotion)
            {
                if (_backgroundRoutine != null)
                {
                    StopCoroutine(_backgroundRoutine);
                    _backgroundRoutine = null;
                }
                CommitBackground(next);
                return;
            }

            // Troca no meio de outra troca (mouse passando rápido): só muda a imagem que está chegando
            // e reinicia o "assentamento", sem piscar.
            if (_backgroundRoutine != null)
            {
                backgroundFront.sprite = next;
                _backgroundTime = Mathf.Min(_backgroundTime, Mathf.Max(0.01f, backgroundTime) * 0.35f);
                return;
            }

            _backgroundTime = 0f;
            _backgroundRoutine = StartCoroutine(SwapBackgroundRoutine(next));
        }

        private IEnumerator SwapBackgroundRoutine(Sprite first)
        {
            backgroundFront.sprite = first;
            backgroundFront.enabled = true;
            SetFrontPose(backgroundSlide, 0f, 1f + backgroundZoom);

            float duration = Mathf.Max(0.01f, backgroundTime);
            while (_backgroundTime < duration)
            {
                _backgroundTime += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float x = Mathf.Clamp01(_backgroundTime / duration);
                float eased = MenuEasing.OutCubic(x);

                SetFrontPose(
                    backgroundSlide * (1f - eased),
                    MenuEasing.OutCubic(Mathf.Clamp01(x * 2.2f)),
                    1f + backgroundZoom * (1f - eased));

                yield return null;
            }

            _backgroundRoutine = null;
            CommitBackground(_backgroundCurrent);
        }

        /// <summary>A imagem nova vira o fundo de baixo e a camada de cima se esconde.</summary>
        private void CommitBackground(Sprite sprite)
        {
            if (backgroundBack != null)
            {
                backgroundBack.sprite = sprite;
                backgroundBack.enabled = sprite != null;
            }

            if (backgroundFront != null)
            {
                backgroundFront.enabled = false;
                SetFrontPose(0f, 0f, 1f);
            }
        }

        private void SetFrontPose(float offsetX, float alpha, float scale)
        {
            if (backgroundFront == null) return;

            _frontScale = scale;
            backgroundFront.rectTransform.anchoredPosition = _backgroundFrontBasePos + new Vector2(offsetX, 0f);

            Color c = backgroundFront.color;
            c.a = alpha;
            backgroundFront.color = c;
        }

        /// <summary>"Respiração" lenta do fundo + escala da troca (as duas camadas respiram juntas).</summary>
        private void ApplyBackgroundScales(bool reduce)
        {
            float idle = 1f;
            if (!reduce && backgroundIdleZoom > 0f)
                idle += backgroundIdleZoom * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * backgroundIdleSpeed));

            if (backgroundBack != null)
                backgroundBack.rectTransform.localScale = new Vector3(idle, idle, 1f);

            if (backgroundFront != null)
            {
                float f = idle * _frontScale;
                backgroundFront.rectTransform.localScale = new Vector3(f, f, 1f);
            }
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