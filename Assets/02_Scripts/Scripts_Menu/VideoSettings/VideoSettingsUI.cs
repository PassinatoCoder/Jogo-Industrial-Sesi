using System; // <-- A correção do erro do console está aqui
using System.Collections;
using System.Collections.Generic;
using Core.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UI.Menu
{
    public class VideoSettingsUI : MonoBehaviour
    {
        [Header("Tela")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown displayModeDropdown;

        [Header("Desempenho")]
        [SerializeField] private Toggle vSyncToggle;
        [SerializeField] private TMP_Dropdown fpsLimitDropdown;
        [SerializeField] private Toggle showFpsToggle;

        [Header("Botões do Rodapé (Ação Geral)")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button revertButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text hintText;

        [Header("Painel de Segurança (Prevenção da Resolução)")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private Button keepButton;
        [SerializeField] private Button undoButton;

        private VideoSettingsManager _manager;
        private VideoSettingsData _pendingData;
        private VideoSettingsData _backupData;
        private Coroutine _confirmRoutine;

        private void Awake()
        {
            _manager = VideoSettingsManager.Instance;
            if (_manager == null) { enabled = false; return; }

            SetupDropdowns();

            // Amarra os botões sem precisar configurar eventos no Inspector
            resolutionDropdown.onValueChanged.AddListener(val => { _pendingData.width = _manager.Resolutions[val].x; _pendingData.height = _manager.Resolutions[val].y; CheckChanges(); });
            displayModeDropdown.onValueChanged.AddListener(val => { _pendingData.displayMode = val; CheckChanges(); });
            vSyncToggle.onValueChanged.AddListener(val => { _pendingData.vSync = val; CheckChanges(); });
            fpsLimitDropdown.onValueChanged.AddListener(val => { _pendingData.fpsLimit = VideoSettingsManager.FpsLimits[val]; CheckChanges(); });
            showFpsToggle.onValueChanged.AddListener(val => { _pendingData.showFps = val; CheckChanges(); });

            applyButton.onClick.AddListener(ApplyChanges);
            revertButton.onClick.AddListener(DiscardPendingChanges);
            resetButton.onClick.AddListener(ResetToDefault);

            keepButton.onClick.AddListener(ConfirmDisplayChanges);
            undoButton.onClick.AddListener(UndoDisplayChanges);
        }

        private void OnEnable()
        {
            if (_manager == null) return;
            if (confirmPanel != null) confirmPanel.SetActive(false);
            DiscardPendingChanges();
        }

        private void SetupDropdowns()
        {
            var resList = new List<string>();
            foreach (var r in _manager.Resolutions) resList.Add($"{r.x} x {r.y}");
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(resList);

            displayModeDropdown.ClearOptions();
            displayModeDropdown.AddOptions(new List<string> { "Tela Cheia (Sem Borda)", "Tela Cheia (Exclusiva)", "Janela" });

            fpsLimitDropdown.ClearOptions();
            fpsLimitDropdown.AddOptions(new List<string> { "30 FPS", "60 FPS", "120 FPS", "Ilimitado" });
        }

        // --- LÓGICA DE ESTADO PENDENTE (O jogador mexe, mas não aplica ainda) ---
        private void DiscardPendingChanges()
        {
            StopConfirmRoutine();
            _pendingData = _manager.Current;
            RefreshUIValues();
        }

        private void ResetToDefault()
        {
            StopConfirmRoutine();
            _pendingData = _manager.GetDefaultSettings();
            RefreshUIValues();
        }

        private void CheckChanges()
        {
            bool hasChanged = !_pendingData.Equals(_manager.Current);
            applyButton.interactable = hasChanged;
            revertButton.interactable = hasChanged;

            fpsLimitDropdown.interactable = !_pendingData.vSync;

            if (hintText != null)
                hintText.text = _pendingData.vSync ? "VSync Ligado: O FPS acompanhará o seu monitor. Limite manual desativado." : "VSync Desligado: Limite manual de FPS ativado.";
        }

        private void RefreshUIValues()
        {
            resolutionDropdown.SetValueWithoutNotify(_manager.GetResolutionIndex(_pendingData.width, _pendingData.height));
            displayModeDropdown.SetValueWithoutNotify(_pendingData.displayMode);
            vSyncToggle.SetIsOnWithoutNotify(_pendingData.vSync);
            showFpsToggle.SetIsOnWithoutNotify(_pendingData.showFps);

            int limitIndex = Array.IndexOf(VideoSettingsManager.FpsLimits, _pendingData.fpsLimit);
            fpsLimitDropdown.SetValueWithoutNotify(limitIndex >= 0 ? limitIndex : 1);

            CheckChanges();
        }

        // --- SISTEMA DE APLICAÇÃO GERAL E PREVENÇÃO DE RESOLUÇÃO ---
        private void ApplyChanges()
        {
            if (_confirmRoutine != null) return;

            // Verifica se a resolução ou modo de janela sofreram alteração
            bool displayChanged = _pendingData.width != _manager.Current.width ||
                                  _pendingData.height != _manager.Current.height ||
                                  _pendingData.displayMode != _manager.Current.displayMode;

            _backupData = _manager.Current;
            _manager.ApplyToEngine(_pendingData); // Injeta tudo na engine de uma vez

            if (displayChanged)
            {
                // Abre a barreira de prevenção de 30 segundos
                _confirmRoutine = StartCoroutine(CountdownRoutine());
            }
            else
            {
                // Só mudou coisas simples (VSync, Mostrar FPS). Salva e encerra.
                _manager.Save();
                CheckChanges();
            }
        }

        private IEnumerator CountdownRoutine()
        {
            yield return null;
            confirmPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(keepButton.gameObject);

            float timer = 30f;
            while (timer > 0)
            {
                countdownText.text = $"Manter essas configurações?\nRevertendo em {Mathf.CeilToInt(timer)}s";
                timer -= Time.unscaledDeltaTime;
                yield return null;
            }

            UndoDisplayChanges(); // Tempo esgotado (jogador ficou com tela preta) -> Reverte sozinho
        }

        private void ConfirmDisplayChanges()
        {
            StopConfirmRoutine();
            _manager.Save();
            CheckChanges();
        }

        private void UndoDisplayChanges()
        {
            StopConfirmRoutine();
            _pendingData = _backupData;
            _manager.ApplyToEngine(_backupData); // Reverte o estrago na tela na mesma hora
            RefreshUIValues();
        }

        private void StopConfirmRoutine()
        {
            if (_confirmRoutine != null) { StopCoroutine(_confirmRoutine); _confirmRoutine = null; }
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }
    }
}