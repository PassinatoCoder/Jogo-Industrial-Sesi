using UnityEngine;
using System;
using Core.Player;
using Core.Missions;
using Core.Interaction;
using Core.Inventory;

namespace Core.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private InteractionManager interactionManager;
        [SerializeField] private InventoryManager inventoryManager; // Nova referência
        [SerializeField] private MissionData firstTutorialMission;

        [Header("Dev Testing (Bypass)")]
        [Tooltip("Marque para liberar a mecânica imediatamente no Play Mode.")]
        [SerializeField] private bool devUnlockInteraction;
        [SerializeField] private bool devUnlockJump;
        [SerializeField] private bool devUnlockCrouch;
        [SerializeField] private bool devUnlockSprint;
        [SerializeField] private bool devUnlockToolBelt; // Novo
        [SerializeField] private bool devUnlockBag;      // Novo

        public event Action OnInteractionUnlocked;
        public event Action OnToolBeltUnlocked; // Evento do Cinto
        public event Action OnBagUnlocked;      // Evento da Bolsa
        public event Action<string> OnMechanicUnlocked;

        private void Awake()
        {
            if (playerController == null)
            {
                Debug.LogError("TUTORIAL: PlayerController não referenciado no TutorialManager.");
                return;
            }

            playerController.isJumpUnlocked = false;
            playerController.isCrouchUnlocked = false;
            playerController.isSprintUnlocked = false;

            if (interactionManager != null) interactionManager.enabled = false;
        }

        private void Start() => ApplyDevBypass();

        private void OnValidate()
        {
            if (Application.isPlaying) ApplyDevBypass();
        }

        private void ApplyDevBypass()
        {
            if (devUnlockInteraction) UnlockInteraction();
            if (devUnlockJump) UnlockJump();
            if (devUnlockCrouch) UnlockCrouch();
            if (devUnlockSprint) UnlockSprint();
            if (devUnlockToolBelt) UnlockToolBelt();
            if (devUnlockBag) UnlockBag();
        }

        public void GiveTutorialMission()
        {
            if (firstTutorialMission != null && MissionManager.Instance != null)
            {
                MissionManager.Instance.StartMission(firstTutorialMission);
            }
        }

        public void UnlockInteraction()
        {
            if (interactionManager != null && !interactionManager.enabled)
            {
                interactionManager.enabled = true;
                OnInteractionUnlocked?.Invoke();
                Debug.Log("TUTORIAL/UI: Interação manual [E, F, Y, G] destrancada.");
            }
        }

        public void UnlockToolBelt()
        {
            if (inventoryManager != null && !inventoryManager.HasToolBelt)
            {
                inventoryManager.EnableToolBelt();
                OnToolBeltUnlocked?.Invoke();
                OnMechanicUnlocked?.Invoke("CINTO ADQUIRIDO! \nToque [G] para equipar ferramentas. \nUse [1], [2], [3] para sacar.");
            }
        }

        public void UnlockBag()
        {
            if (inventoryManager != null && !inventoryManager.HasBag)
            {
                inventoryManager.EnableBag();
                OnBagUnlocked?.Invoke();
                OnMechanicUnlocked?.Invoke("BOLSA ADQUIRIDA! \nSegure [G] para guardar. \nPressione [I] para abrir o inventário.");
            }
        }

        public void UnlockJump()
        {
            if (playerController.isJumpUnlocked) return;
            playerController.isJumpUnlocked = true;
            OnMechanicUnlocked?.Invoke("[ESPAÇO] Pular desbloqueado.");
        }

        public void UnlockCrouch()
        {
            if (playerController.isCrouchUnlocked) return;
            playerController.isCrouchUnlocked = true;
            OnMechanicUnlocked?.Invoke("[CTRL Esquerdo] Agachar desbloqueado.");
        }

        public void UnlockSprint()
        {
            if (playerController.isSprintUnlocked) return;
            playerController.isSprintUnlocked = true;
            OnMechanicUnlocked?.Invoke("[SHIFT] Correr desbloqueado.");
        }
    }
}