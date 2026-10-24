using UnityEngine;

namespace Core.Camera
{
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("Arraste a protagonista para este campo")]
        [SerializeField] private Transform target;

        [Tooltip("Velocidade da suavização (maior = mais rápido)")]
        [SerializeField] private float smoothSpeed = 5f;

        [Tooltip("Distância da câmera. O Z DEVE ser -10 para não sumir com o jogo 2D")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);

        private void LateUpdate()
        {
            if (target == null) return;

            // Calcula a posição desejada
            Vector3 desiredPosition = target.position + offset;

            // Move suavemente a câmera até o alvo
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }
}