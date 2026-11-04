using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections;

namespace Core.UI.Menu
{
    public class TabletOptionsManager : MonoBehaviour
    {
        [Header("Navegação do Tablet")]
        [SerializeField] private CanvasGroup hubGroup;
        [SerializeField] private CanvasGroup[] subPanels;
        [SerializeField] private MainMenuController menuController;

        [Header("Transição (Fade to Black)")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float fadeDuration = 0.3f;

        private CanvasGroup _currentActivePanel = null;
        private bool _isAnimating = false;

        [Header("Áudio")]
        [SerializeField] private AudioMixer mainMixer;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        private void Start()
        {
            SetCanvasState(hubGroup, 1f, true);
            foreach (var panel in subPanels) SetCanvasState(panel, 0f, false);
            LoadAudioSettings();
        }

        public void OpenSubPanel(CanvasGroup targetPanel)
        {
            if (_isAnimating || targetPanel == null) return;
            StartCoroutine(FadeToBlackTransition(hubGroup, targetPanel));
            _currentActivePanel = targetPanel;
        }

        public void Btn_TabletBack()
        {
            if (_isAnimating) return;

            if (_currentActivePanel != null)
            {
                StartCoroutine(FadeToBlackTransition(_currentActivePanel, hubGroup));
                _currentActivePanel = null;
            }
            else if (menuController != null)
            {
                menuController.Btn_BackToMenu();
            }
        }

        private IEnumerator FadeToBlackTransition(CanvasGroup outGroup, CanvasGroup inGroup)
        {
            _isAnimating = true;
            SetCanvasState(outGroup, outGroup.alpha, false);
            SetCanvasState(inGroup, inGroup.alpha, false);

            float elapsed = 0f;
            while (elapsed < fadeDuration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Lerp(0f, 1f, elapsed / (fadeDuration / 2f));
                yield return null;
            }

            outGroup.alpha = 0f;
            inGroup.alpha = 1f;

            elapsed = 0f;
            while (elapsed < fadeDuration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Lerp(1f, 0f, elapsed / (fadeDuration / 2f));
                yield return null;
            }

            SetCanvasState(inGroup, 1f, true);
            _isAnimating = false;
        }

        private void SetCanvasState(CanvasGroup group, float alpha, bool interactable)
        {
            if (group == null) return;
            group.alpha = alpha;
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }

        private void SetMixerVolume(string exposedParam, float sliderValue)
        {
            if (mainMixer == null) return;
            float db = sliderValue > 0.001f ? Mathf.Log10(sliderValue) * 20f : -80f;
            mainMixer.SetFloat(exposedParam, db);
        }

        public void ApplyVolumeMaster(float value)
        {
            SetMixerVolume("MasterVol", value);
            PlayerPrefs.SetFloat("Config_VolumeMaster", value);
            PlayerPrefs.Save();
        }

        public void ApplyVolumeBGM(float value)
        {
            SetMixerVolume("BGMVol", value);
            PlayerPrefs.SetFloat("Config_VolumeBGM", value);
            PlayerPrefs.Save();
        }

        public void ApplyVolumeSFX(float value)
        {
            SetMixerVolume("SFXVol", value);
            PlayerPrefs.SetFloat("Config_VolumeSFX", value);
            PlayerPrefs.Save();
        }

        private void LoadAudioSettings()
        {
            if (masterSlider != null)
            {
                float vol = PlayerPrefs.GetFloat("Config_VolumeMaster", 1f);
                masterSlider.value = vol;
                SetMixerVolume("MasterVol", vol);
            }
            if (bgmSlider != null)
            {
                float vol = PlayerPrefs.GetFloat("Config_VolumeBGM", 1f);
                bgmSlider.value = vol;
                SetMixerVolume("BGMVol", vol);
            }
            if (sfxSlider != null)
            {
                float vol = PlayerPrefs.GetFloat("Config_VolumeSFX", 1f);
                sfxSlider.value = vol;
                SetMixerVolume("SFXVol", vol);
            }
        }
    }
}