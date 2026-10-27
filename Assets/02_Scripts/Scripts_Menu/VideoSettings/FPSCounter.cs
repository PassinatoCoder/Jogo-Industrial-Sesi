using System;
using Core.Settings;
using TMPro;
using UnityEngine;

namespace Core.UI.Menu
{
    [RequireComponent(typeof(TMP_Text))]
    public class FpsCounter : MonoBehaviour
    {
        [SerializeField] private float refreshInterval = 0.5f;

        private TMP_Text _text;
        private float _accumTime;
        private int _accumFrames;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            VideoSettingsManager.SettingsApplied += CheckVisibility;
            CheckVisibility();
        }

        private void OnDisable() => VideoSettingsManager.SettingsApplied -= CheckVisibility;

        private void CheckVisibility()
        {
            _text.enabled = VideoSettingsManager.Instance != null && VideoSettingsManager.Instance.Current.showFps;
        }

        private void Update()
        {
            if (!_text.enabled || Time.unscaledDeltaTime <= 0f) return;

            _accumTime += Time.unscaledDeltaTime;
            _accumFrames++;

            if (_accumTime >= refreshInterval)
            {
                _text.text = $"{Mathf.RoundToInt(_accumFrames / _accumTime)} FPS";
                _accumTime = 0f;
                _accumFrames = 0;
            }
        }
    }
}