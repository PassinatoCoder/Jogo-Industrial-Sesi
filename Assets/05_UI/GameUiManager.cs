using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Player;
using Core.Dialogue;
using Core.Missions;
using Core.Tutorial;
using Core.Inventory;
using Core.Interaction;

namespace Core.UI
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private PlayerVitals playerVitals;
        [SerializeField] private InteractionManager interactionManager;
        [SerializeField] private TutorialManager tutorialManager;

        [Header("Vitals UI (Sliders)")]
        [SerializeField] private Slider sliderHealth;
        [SerializeField] private Slider sliderStamina;
        [SerializeField] private Slider sliderIntegrity;

        [Header("Interaction UI")]
        [SerializeField] private TextMeshProUGUI textWorldPrompt;
        [SerializeField] private TextMeshProUGUI textHeldOptions;

        [Header("Dialogue UI")]
        [SerializeField] private CanvasGroup dialoguePanelGroup;
        [SerializeField] private TextMeshProUGUI textDialogueName;
        [SerializeField] private TextMeshProUGUI textDialogueContent;
        [SerializeField] private CanvasGroup leftSpeakerGroup;
        [SerializeField] private CanvasGroup rightSpeakerGroup;

        [Header("Mission Notification (Pop-up)")]
        [SerializeField] private TextMeshProUGUI textMissionNotification;

        [Header("Tablet UI (Tecla M)")]
        [SerializeField] private CanvasGroup tabletGroup;
        [SerializeField] private TextMeshProUGUI textTabletContent;
        private bool _isTabletOpen = false;

        [Header("Tutorial UI")]
        [SerializeField] private TextMeshProUGUI textTutorialInstruction;

        [Header("Inventory UI")]
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private CanvasGroup inventoryPanelGroup;
        [SerializeField] private TextMeshProUGUI textBagContent;
        [SerializeField] private TextMeshProUGUI textBagWeight;
        [SerializeField] private GameObject toolBeltHUD;
        [SerializeField] private TextMeshProUGUI textToolBeltContent;
        private bool _isInventoryOpen = false;

        private void OnEnable()
        {
            if (interactionManager != null)
            {
                interactionManager.OnWorldPromptChanged += UpdateWorldPrompt;
                interactionManager.OnHeldOptionsToggled += UpdateHeldOptions;
                interactionManager.OnInventoryToggled += ToggleInventoryPanel;
            }
            if (playerVitals != null)
            {
                playerVitals.OnHealthChanged += UpdateHealthUI;
                playerVitals.OnStaminaChanged += UpdateStaminaUI;
                playerVitals.OnIntegrityChanged += UpdateIntegrityUI;
            }
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnLinePlayed += HandleDialogueLine;
                DialogueManager.Instance.OnDialogueEnded += HideDialoguePanel;
            }
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionStarted += ShowMissionStart;
                MissionManager.Instance.OnMissionCompleted += ShowMissionComplete;
            }
            if (tutorialManager != null)
            {
                tutorialManager.OnMechanicUnlocked += ShowTutorialInstruction;
                tutorialManager.OnInteractionUnlocked += EnableToolBeltHUD;
            }
            if (inventoryManager != null)
            {
                inventoryManager.OnInventoryUpdated += RefreshInventoryUI;
            }
        }

        private void OnDisable()
        {
            if (interactionManager != null)
            {
                interactionManager.OnWorldPromptChanged -= UpdateWorldPrompt;
                interactionManager.OnHeldOptionsToggled -= UpdateHeldOptions;
                interactionManager.OnInventoryToggled -= ToggleInventoryPanel;
            }
            if (playerVitals != null)
            {
                playerVitals.OnHealthChanged -= UpdateHealthUI;
                playerVitals.OnStaminaChanged -= UpdateStaminaUI;
                playerVitals.OnIntegrityChanged -= UpdateIntegrityUI;
            }
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnLinePlayed -= HandleDialogueLine;
                DialogueManager.Instance.OnDialogueEnded -= HideDialoguePanel;
            }
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionStarted -= ShowMissionStart;
                MissionManager.Instance.OnMissionCompleted -= ShowMissionComplete;
            }
            if (tutorialManager != null)
            {
                tutorialManager.OnMechanicUnlocked -= ShowTutorialInstruction;
                tutorialManager.OnInteractionUnlocked -= EnableToolBeltHUD;
            }
            if (inventoryManager != null)
            {
                inventoryManager.OnInventoryUpdated -= RefreshInventoryUI;
            }
        }

        private void Start()
        {
            textWorldPrompt.gameObject.SetActive(false);
            textHeldOptions.gameObject.SetActive(false);
            dialoguePanelGroup.alpha = 0f;
            textMissionNotification.text = "";
            textTutorialInstruction.gameObject.SetActive(false);

            tabletGroup.alpha = 0f;
            tabletGroup.interactable = false;
            tabletGroup.blocksRaycasts = false;

            inventoryPanelGroup.alpha = 0f;
            inventoryPanelGroup.interactable = false;
            inventoryPanelGroup.blocksRaycasts = false;

            if (toolBeltHUD != null) toolBeltHUD.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) ToggleTablet();
        }

        // --- INTERACTION & VITALS ---
        private void UpdateWorldPrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt)) textWorldPrompt.gameObject.SetActive(false);
            else { textWorldPrompt.text = $"[E] {prompt}"; textWorldPrompt.gameObject.SetActive(true); }
        }

        private void UpdateHeldOptions(bool isHolding)
        {
            textHeldOptions.gameObject.SetActive(isHolding);
            if (isHolding) textHeldOptions.text = "[F] Usar | [Y] Inspecionar | [G] Guardar";
        }

        private void UpdateHealthUI(float health) { if (sliderHealth != null) sliderHealth.value = health; }
        private void UpdateStaminaUI(float stamina) { if (sliderStamina != null) sliderStamina.value = stamina; }
        private void UpdateIntegrityUI(int integrity) { if (sliderIntegrity != null) sliderIntegrity.value = integrity; }

        // --- DIALOGUE ---
        private void HandleDialogueLine(DialogueLine line)
        {
            dialoguePanelGroup.alpha = 1f;
            textDialogueName.text = line.speakerName;
            textDialogueContent.text = line.text;

            if (line.speaker == SpeakerType.Protagonist)
            {
                leftSpeakerGroup.alpha = 1f; leftSpeakerGroup.transform.localScale = Vector3.one * 1.1f;
                rightSpeakerGroup.alpha = 0.5f; rightSpeakerGroup.transform.localScale = Vector3.one * 0.9f;
            }
            else
            {
                rightSpeakerGroup.alpha = 1f; rightSpeakerGroup.transform.localScale = Vector3.one * 1.1f;
                leftSpeakerGroup.alpha = 0.5f; leftSpeakerGroup.transform.localScale = Vector3.one * 0.9f;
            }
        }

        private void HideDialoguePanel() => dialoguePanelGroup.alpha = 0f;

        // --- MISSIONS & TABLET ---
        private void ShowMissionStart(MissionData mission)
        {
            textMissionNotification.text = $"NOVA TAREFA: {mission.missionTitle}";
            Invoke(nameof(ClearMissionNotification), 4f);
            if (_isTabletOpen) RefreshTablet();
        }

        private void ShowMissionComplete(MissionData mission, GradeMedal medal)
        {
            textMissionNotification.text = $"TAREFA CONCLUÍDA: {mission.missionTitle} \nMedalha: {medal}";
            Invoke(nameof(ClearMissionNotification), 4f);
            if (_isTabletOpen) RefreshTablet();
        }

        private void ClearMissionNotification() => textMissionNotification.text = "";

        private void ToggleTablet()
        {
            _isTabletOpen = !_isTabletOpen;
            tabletGroup.alpha = _isTabletOpen ? 1f : 0f;
            if (_isTabletOpen) RefreshTablet();
        }

        private void RefreshTablet()
        {
            if (MissionManager.Instance == null) return;
            var missions = MissionManager.Instance.GetActiveMissions();
            if (missions.Count == 0)
            {
                textTabletContent.text = "Nenhuma tarefa designada.";
                return;
            }
            string content = "";
            foreach (var m in missions) content += $"<color=yellow><b>{m.missionTitle}</b></color>\n{m.description}\n\n";
            textTabletContent.text = content;
        }

        // --- TUTORIAL ---
        private void ShowTutorialInstruction(string instructionText)
        {
            textTutorialInstruction.text = instructionText;
            textTutorialInstruction.gameObject.SetActive(true);
            CancelInvoke(nameof(HideTutorialInstruction));
            Invoke(nameof(HideTutorialInstruction), 5f);
        }

        private void HideTutorialInstruction() => textTutorialInstruction.gameObject.SetActive(false);

        // --- INVENTORY ---
        private void EnableToolBeltHUD()
        {
            if (toolBeltHUD != null)
            {
                toolBeltHUD.SetActive(true);
                RefreshInventoryUI();
            }
        }

        private void ToggleInventoryPanel()
        {
            _isInventoryOpen = !_isInventoryOpen;
            inventoryPanelGroup.alpha = _isInventoryOpen ? 1f : 0f;
            inventoryPanelGroup.interactable = _isInventoryOpen;
            inventoryPanelGroup.blocksRaycasts = _isInventoryOpen;
            if (_isInventoryOpen) RefreshInventoryUI();
        }

        private void RefreshInventoryUI()
        {
            if (inventoryManager == null) return;

            string beltItems = "CINTO: ";
            foreach (var item in inventoryManager.ToolBelt) beltItems += $"[{item}] ";
            if (textToolBeltContent != null) textToolBeltContent.text = beltItems;

            string bagItems = "BOLSA:\n";
            foreach (var item in inventoryManager.Bag) bagItems += $"- {item}\n";
            if (textBagContent != null) textBagContent.text = bagItems;

            if (textBagWeight != null) textBagWeight.text = $"PESO: {inventoryManager.CurrentWeight}kg / {inventoryManager.maxBagWeight}kg";
        }
    }
}