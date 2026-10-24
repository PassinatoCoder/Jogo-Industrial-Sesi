using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UI.Menu
{
    public class MenuButtonAnimator : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [Header("Referências")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private TextMeshProUGUI buttonText;
        [Tooltip("Opcional: Imagem de fundo do botão para também receber feedback de cor.")]
        [SerializeField] private Graphic backgroundGraphic;

        [Header("Escala e Elevação")]
        [SerializeField] private float hoverScale = 1.12f;
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField, Range(0f, 0.4f)] private float squashAmount = 0.12f;
        [Tooltip("Move o botão no eixo Y ao passar o mouse (eleva) e clicar (afunda).")]
        [SerializeField] private float hoverElevation = 2f;
        [SerializeField] private float pressedElevation = -3f;

        [Header("Espaçamento de Texto (Tracking)")]
        [SerializeField] private float normalCharacterSpacing = 0f;
        [SerializeField] private float hoverCharacterSpacing = 2.5f;

        [Header("Mola (Feeling Tátil/Cartoon)")]
        [SerializeField] private float stiffness = 380f;
        [SerializeField] private float damping = 16f;
        [SerializeField] private float hoverPopImpulse = 2.5f;
        [SerializeField] private float releaseImpulse = 3f;
        [SerializeField] private float wiggleImpulse = 100f;

        [Header("Cores e Estados")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 0.82f, 0.2f);
        [SerializeField] private Color disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        [SerializeField] private float colorSpeed = 14f;

        [Header("Eventos")]
        [SerializeField] private UnityEvent onHover;
        [SerializeField] private UnityEvent onPress;

        private struct Spring
        {
            public float Value;
            public float Velocity;

            public void Step(float target, float stiffness, float damping, float dt)
            {
                float force = (target - Value) * stiffness - Velocity * damping;
                Velocity += force * dt;
                Value += Velocity * dt;
            }

            public void Reset(float value)
            {
                Value = value;
                Velocity = 0f;
            }
        }

        private const float MaxFrameTime = 0.033f;
        private const float MaxSubStep = 0.008f;

        private Selectable _selectable;
        private Spring _scaleX, _scaleY, _rotation, _elevation;
        private bool _hover, _selected, _pressed, _wiggleFlip;

        private Vector3 _basePosition;
        private bool Interactable => _selectable == null || _selectable.IsInteractable();

        private void Awake()
        {
            if (targetTransform == null) targetTransform = transform;
            _selectable = GetComponent<Selectable>();
            _basePosition = targetTransform.localPosition;
        }

        private void OnEnable() => ResetState();

        private void OnDisable()
        {
            _hover = _selected = _pressed = false;
            ResetState();
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxFrameTime);
            if (dt <= 0f) return;

            bool isInteractable = Interactable;
            bool active = isInteractable && (_hover || _selected);
            bool pressed = isInteractable && _pressed;

            // Alvos da mola (Escala)
            float scale = pressed ? pressedScale : (active ? hoverScale : 1f);
            float squash = pressed ? squashAmount : 0f;
            float targetX = scale * (1f + squash);
            float targetY = scale * (1f - squash);

            // Alvo da mola (Elevação/Y-Offset)
            float targetElevation = pressed ? pressedElevation : (active ? hoverElevation : 0f);

            int steps = Mathf.CeilToInt(dt / MaxSubStep);
            float h = dt / steps;

            for (int i = 0; i < steps; i++)
            {
                _scaleX.Step(targetX, stiffness, damping, h);
                _scaleY.Step(targetY, stiffness, damping, h);
                _rotation.Step(0f, stiffness, damping, h);
                _elevation.Step(targetElevation, stiffness, damping, h);
            }

            targetTransform.localScale = new Vector3(_scaleX.Value, _scaleY.Value, 1f);
            targetTransform.localRotation = Quaternion.Euler(0f, 0f, _rotation.Value);
            targetTransform.localPosition = _basePosition + new Vector3(0f, _elevation.Value, 0f);

            // Transições de Cor e Espaçamento de Texto
            UpdateVisuals(active, isInteractable, dt);
        }

        private void UpdateVisuals(bool active, bool isInteractable, float dt)
        {
            Color targetColor = normalColor;
            if (!isInteractable) targetColor = disabledColor;
            else if (active) targetColor = hoverColor;

            float k = 1f - Mathf.Exp(-colorSpeed * dt);

            if (buttonText != null)
            {
                buttonText.color = Color.Lerp(buttonText.color, targetColor, k);

                // Anima o espaçamento das letras suavemente
                float targetSpacing = active ? hoverCharacterSpacing : normalCharacterSpacing;
                buttonText.characterSpacing = Mathf.Lerp(buttonText.characterSpacing, targetSpacing, k);
            }

            if (backgroundGraphic != null)
            {
                backgroundGraphic.color = Color.Lerp(backgroundGraphic.color, targetColor, k);
            }
        }

        // ---------- Eventos ----------

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Interactable) return;
            _hover = true;
            Pop();
            onHover?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hover = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactable) return;
            _pressed = true;
            onPress?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed) return;
            _pressed = false;
            Release();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!Interactable) return;
            if (!_hover) { Pop(); onHover?.Invoke(); }
            _selected = true;
        }

        public void OnDeselect(BaseEventData eventData) => _selected = false;

        public void OnSubmit(BaseEventData eventData)
        {
            if (!Interactable) return;
            onPress?.Invoke();
            Release();
        }

        // ---------- Impulsos ----------

        private void Pop()
        {
            // Substitua MenuMotion.ReduceMotion pelo seu gerenciador de acessibilidade
            // se aplicável. Removido aqui para evitar erros de compilação sem o contexto original.
            _scaleX.Velocity += hoverPopImpulse;
            _scaleY.Velocity += hoverPopImpulse;
            _rotation.Velocity += wiggleImpulse * (_wiggleFlip ? 1f : -1f);
            _wiggleFlip = !_wiggleFlip;
        }

        private void Release()
        {
            _scaleY.Velocity += releaseImpulse;
            _scaleX.Velocity -= releaseImpulse * 0.6f;
            _elevation.Velocity -= releaseImpulse * 5f; // Dá um "tranco" pra baixo ao soltar
        }

        private void ResetState()
        {
            _scaleX.Reset(1f);
            _scaleY.Reset(1f);
            _rotation.Reset(0f);
            _elevation.Reset(0f);

            if (targetTransform != null)
            {
                targetTransform.localScale = Vector3.one;
                targetTransform.localRotation = Quaternion.identity;
                targetTransform.localPosition = _basePosition;
            }
            if (buttonText != null)
            {
                buttonText.color = normalColor;
                buttonText.characterSpacing = normalCharacterSpacing;
            }
            if (backgroundGraphic != null) backgroundGraphic.color = normalColor;
        }
    }
}