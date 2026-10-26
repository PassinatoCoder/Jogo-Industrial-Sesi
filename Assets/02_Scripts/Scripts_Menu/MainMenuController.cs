using UnityEngine;
using System.Collections;
using Core.Systems;

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

        [Header("Phase 3: Tablet (Opções)")]
        [SerializeField] private CanvasGroup tabletGroup;

        [Header("Transição (Fade to Black)")]
        [SerializeField] private CanvasGroup fadeOverlay; // ARRASTE A TELA PRETA AQUI
        [SerializeField] private float fadeDuration = 1.0f;

        private bool _isWaitingForKey = true;

        private void Start()
        {
            SetCanvasState(pressAnyKeyGroup, 1f, false);
            SetCanvasState(backgroundPhase1, 1f, false);

            SetCanvasState(backgroundPhase2, 0f, false);
            SetCanvasState(buttonsGroup, 0f, false);

            SetCanvasState(tabletGroup, 0f, false);
            SetCanvasState(fadeOverlay, 0f, false); // Garante que a tela preta comece invisível
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
            // 1. Escurece a tela para preto
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 0f, 1f, fadeDuration / 2f));

            // 2. Troca os painéis instantaneamente no escuro
            pressAnyKeyGroup.alpha = 0f;
            backgroundPhase1.alpha = 0f;
            backgroundPhase2.alpha = 1f;
            buttonsGroup.alpha = 1f;

            // 3. Clareia a tela revelando o Menu
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 1f, 0f, fadeDuration / 2f));

            SetCanvasState(buttonsGroup, 1f, true);
        }

        // --- TRANSITIONS: MENU <-> TABLET ---
        public void Btn_Options()
        {
            StartCoroutine(FadeToBlackTransition(buttonsGroup, tabletGroup, 0f)); // 0f apaga a fábrica
        }

        public void Btn_BackToMenu()
        {
            StartCoroutine(FadeToBlackTransition(tabletGroup, buttonsGroup, 1f)); // 1f restaura a fábrica
        }

        private IEnumerator FadeToBlackTransition(CanvasGroup outGroup, CanvasGroup inGroup, float factoryBackgroundAlpha)
        {
            SetCanvasState(outGroup, outGroup.alpha, false);
            SetCanvasState(inGroup, inGroup.alpha, false);

            // 1. Escurece tudo
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 0f, 1f, fadeDuration / 2f));

            // 2. Troca os painéis no escuro
            outGroup.alpha = 0f;
            inGroup.alpha = 1f;
            backgroundPhase2.alpha = factoryBackgroundAlpha;

            // 3. Clareia revelando o novo painel
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 1f, 0f, fadeDuration / 2f));

            SetCanvasState(inGroup, 1f, true);
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
                yield return null;
            }
            cg.alpha = end;
        }

        private void SetCanvasState(CanvasGroup group, float alpha, bool interactable)
        {
            if (group == null) return;
            group.alpha = alpha;
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }

        public void Btn_Play()
        {
            if (LevelManager.Instance != null) LevelManager.Instance.LoadPrensa_Minigame();
            else Debug.LogError("ERRO FATAL: LevelManager não encontrado.");
        }

        public void Btn_Quit() => Application.Quit();
    }
}