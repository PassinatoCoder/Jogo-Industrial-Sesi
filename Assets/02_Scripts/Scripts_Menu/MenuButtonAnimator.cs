using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Core.UI.Menu
{
    public class MenuButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Animation Settings")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float clickScale = 0.95f;
        [SerializeField] private float smoothSpeed = 15f;

        [Header("Color Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(0.8f, 0.6f, 0f); // Dourado escuro / Amarelo Blasphemous

        private Vector3 _targetScale = Vector3.one;
        private Color _targetColor;

        private void Start()
        {
            if (targetTransform == null) targetTransform = transform;
            _targetColor = normalColor;
            if (buttonText != null) buttonText.color = normalColor;
        }

        private void Update()
        {
            // Executa a transição matemática em todo frame (Smooth)
            targetTransform.localScale = Vector3.Lerp(targetTransform.localScale, _targetScale, Time.deltaTime * smoothSpeed);

            if (buttonText != null)
            {
                buttonText.color = Color.Lerp(buttonText.color, _targetColor, Time.deltaTime * smoothSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _targetScale = Vector3.one * hoverScale;
            _targetColor = hoverColor;
            // Dica de Ouro: Aqui você pode colocar um AudioManager.PlaySound("Hover");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetScale = Vector3.one;
            _targetColor = normalColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _targetScale = Vector3.one * clickScale; // Afunda o botão ao clicar
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _targetScale = Vector3.one * hoverScale; // Volta ao tamanho do Hover
        }
    }
}