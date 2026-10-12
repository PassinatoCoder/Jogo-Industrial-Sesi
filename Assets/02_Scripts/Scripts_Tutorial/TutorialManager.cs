using UnityEngine;
using Core.Player;

namespace Core.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        [Header("Player Reference")]
        [SerializeField] private PlayerController playerController;

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

            Debug.Log("TUTORIAL: Inicializado. Mecânicas avançadas bloqueadas.");
        }

        // --- MÉTODOS DE DESBLOQUEIO (Chamados pelos EventTrigger2D) ---

        public void EncounterBoss()
        {
            // GDD Passo 1: Encontro com o chefe
            Debug.Log("TUTORIAL/UI: Diálogo do Chefe iniciado. Bem-vindo à fábrica.");
            // O sistema de diálogo será plugado aqui
        }

        public void UnlockJump()
        {
            playerController.isJumpUnlocked = true;
            Debug.Log("TUTORIAL/UI: Pulo destrancado. Instrução de UI [Espaço] disparada.");
        }

        public void UnlockCrouch()
        {
            playerController.isCrouchUnlocked = true;
            Debug.Log("TUTORIAL/UI: Agachar destrancado. Instrução de UI [Ctrl] disparada.");
        }

        public void UnlockSprint()
        {
            playerController.isSprintUnlocked = true;
            Debug.Log("TUTORIAL/UI: Correr destrancado. Instrução de UI [Shift] disparada.");
        }
    }
}