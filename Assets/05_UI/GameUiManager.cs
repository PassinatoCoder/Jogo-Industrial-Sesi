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

        [Header("Vitals UI")]
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

        [Header("Mission & Tablet UI")]
        [SerializeField] private TextMeshProUGUI textMissionNotification;
        [SerializeField] private CanvasGroup tabletGroup;
        [SerializeField] private TextMeshProUGUI textTabletContent;
        private bool _isTabletOpen = false;
        private bool _hasNotebook = false;

        [Header("Tutorial Freeze UI")]
        [SerializeField] private GameObject tutorialFreezePanel;
        [SerializeField] private TextMeshProUGUI textTutorialInstruction;
        private bool _isTimeFrozen = false;

        [Header("Inventory UI (Visual Slots)")]
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private CanvasGroup inventoryPanelGroup;
        [SerializeField] private GameObject toolBeltHUD;
        [SerializeField] private TextMeshProUGUI textBagWeight;
        [SerializeField] private Image[] beltSlotIcons;
        [SerializeField] private Image[] bagSlotIcons;
        [Tooltip("Texto para mostrar os controles do item selecionado na bolsa")]
        [SerializeField] private TextMeshProUGUI textBagInteractionControls;

        [Header("Painéis para Ocultar (UI Overlap)")]
        [SerializeField] private CanvasGroup vitalsPanelGroup;
        [SerializeField] private CanvasGroup interactionPanelGroup;
        private bool _isInventoryOpen = false;

        private void OnEnable()
        {
            if (interactionManager != null)
            {
                interactionManager.OnWorldPromptChanged += UpdateWorldPrompt;
                interactionManager.OnHeldOptionsToggled += UpdateHeldOptions;
                interactionManager.OnInventoryToggled += ToggleInventoryPanel;
                interactionManager.OnBagSlotSelected += UpdateBagItemControls;
            }
            if (playerVitals != null) { playerVitals.OnHealthChanged += UpdateHealthUI; playerVitals.OnStaminaChanged += UpdateStaminaUI; playerVitals.OnIntegrityChanged += UpdateIntegrityUI; }
            if (DialogueManager.Instance != null) { DialogueManager.Instance.OnLinePlayed += HandleDialogueLine; DialogueManager.Instance.OnDialogueEnded += HideDialoguePanel; }
            if (MissionManager.Instance != null) { MissionManager.Instance.OnMissionStarted += ShowMissionStart; MissionManager.Instance.OnMissionCompleted += ShowMissionComplete; }
            if (tutorialManager != null) { tutorialManager.OnMechanicUnlocked += ShowTutorialInstruction; tutorialManager.OnToolBeltUnlocked += EnableToolBeltHUD; }
            if (inventoryManager != null) inventoryManager.OnInventoryUpdated += RefreshInventoryUI;
        }

        private void OnDisable()
        {

        }

        private void Start()
        {
            textWorldPrompt.gameObject.SetActive(false);
            textHeldOptions.gameObject.SetActive(false);
            dialoguePanelGroup.alpha = 0f;
            textMissionNotification.text = "";
            if (tutorialFreezePanel != null) tutorialFreezePanel.SetActive(false);
            tabletGroup.alpha = 0f;
            inventoryPanelGroup.alpha = 0f;
            if (toolBeltHUD != null) toolBeltHUD.SetActive(false);
            if (textBagInteractionControls != null) textBagInteractionControls.text = "";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M) && _hasNotebook && !_isTimeFrozen) ToggleTablet();
            if (_isTimeFrozen && Input.GetKeyDown(KeyCode.Return)) ResumeTime();
        }

        public void UnlockNotebook() { _hasNotebook = true; ToggleTablet(); }

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
        private void HandleDialogueLine(DialogueLine line) { dialoguePanelGroup.alpha = 1f; textDialogueName.text = line.speakerName; textDialogueContent.text = line.text; }
        private void HideDialoguePanel() => dialoguePanelGroup.alpha = 0f;
        private void ShowMissionStart(MissionData mission) { textMissionNotification.text = $"NOVA TAREFA: {mission.missionTitle}"; Invoke(nameof(ClearMissionNotification), 4f); }
        private void ShowMissionComplete(MissionData mission, GradeMedal medal) { textMissionNotification.text = $"TAREFA CONCLUÍDA: {mission.missionTitle} \nMedalha: {medal}"; Invoke(nameof(ClearMissionNotification), 4f); }
        private void ClearMissionNotification() => textMissionNotification.text = "";
        private void ToggleTablet() { _isTabletOpen = !_isTabletOpen; tabletGroup.alpha = _isTabletOpen ? 1f : 0f; if (_isTabletOpen) RefreshTablet(); }

        private void RefreshTablet()
        {
            if (MissionManager.Instance == null) return;
            var missions = MissionManager.Instance.GetActiveMissions();
            if (missions.Count == 0) { textTabletContent.text = "Nenhuma tarefa."; return; }
            string content = "";
            foreach (var m in missions) content += $"<color=yellow><b>{m.missionTitle}</b></color>\n{m.description}\n\n";
            textTabletContent.text = content;
        }

        private void ShowTutorialInstruction(string instructionText)
        {
            // ADICIONADO O AVISO DE ENTER AQUI
            textTutorialInstruction.text = instructionText + "\n\n<color=yellow><b>[Pressione ENTER para prosseguir]</b></color>";

            if (tutorialFreezePanel != null) tutorialFreezePanel.SetActive(true);
            Time.timeScale = 0f;
            _isTimeFrozen = true;
        }

        private void ResumeTime() { if (tutorialFreezePanel != null) tutorialFreezePanel.SetActive(false); Time.timeScale = 1f; _isTimeFrozen = false; }

        private void EnableToolBeltHUD() { if (toolBeltHUD != null) { toolBeltHUD.SetActive(true); RefreshInventoryUI(); } }

        private void ToggleInventoryPanel()
        {
            _isInventoryOpen = !_isInventoryOpen;

            inventoryPanelGroup.alpha = _isInventoryOpen ? 1f : 0f;
            inventoryPanelGroup.interactable = _isInventoryOpen;
            inventoryPanelGroup.blocksRaycasts = _isInventoryOpen;

            if (vitalsPanelGroup != null) vitalsPanelGroup.alpha = _isInventoryOpen ? 0f : 1f;
            if (interactionPanelGroup != null) interactionPanelGroup.alpha = _isInventoryOpen ? 0f : 1f;

            if (_isInventoryOpen)
            {
                if (textBagInteractionControls != null) textBagInteractionControls.text = "Selecione um item";
                RefreshInventoryUI();
            }
        }

        private void UpdateBagItemControls(string itemName)
        {
            if (textBagInteractionControls == null) return;

            if (itemName == "Vazio") textBagInteractionControls.text = "";
            else textBagInteractionControls.text = $"<color=yellow>{itemName}</color>\n[E] Pegar  |  [F] Usar";
        }

        private void RefreshInventoryUI()
        {
            if (inventoryManager == null) return;

            for (int i = 0; i < beltSlotIcons.Length; i++)
            {
                if (i < inventoryManager.ToolBelt.Length && inventoryManager.ToolBelt[i] != null)
                {
                    Core.Items.ToolItem tool = inventoryManager.ToolBelt[i].GetComponent<Core.Items.ToolItem>();
                    if (tool != null && tool.itemIcon != null)
                    {
                        beltSlotIcons[i].sprite = tool.itemIcon;
                        beltSlotIcons[i].color = Color.white;
                    }
                }
                else { beltSlotIcons[i].sprite = null; beltSlotIcons[i].color = new Color(1, 1, 1, 0); }
            }

            for (int i = 0; i < bagSlotIcons.Length; i++)
            {
                if (i < inventoryManager.Bag.Count && inventoryManager.Bag[i] != null)
                {
                    Core.Items.ToolItem tool = inventoryManager.Bag[i].GetComponent<Core.Items.ToolItem>();
                    if (tool != null && tool.itemIcon != null)
                    {
                        bagSlotIcons[i].sprite = tool.itemIcon;
                        bagSlotIcons[i].color = Color.white;
                    }
                }
                else { bagSlotIcons[i].sprite = null; bagSlotIcons[i].color = new Color(1, 1, 1, 0); }
            }

            if (textBagWeight != null) textBagWeight.text = $"PESO: {inventoryManager.CurrentWeight}kg / {inventoryManager.maxBagWeight}kg";
        }

        public void ClickBagSlot(int slotIndex)
        {
            if (interactionManager != null) interactionManager.SelectBagSlot(slotIndex);
        }
    }
}