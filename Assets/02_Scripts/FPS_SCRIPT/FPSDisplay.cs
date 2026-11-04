using UnityEngine;
using TMPro;

namespace Core.Systems
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FPSDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _fpsText;
        private float _timer;
        private const float RefreshRate = 0.5f;

        private void Awake()
        {
            _fpsText = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (Time.unscaledTime > _timer)
            {
                int fps = (int)(1f / Time.unscaledDeltaTime);
                _fpsText.text = $"FPS: {fps}";
                _timer = Time.unscaledTime + RefreshRate;
            }
        }
    }
}