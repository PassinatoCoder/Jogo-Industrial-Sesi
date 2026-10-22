using UnityEngine;

namespace Core.UI.Menu
{
    public class MenuParallax : MonoBehaviour
    {
        [Header("Parallax Settings")]
        [Tooltip("Transform do elemento de fundo (RectTransform). Se vazio, pega o próprio objeto.")]
        [SerializeField] private RectTransform backgroundTransform;

        [Tooltip("Quão longe a imagem vai ao mover o mouse.")]
        [SerializeField] private float movementMultiplier = 15f;

        [Tooltip("Velocidade e suavidade do arrasto.")]
        [SerializeField] private float smoothSpeed = 5f;

        private Vector2 _startPosition;

        private void Awake()
        {
            if (backgroundTransform == null)
            {
                backgroundTransform = GetComponent<RectTransform>();
            }

            _startPosition = backgroundTransform.anchoredPosition;
        }

        private void Update()
        {
            // Normaliza a posição do mouse de -1 (Esquerda/Baixo) até 1 (Direita/Cima)
            Vector2 mousePos = Input.mousePosition;
            float offsetX = (mousePos.x / Screen.width) * 2f - 1f;
            float offsetY = (mousePos.y / Screen.height) * 2f - 1f;

            // Calcula a nova posição baseada no deslocamento do mouse
            Vector2 targetPos = _startPosition + new Vector2(offsetX * movementMultiplier, offsetY * movementMultiplier);

            // Suaviza a movimentação usando unscaledDeltaTime para funcionar mesmo com o jogo pausado
            backgroundTransform.anchoredPosition = Vector2.Lerp(
                backgroundTransform.anchoredPosition,
                targetPos,
                Time.unscaledDeltaTime * smoothSpeed
            );
        }
    }
}