using System;
using System.Collections;
using UnityEngine;
using Core.Systems;

namespace Core.UI.Menu
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Phase 1: Press Any Key")]
        [SerializeField] private CanvasGroup pressAnyKeyGroup;
        [SerializeField] private CanvasGroup backgroundPhase1;
        [Tooltip("Opcional: o texto 'Pressione qualquer tecla' (CinematicMenuText). Faz a saída animada antes da transição.")]
        [SerializeField] private CinematicMenuText pressAnyKeyText;
        [SerializeField] private float pressAnyKeyExitDelay = 0.35f;

        [Header("Phase 2: Main Menu")]
        [SerializeField] private CanvasGroup buttonsGroup;
        [SerializeField] private CanvasGroup backgroundPhase2;
        [Tooltip("Opcional: toca a entrada em cascata do menu toda vez que ele aparece.")]
        [SerializeField] private MenuEntranceAnimator entrance;

        [Header("Phase 3: Tablet (Opções)")]
        [SerializeField] private CanvasGroup tabletGroup;

        [Header("Transição (Fade to Black)")]
        [SerializeField] private CanvasGroup fadeOverlay; // ARRASTE A TELA PRETA AQUI
        [SerializeField] private float fadeDuration = 1.0f;

        [Header("Transição Estilizada (opcional)")]
        [Tooltip("Se preenchido, as trocas de tela usam as faixas diagonais em vez do fade preto. O fade continua como reserva.")]
        [SerializeField] private MenuWipeTransition wipe;

        private bool _isWaitingForKey = true;
        private bool _isTransitioning;

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
            _isTransitioning = true;

            // 0. O texto "Pressione qualquer tecla" faz a saída dele antes da transição
            if (pressAnyKeyText != null)
            {
                pressAnyKeyText.Dismiss();
                yield return new WaitForSecondsRealtime(pressAnyKeyExitDelay);
            }

            // 1-3. Cobre a tela, troca os painéis no escuro e revela o menu
            yield return StartCoroutine(RunTransition(() =>
            {
                pressAnyKeyGroup.alpha = 0f;
                backgroundPhase1.alpha = 0f;
                backgroundPhase2.alpha = 1f;
                buttonsGroup.alpha = 1f;
                if (entrance != null) entrance.Play();
            }));

            SetCanvasState(buttonsGroup, 1f, true);
            _isTransitioning = false;
        }

        // --- TRANSITIONS: MENU <-> TABLET ---
        public void Btn_Options()
        {
            if (_isTransitioning) return;
            StartCoroutine(FadeToBlackTransition(buttonsGroup, tabletGroup, 0f, false)); // 0f apaga a fábrica
        }

        public void Btn_BackToMenu()
        {
            if (_isTransitioning) return;
            StartCoroutine(FadeToBlackTransition(tabletGroup, buttonsGroup, 1f, true)); // 1f restaura a fábrica
        }

        private IEnumerator FadeToBlackTransition(CanvasGroup outGroup, CanvasGroup inGroup, float factoryBackgroundAlpha, bool replayEntrance)
        {
            _isTransitioning = true;

            SetCanvasState(outGroup, outGroup.alpha, false);
            SetCanvasState(inGroup, inGroup.alpha, false);

            yield return StartCoroutine(RunTransition(() =>
            {
                // Troca os painéis no escuro
                outGroup.alpha = 0f;
                inGroup.alpha = 1f;
                backgroundPhase2.alpha = factoryBackgroundAlpha;
                if (replayEntrance && entrance != null) entrance.Play();
            }));

            SetCanvasState(inGroup, 1f, true);
            _isTransitioning = false;
        }

        /// <summary>
        /// Cobre a tela, executa 'swap' (troca de painéis) com a tela coberta e revela.
        /// Usa as faixas diagonais se existirem (e o "Reduzir movimento" estiver desligado); senão, o fade preto.
        /// </summary>
        private IEnumerator RunTransition(Action swap)
        {
            if (wipe != null && !MenuMotion.ReduceMotion)
            {
                yield return StartCoroutine(wipe.Play(swap));
                yield break;
            }

            // 1. Escurece tudo
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 0f, 1f, fadeDuration / 2f));

            // 2. Troca os painéis no escuro
            swap();

            // 3. Clareia revelando o novo painel
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 1f, 0f, fadeDuration / 2f));
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
        {
            if (cg == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // funciona também com o jogo pausado
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
            if (_isTransitioning) return;

            if (LevelManager.Instance != null) LevelManager.Instance.LoadPrensa_Minigame();
            else Debug.LogError("ERRO FATAL: LevelManager não encontrado.");
        }

        public void Btn_Quit() => Application.Quit();
    }
}