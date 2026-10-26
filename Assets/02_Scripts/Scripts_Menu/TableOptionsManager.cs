using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace Core.UI.Menu
{
    public class TabletOptionsManager : MonoBehaviour
    {
        [Header("Navegação do Tablet (RDR2 Style)")]
        [SerializeField] private CanvasGroup hubGroup;
        [SerializeField] private CanvasGroup[] subPanels;
        [SerializeField] private MainMenuController menuController;

        [Header("Transição (Fade to Black)")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float fadeDuration = 0.3f;

        private CanvasGroup _currentActivePanel = null;
        private bool _isAnimating = false;

        [Header("Referências - Vídeo (Básico)")]
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vSyncToggle;

        [Header("Referências - Vídeo (Desempenho)")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown fpsCapDropdown;
        [SerializeField] private Toggle fpsCounterToggle;
        [SerializeField] private GameObject fpsCounterUIObject; // Arraste o GameObject que contém o script FPSDisplay
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TextMeshProUGUI countdownText;

        private Resolution[] _resolutions;
        private int _currentResolutionIndex;
        private int _previousResolutionIndex;
        private Coroutine _revertCoroutine;

        [Header("Referências - Áudio")]
        [SerializeField] private AudioMixer mainMixer;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        private void Start()
        {
            SetCanvasState(hubGroup, 1f, true);
            foreach (var panel in subPanels) SetCanvasState(panel, 0f, false);

            LoadSettings();
            InitializeResolutions();
        }

        // --- SISTEMA DE NAVEGAÇÃO ---
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
                elapsed += Time.deltaTime;
                fadeOverlay.alpha = Mathf.Lerp(0f, 1f, elapsed / (fadeDuration / 2f));
                yield return null;
            }

            outGroup.alpha = 0f;
            inGroup.alpha = 1f;

            elapsed = 0f;
            while (elapsed < fadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
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

        // --- APLICAÇÃO DE VÍDEO (Básico e Desempenho) ---
        public void ApplyFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt("Config_Fullscreen", isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ApplyVSync(bool isVSync)
        {
            QualitySettings.vSyncCount = isVSync ? 1 : 0;
            PlayerPrefs.SetInt("Config_VSync", isVSync ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ApplyFPSCounter(bool showFPS)
        {
            if (fpsCounterUIObject != null) fpsCounterUIObject.SetActive(showFPS);
            PlayerPrefs.SetInt("Config_FPSCounter", showFPS ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ApplyFPSCap(int index)
        {
            // O Dropdown deve ter estas opções na mesma ordem: 0 = 30, 1 = 60, 2 = 120, 3 = Ilimitado
            int targetFPS = -1;
            switch (index)
            {
                case 0: targetFPS = 30; break;
                case 1: targetFPS = 60; break;
                case 2: targetFPS = 120; break;
                case 3: targetFPS = -1; break;
            }

            Application.targetFrameRate = targetFPS;
            PlayerPrefs.SetInt("Config_FPSCapIndex", index);
            PlayerPrefs.Save();
        }

        // --- SISTEMA DE RESOLUÇÃO ---
        private void InitializeResolutions()
        {
            if (resolutionDropdown == null) return;

            _resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int savedResIndex = PlayerPrefs.GetInt("Config_ResolutionIndex", -1);
            int currentResIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                string option = $"{_resolutions[i].width} x {_resolutions[i].height} @ {_resolutions[i].refreshRateRatio.value}hz";
                options.Add(option);

                if (savedResIndex == -1 && _resolutions[i].width == Screen.currentResolution.width && _resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResIndex = i;
                }
                else if (i == savedResIndex)
                {
                    currentResIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResIndex;
            resolutionDropdown.RefreshShownValue();

            _currentResolutionIndex = currentResIndex;
            _previousResolutionIndex = currentResIndex;

            resolutionDropdown.onValueChanged.AddListener(OnResolutionDropdownChanged);
        }

        private void OnResolutionDropdownChanged(int newIndex)
        {
            _previousResolutionIndex = _currentResolutionIndex;
            _currentResolutionIndex = newIndex;

            Resolution res = _resolutions[newIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);

            if (confirmationPanel != null) confirmationPanel.SetActive(true);
            if (_revertCoroutine != null) StopCoroutine(_revertCoroutine);
            _revertCoroutine = StartCoroutine(RevertCountdownRoutine());
        }

        private IEnumerator RevertCountdownRoutine()
        {
            int timer = 30;
            while (timer > 0)
            {
                if (countdownText != null) countdownText.text = $"Deseja manter as alterações?\nRevertendo em {timer}...";
                yield return new WaitForSecondsRealtime(1f);
                timer--;
            }
            Btn_RevertResolution();
        }

        public void Btn_ApplyResolution()
        {
            if (_revertCoroutine != null) StopCoroutine(_revertCoroutine);
            if (confirmationPanel != null) confirmationPanel.SetActive(false);

            PlayerPrefs.SetInt("Config_ResolutionIndex", _currentResolutionIndex);
            PlayerPrefs.Save();
        }

        public void Btn_RevertResolution()
        {
            if (_revertCoroutine != null) StopCoroutine(_revertCoroutine);
            if (confirmationPanel != null) confirmationPanel.SetActive(false);

            _currentResolutionIndex = _previousResolutionIndex;
            resolutionDropdown.value = _previousResolutionIndex;
            resolutionDropdown.RefreshShownValue();

            Resolution res = _resolutions[_previousResolutionIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        }

        // --- MOTOR LOGARÍTMICO DE ÁUDIO ---
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

        // --- CARREGAMENTO GLOBAL ---
        private void LoadSettings()
        {
            if (fullscreenToggle != null)
            {
                bool isFullscreen = PlayerPrefs.GetInt("Config_Fullscreen", 1) == 1;
                fullscreenToggle.isOn = isFullscreen;
                Screen.fullScreen = isFullscreen;
            }

            if (vSyncToggle != null)
            {
                bool isVSync = PlayerPrefs.GetInt("Config_VSync", 1) == 1;
                vSyncToggle.isOn = isVSync;
                QualitySettings.vSyncCount = isVSync ? 1 : 0;
            }

            if (fpsCounterToggle != null)
            {
                bool showFPS = PlayerPrefs.GetInt("Config_FPSCounter", 0) == 1;
                fpsCounterToggle.isOn = showFPS;
                if (fpsCounterUIObject != null) fpsCounterUIObject.SetActive(showFPS);
            }

            if (fpsCapDropdown != null)
            {
                int capIndex = PlayerPrefs.GetInt("Config_FPSCapIndex", 1); // Padrão: 60 FPS (Index 1)
                fpsCapDropdown.value = capIndex;
                ApplyFPSCap(capIndex);
            }

            if (masterSlider != null)
            {
                float volMaster = PlayerPrefs.GetFloat("Config_VolumeMaster", 1f);
                masterSlider.value = volMaster;
                SetMixerVolume("MasterVol", volMaster);
            }

            if (bgmSlider != null)
            {
                float volBGM = PlayerPrefs.GetFloat("Config_VolumeBGM", 1f);
                bgmSlider.value = volBGM;
                SetMixerVolume("BGMVol", volBGM);
            }

            if (sfxSlider != null)
            {
                float volSFX = PlayerPrefs.GetFloat("Config_VolumeSFX", 1f);
                sfxSlider.value = volSFX;
                SetMixerVolume("SFXVol", volSFX);
            }
        }
    }
}