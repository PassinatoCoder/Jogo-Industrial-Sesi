using TMPro;
using Unity.VisualScripting;
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

        [Header("Escala")]
        [SerializeField] private float hoverScale = 1.12f;
        [SerializeField] private float pressedScale = 0.92f;
        [Tooltip("Squash & stretch: achata ao clicar e estica ao soltar")]
        [SerializeField, Range(0f, 0.4f)] private float squashAmount = 0.12f;

        [Header("Mola (feeling cartoon)")]
        [Tooltip("Maior = mais rápida")]
        [SerializeField] private float stiffness = 380f;
        [Tooltip("Menor = mais elástica / balança mais")]
        [SerializeField] private float damping = 16f;
        [SerializeField] private float hoverPopImpulse = 2.5f;
        [SerializeField] private float releaseImpulse = 3f;
        [Tooltip("Balanço (graus/seg) ao passar o mouse")]
        [SerializeField] private float wiggleImpulse = 100f;

        [Header("Cores")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 0.82f, 0.2f);
        [SerializeField] private float colorSpeed = 14f;

        [Header("Eventos (ligue o áudio aqui)")]
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
        private Spring _scaleX, _scaleY, _rotation;
        private bool _hover, _selected, _pressed, _wiggleFlip;

        private bool Interactable => _selectable == null || _selectable.IsInteractable();

        private void Awake()
        {
            if (targetTransform == null) targetTransform = transform;
            _selectable = GetComponent<Selectable>();
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

            bool active = Interactable && (_hover || _selected);
            bool pressed = Interactable && _pressed;

            float scale = pressed ? pressedScale : (active ? hoverScale : 1f);
            float squash = pressed ? squashAmount : 0f;
            float targetX = scale * (1f + squash);
            float targetY = scale * (1f - squash);

            int steps = Mathf.CeilToInt(dt / MaxSubStep);
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                _scaleX.Step(targetX, stiffness, damping, h);
                _scaleY.Step(targetY, stiffness, damping, h);
                _rotation.Step(0f, stiffness, damping, h);
            }

            targetTransform.localScale = new Vector3(_scaleX.Value, _scaleY.Value, 1f);
            targetTransform.localRotation = Quaternion.Euler(0f, 0f, _rotation.Value);

            if (buttonText != null)
            {
                Color target = active ? hoverColor : normalColor;
                float k = 1f - Mathf.Exp(-colorSpeed * dt); // lerp independente de FPS
                buttonText.color = Color.Lerp(buttonText.color, target, k);
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
            if (MenuMotion.ReduceMotion) return;
            _scaleX.Velocity += hoverPopImpulse;
            _scaleY.Velocity += hoverPopImpulse;
            _rotation.Velocity += wiggleImpulse * (_wiggleFlip ? 1f : -1f);
            _wiggleFlip = !_wiggleFlip;
        }

        private void Release()
        {
            if (MenuMotion.ReduceMotion) return;
            _scaleY.Velocity += releaseImpulse;
            _scaleX.Velocity -= releaseImpulse * 0.6f;
        }

        private void ResetState()
        {
            _scaleX.Reset(1f);
            _scaleY.Reset(1f);
            _rotation.Reset(0f);

            if (targetTransform != null)
            {
                targetTransform.localScale = Vector3.one;
                targetTransform.localRotation = Quaternion.identity;
            }
            if (buttonText != null) buttonText.color = normalColor;
        }
    }
}