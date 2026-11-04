using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Core.Systems;

namespace Core.Minigames.Welding
{
    public class WeldingMinigameController : MonoBehaviour
    {
        public enum WeldingPhase { NotStarted, EPI, Cleaning, Shield, Extinguisher, Welding, Finished }

        [Header("Status Global")]
        public WeldingPhase currentPhase = WeldingPhase.NotStarted;

        [Header("Transição Cinematográfica (A Piscada)")]
        [Tooltip("Imagem totalmente preta no Canvas, cobrindo a tela inteira.")]
        [SerializeField] private Image blinkImage;
        [SerializeField] private AudioSource foleySource;
        [SerializeField] private AudioClip sfxFootsteps;
        [SerializeField] private float blinkDuration = 0.5f;

        [Header("Referências das Fases")]
        public MonoBehaviour phaseEPI;
        public MonoBehaviour phaseCleaning;
        public MonoBehaviour phaseShield;
        public MonoBehaviour phaseExtinguisher;
        public MonoBehaviour phaseWeldingAction;

        private void Start()
        {
            if (blinkImage != null) blinkImage.color = new Color(0, 0, 0, 0);
            StartPhase(WeldingPhase.EPI);
        }

        public void AdvanceToPhase(WeldingPhase nextPhase)
        {
            StartCoroutine(BlinkAndTransitionSequence(nextPhase));
        }

        // NOVO: Método que o WeldingPrecisionBar vai chamar quando a barra bater 100%
        public void ConcludeWelding()
        {
            Debug.Log("[WeldingMinigameController] Solda Finalizada com sucesso! Indo para a etapa final.");
            AdvanceToPhase(WeldingPhase.Finished);
        }

        private IEnumerator BlinkAndTransitionSequence(WeldingPhase nextPhase)
        {
            float t = 0;
            while (t < blinkDuration)
            {
                t += Time.deltaTime;
                if (blinkImage != null) blinkImage.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, t / blinkDuration));
                yield return null;
            }

            if (foleySource != null && sfxFootsteps != null)
            {
                foleySource.PlayOneShot(sfxFootsteps);
                yield return new WaitForSeconds(sfxFootsteps.length * 0.5f);
            }

            StartPhase(nextPhase);

            t = 0;
            while (t < blinkDuration)
            {
                t += Time.deltaTime;
                if (blinkImage != null) blinkImage.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, t / blinkDuration));
                yield return null;
            }
        }

        private void StartPhase(WeldingPhase phase)
        {
            currentPhase = phase;
            Debug.Log($"[WeldingController] Iniciando Fase: {phase}");

            if (phaseEPI != null) phaseEPI.enabled = false;
            if (phaseCleaning != null) phaseCleaning.enabled = false;
            if (phaseShield != null) phaseShield.enabled = false;
            if (phaseExtinguisher != null) phaseExtinguisher.enabled = false;
            if (phaseWeldingAction != null) phaseWeldingAction.enabled = false;

            switch (phase)
            {
                case WeldingPhase.EPI: if (phaseEPI != null) phaseEPI.enabled = true; break;
                case WeldingPhase.Cleaning: if (phaseCleaning != null) phaseCleaning.enabled = true; break;
                case WeldingPhase.Shield: if (phaseShield != null) phaseShield.enabled = true; break;
                case WeldingPhase.Extinguisher: if (phaseExtinguisher != null) phaseExtinguisher.enabled = true; break;
                case WeldingPhase.Welding: if (phaseWeldingAction != null) phaseWeldingAction.enabled = true; break;
                    // No Finished, não ativamos nada por enquanto
            }
        }
    }
}