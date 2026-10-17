using UnityEngine;

namespace Core.UI.Menu
{
    public class MenuParallax : MonoBehaviour
    {
        [Header("Parallax Settings")]
        [SerializeField] private RectTransform backgroundTransform;
        [SerializeField] private float movementMultiplier = 15f; // Quão longe a imagem vai
        [SerializeField] private float smoothSpeed = 5f; // Suavidade do arrasto

        private Vector2 _startPosition;

        private void Start()
        {
            if (backgroundTransform == null) backgroundTransform = GetComponent<RectTransform>();
            _startPosition = backgroundTransform.anchoredPosition;
        }

        private void Update()
        {
            // Normaliza a posição do mouse de -1 (Esquerda/Baixo) até 1 (Direita/Cima)
            Vector2 mousePos = Input.mousePosition;
            float offsetX = (mousePos.x / Screen.width) * 2f - 1f;
            float offsetY = (mousePos.y / Screen.height) * 2f - 1f;

            // Calcula a nova posição e suaviza via Lerp
            Vector2 targetPos = _startPosition + new Vector2(offsetX * movementMultiplier, offsetY * movementMultiplier);
            backgroundTransform.anchoredPosition = Vector2.Lerp(backgroundTransform.anchoredPosition, targetPos, Time.deltaTime * smoothSpeed);
        }
    }
}