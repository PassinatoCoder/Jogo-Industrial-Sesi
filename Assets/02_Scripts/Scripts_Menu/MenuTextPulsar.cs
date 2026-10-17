using UnityEngine;
using TMPro;

namespace Core.UI.Menu
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class MenuTextPulsar : MonoBehaviour
    {
        [Header("Pulse Settings")]
        [SerializeField] private float speed = 2f;
        [SerializeField] private float minAlpha = 0.2f;
        [SerializeField] private float maxAlpha = 1.0f;

        private TextMeshProUGUI _text;
        private Color _baseColor;

        private void Start()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _baseColor = _text.color;
        }

        private void Update()
        {
            // Calcula o pulso entre 0 e 1 usando o Seno do tempo
            float pulse = (Mathf.Sin(Time.time * speed) + 1f) / 2f;

            // Interpola entre o Alpha mínimo e máximo
            float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);

            _text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, currentAlpha);
        }
    }
}