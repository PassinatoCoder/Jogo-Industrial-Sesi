using UnityEngine;
using System;
using Core.Player;
using Core.Missions;
using Core.Interaction;

namespace Core.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private InteractionManager interactionManager;
        [SerializeField] private MissionData firstTutorialMission;

        [Header("Dev Testing (Bypass)")]
        [Tooltip("Marque para liberar a mecânica imediatamente (funciona em tempo real no Play Mode).")]
        [SerializeField] private bool devUnlockInteraction;
        [SerializeField] private bool devUnlockJump;
        [SerializeField] private bool devUnlockCrouch;
        [SerializeField] private bool devUnlockSprint;

        public event Action OnInteractionUnlocked;
        public event Action<string> OnMechanicUnlocked;

        private void Awake()
        {
            if (playerController == null)
            {
                Debug.LogError("TUTORIAL: PlayerController não referenciado no TutorialManager.");
                return;
            }

            // GDD: Aplicação imediata das travas iniciais
            playerController.isJumpUnlocked = false;
            playerController.isCrouchUnlocked = false;
            playerController.isSprintUnlocked = false;

            // Trava o Inventário/Interação no início
            if (interactionManager != null) interactionManager.enabled = false;
        }

        private void Start()
        {
            // Aplica o destrancamento se as caixas já estiverem marcadas antes de dar o Play
            ApplyDevBypass();
        }

        private void OnValidate()
        {
            // Permite clicar nas caixas durante o Play Mode e forçar o destrancamento imediato
            if (Application.isPlaying)
            {
                ApplyDevBypass();
            }
        }

        private void ApplyDevBypass()
        {
            if (devUnlockInteraction) UnlockInteraction();
            if (devUnlockJump) UnlockJump();
            if (devUnlockCrouch) UnlockCrouch();
            if (devUnlockSprint) UnlockSprint();
        }

        // --- INTEGRAÇÃO COM MISSÕES E EVENTOS ---

        public void GiveTutorialMission()
        {
            if (firstTutorialMission != null && MissionManager.Instance != null)
            {
                MissionManager.Instance.StartMission(firstTutorialMission);
                Debug.Log("TUTORIAL: Missão primária despachada ao jogador.");
            }
            else
            {
                Debug.LogError("TUTORIAL: Falha ao entregar missão. MissionData não atribuído ou MissionManager ausente.");
            }
        }

        // --- MÉTODOS DE DESBLOQUEIO (Chamados pelos EventTrigger2D ou pelo Dev Bypass) ---

        public void UnlockInteraction()
        {
            if (interactionManager != null && !interactionManager.enabled)
            {
                interactionManager.enabled = true;
                OnInteractionUnlocked?.Invoke();
                Debug.Log("TUTORIAL/UI: Interação manual [E, F, Y, G] e HUD do Cinto destrancados.");
            }
        }

        public void UnlockJump()
        {
            if (playerController.isJumpUnlocked) return;
            playerController.isJumpUnlocked = true;
            OnMechanicUnlocked?.Invoke("[ESPAÇO] Pular desbloqueado.");
            Debug.Log("TUTORIAL/UI: Pulo destrancado.");
        }

        public void UnlockCrouch()
        {
            if (playerController.isCrouchUnlocked) return;
            playerController.isCrouchUnlocked = true;
            OnMechanicUnlocked?.Invoke("[CTRL Esquerdo] Agachar desbloqueado.");
            Debug.Log("TUTORIAL/UI: Agachar destrancado.");
        }

        public void UnlockSprint()
        {
            if (playerController.isSprintUnlocked) return;
            playerController.isSprintUnlocked = true;
            OnMechanicUnlocked?.Invoke("[SHIFT] Correr desbloqueado.");
            Debug.Log("TUTORIAL/UI: Correr destrancado.");
        }
    }
}