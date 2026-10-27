using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UI.Menu
{
    /// <summary>
    /// Visual de UM item do menu estilizado:
    /// o texto se inclina, cresce e desliza; uma mancha de tinta (troca de sprites) aparece atrás;
    /// uma faixa de destaque cresce; o subtítulo surge.
    ///
    /// Convive com o MenuButtonAnimator no mesmo botão:
    /// o MenuButtonAnimator cuida da mola/cor do botão, este script cuida da "tinta" e da inclinação.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class StyleMenuItem : MonoBehaviour,
        ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Texto")]
        [Tooltip("RectTransform do texto do botão (o TMP). Pivô sugerido: esquerda/centro (0, 0.5).")]
        [SerializeField] private RectTransform label;
        [Tooltip("Opcional: linha pequena que só aparece quando o item está selecionado.")]
        [SerializeField] private TMP_Text subtitle;

        [Header("Inclinação, tamanho e deslocamento do texto")]
        [Tooltip("Graus. Positivo = o texto sobe pra direita.")]
        [SerializeField] private float restingTilt = 5f;
        [SerializeField] private float selectedTilt = 2f;
        [SerializeField] private float restingScale = 1f;
        [SerializeField] private float selectedScale = 1.25f;
        [Tooltip("Quanto o texto desliza pra direita quando selecionado (pixels).")]
        [SerializeField] private float selectedOffsetX = 48f;

        [Header("Mancha de tinta (troca de sprites)")]
        [SerializeField] private Image splashImage;
        [Tooltip("Frames da animação, do menor ao maior. Todos do MESMO tamanho.")]
        [SerializeField] private Sprite[] splashFrames;
        [Tooltip("Velocidade da animação (7 = leva ~0,15 s).")]
        [SerializeField] private float splashSpeed = 7f;
        [Tooltip("A tinta some mais rápido do que aparece (1 = igual).")]
        [SerializeField] private float splashRetreatMultiplier = 1.8f;
        [SerializeField] private Color splashColor = Color.white;

        [Header("Faixa de destaque (opcional)")]
        [Tooltip("Barra que cresce da esquerda. Pivô à esquerda (0, 0.5).")]
        [SerializeField] private RectTransform accentStrip;

        [Header("Identidade deste item")]
        [Tooltip("Cor do fundo colorido quando este item está selecionado.")]
        [SerializeField] private Color accentColor = new Color(0.84f, 0.09f, 0.16f);
        [Tooltip("Arte grande ao lado quando este item está selecionado (opcional).")]
        [SerializeField] private Sprite featuredSprite;

        [Header("Mola")]
        [SerializeField] private float stiffness = 260f;
        [SerializeField] private float damping = 17f;

        /// <summary>Disparado quando este item recebe a seleção (mouse, teclado ou controle).</summary>
        public event Action<StyleMenuItem> Selected;

        public Color AccentColor => accentColor;
        public Sprite FeaturedSprite => featuredSprite;
        public bool IsSelected => _selected;

        private Selectable _selectable;
        private MenuSpring _tilt, _scale, _offsetX, _strip;
        private Vector2 _labelBasePos;
        private Vector3 _labelBaseScale = Vector3.one;
        private Vector2 _subtitleBasePos;
        private Vector3 _splashBaseScale = Vector3.one;
        private float _splashProgress;
        private float _subtitleAlpha;
        private int _frameIndex = -1;
        private bool _selected, _hovered, _pendingSelect;

        private bool IsActive => _selected && _selectable != null && _selectable.IsInteractable();

        // ---------- Ciclo de vida ----------

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();

            if (label != null)
            {
                _labelBasePos = label.anchoredPosition;
                _labelBaseScale = label.localScale;
            }

            if (subtitle != null) _subtitleBasePos = subtitle.rectTransform.anchoredPosition;

            if (splashImage != null)
            {
                _splashBaseScale = splashImage.rectTransform.localScale;
                splashImage.color = splashColor;
            }
        }

        private void OnEnable()
        {
            EventSystem es = EventSystem.current;
            _selected = es != null && es.currentSelectedGameObject == gameObject;
            SnapToState();
        }

        private void OnDisable()
        {
            _hovered = false;
            _pendingSelect = false;
            _selected = false;
            SnapToState();
        }

        private void Update()
        {
            // Passar o mouse = selecionar (assim só UM item fica em destaque).
            // Feito aqui (e não direto no evento) pra não brigar com a ordem dos outros scripts do botão.
            if (_pendingSelect)
            {
                _pendingSelect = false;
                SelectFromHover();
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            if (dt <= 0f) return;

            if (MenuMotion.ReduceMotion)
            {
                SnapToState();
                return;
            }

            bool active = IsActive;
            _tilt.Step(active ? selectedTilt : restingTilt, stiffness, damping, dt);
            _scale.Step(active ? selectedScale : restingScale, stiffness, damping, dt);
            _offsetX.Step(active ? selectedOffsetX : 0f, stiffness, damping, dt);
            _strip.Step(active ? 1f : 0f, stiffness * 0.8f, damping, dt);

            float target = active ? 1f : 0f;
            float rate = splashSpeed * (active ? 1f : splashRetreatMultiplier);
            _splashProgress = Mathf.MoveTowards(_splashProgress, target, rate * dt);
            _subtitleAlpha = Mathf.MoveTowards(_subtitleAlpha, target, 8f * dt);

            ApplyAll();
        }

        // ---------- Eventos ----------

        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
            Selected?.Invoke(this);
        }

        public void OnDeselect(BaseEventData eventData) => _selected = false;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            _pendingSelect = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pendingSelect = false;
        }

        private void SelectFromHover()
        {
            if (!_hovered || _selectable == null || !_selectable.IsInteractable()) return;

            EventSystem es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != gameObject)
                es.SetSelectedGameObject(gameObject);
        }

        // ---------- Aplicação visual ----------

        private void SnapToState()
        {
            bool active = IsActive;
            _tilt.Snap(active ? selectedTilt : restingTilt);
            _scale.Snap(active ? selectedScale : restingScale);
            _offsetX.Snap(active ? selectedOffsetX : 0f);
            _strip.Snap(active ? 1f : 0f);
            _splashProgress = active ? 1f : 0f;
            _subtitleAlpha = active ? 1f : 0f;
            ApplyAll();
        }

        private void ApplyAll()
        {
            ApplyLabel();
            ApplyStrip();
            ApplySubtitle();
            ApplySplash();
        }

        private void ApplyLabel()
        {
            if (label == null) return;

            label.localRotation = Quaternion.Euler(0f, 0f, _tilt.Value);
            label.localScale = _labelBaseScale * _scale.Value;
            label.anchoredPosition = _labelBasePos + new Vector2(_offsetX.Value, 0f);
        }

        private void ApplyStrip()
        {
            if (accentStrip == null) return;

            Vector3 s = accentStrip.localScale;
            s.x = Mathf.Max(0f, _strip.Value);
            accentStrip.localScale = s;
        }

        private void ApplySubtitle()
        {
            if (subtitle == null) return;

            subtitle.alpha = _subtitleAlpha;
            float eased = MenuEasing.OutCubic(_subtitleAlpha);
            subtitle.rectTransform.anchoredPosition = _subtitleBasePos + new Vector2(-28f * (1f - eased), 0f);
        }

        private void ApplySplash()
        {
            if (splashImage == null) return;

            bool visible = _splashProgress > 0.001f;
            if (splashImage.enabled != visible) splashImage.enabled = visible;
            if (!visible) return;

            float eased = MenuEasing.OutCubic(_splashProgress);

            if (splashFrames != null && splashFrames.Length > 0)
            {
                // Troca de sprite: o progresso escolhe qual frame mostrar
                int index = Mathf.Clamp(Mathf.RoundToInt(eased * (splashFrames.Length - 1)), 0, splashFrames.Length - 1);
                if (index != _frameIndex)
                {
                    _frameIndex = index;
                    splashImage.sprite = splashFrames[index];
                }
            }
            else
            {
                // Sem frames: a imagem cresce da esquerda (pivô esquerdo)
                Vector3 s = _splashBaseScale;
                s.x *= eased;
                splashImage.rectTransform.localScale = s;
            }
        }
    }
}