using UnityEngine;
using Core.Player;
using Core.Missions;

namespace Core.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MissionData firstTutorialMission; // Removi o segundo Header que estava causando bug na serialização

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

        // --- MÉTODOS DE DESBLOQUEIO (Chamados pelos EventTrigger2D) ---

        public void UnlockJump()
        {
            playerController.isJumpUnlocked = true;
            Debug.Log("TUTORIAL/UI: Pulo destrancado.");
        }

        public void UnlockCrouch()
        {
            playerController.isCrouchUnlocked = true;
            Debug.Log("TUTORIAL/UI: Agachar destrancado.");
        }

        public void UnlockSprint()
        {
            playerController.isSprintUnlocked = true;
            Debug.Log("TUTORIAL/UI: Correr destrancado.");
        }
    }
}