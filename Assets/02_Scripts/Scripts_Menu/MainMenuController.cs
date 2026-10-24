using UnityEngine;
using System.Collections;
using Core.Systems; // Exige que o LevelManager da resposta anterior exista

namespace Core.UI.Menu
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Phase 1: Press Any Key")]
        [SerializeField] private CanvasGroup pressAnyKeyGroup;
        [SerializeField] private CanvasGroup backgroundPhase1;

        [Header("Phase 2: Main Menu")]
        [SerializeField] private CanvasGroup buttonsGroup;
        [SerializeField] private CanvasGroup backgroundPhase2;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 1.5f;

        private bool _isWaitingForKey = true;

        private void Start()
        {
            // Setup Inicial Imperativo
            pressAnyKeyGroup.alpha = 1f;
            backgroundPhase1.alpha = 1f;
            backgroundPhase2.alpha = 0f;

            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            if (_isWaitingForKey && Input.anyKeyDown)
            {
                _isWaitingForKey = false;
                StartCoroutine(TransitionToMenu());
            }
        }

        private IEnumerator TransitionToMenu()
        {
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeDuration;

                // Animação Crossfade Matemática
                pressAnyKeyGroup.alpha = Mathf.Lerp(1f, 0f, t);
                backgroundPhase1.alpha = Mathf.Lerp(1f, 0f, t);
                backgroundPhase2.alpha = Mathf.Lerp(0f, 1f, t);
                buttonsGroup.alpha = Mathf.Lerp(0f, 1f, t);

                yield return null;
            }

            // Destrava a tela para o jogador clicar
            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = true;
        }

        // --- CONEXÕES DOS BOTÕES ---
        public void Btn_Play()
        {
            if (LevelManager.Instance != null) LevelManager.Instance.LoadTutorial();
            else Debug.LogError("LEVEL MANAGER AUSENTE NA CENA.");
        }

        public void Btn_Quit()
        {
            if (LevelManager.Instance != null) LevelManager.Instance.QuitGame();
        }
    }
}