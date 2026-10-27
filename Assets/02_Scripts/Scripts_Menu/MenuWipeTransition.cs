using System;
using System.Collections;
using UnityEngine;

namespace Core.UI.Menu
{
    /// <summary>
    /// Transição de tela com faixas diagonais que cobrem a tela e se afastam (no lugar do fade preto).
    /// O MainMenuController chama: StartCoroutine(wipe.Play(() => trocaAsTelasAqui)).
    ///
    /// Montagem: um objeto com CanvasGroup (este script) cobrindo a tela inteira,
    /// e dentro dele as faixas (Images tortas). Veja o GUIA.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MenuWipeTransition : MonoBehaviour
    {
        [Header("Faixas (arraste as Images, da esquerda pra direita)")]
        [SerializeField] private RectTransform[] strips;

        [Header("Tempos (segundos)")]
        [SerializeField] private float coverTime = 0.32f;
        [SerializeField] private float stagger = 0.06f;
        [SerializeField] private float holdTime = 0.06f;
        [SerializeField] private float revealTime = 0.34f;

        [Header("Movimento")]
        [Tooltip("Distância (em pixels do Canvas) que cada faixa percorre pra sair da tela. Faixa de 3200 de altura: use 2600.")]
        [SerializeField] private float travel = 2600f;
        [Tooltip("Faixas pares vêm de um lado e as ímpares do outro (efeito zigue-zague).")]
        [SerializeField] private bool alternateDirection = true;

        private CanvasGroup _group;
        private Vector2[] _basePositions;
        private Vector2[] _axes;
        private float[] _directions;
        private bool _playing;

        public bool IsPlaying => _playing;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            int count = strips != null ? strips.Length : 0;
            _basePositions = new Vector2[count];
            _axes = new Vector2[count];
            _directions = new float[count];

            for (int i = 0; i < count; i++)
            {
                if (strips[i] == null) continue;

                _basePositions[i] = strips[i].anchoredPosition;
                _axes[i] = (Vector2)(strips[i].localRotation * Vector3.up);
                _directions[i] = (alternateDirection && i % 2 == 1) ? -1f : 1f;
                SetStrip(i, -1f); // começa escondida fora da tela
            }
        }

        /// <summary>
        /// Cobre a tela, chama onCovered (aqui você troca os painéis) e revela.
        /// Use com StartCoroutine(wipe.Play(...)).
        /// </summary>
        public IEnumerator Play(Action onCovered)
        {
            int count = strips != null ? strips.Length : 0;

            if (_playing || count == 0)
            {
                onCovered?.Invoke();
                yield break;
            }

            _playing = true;
            _group.blocksRaycasts = true; // bloqueia cliques durante a transição

            float cover = Mathf.Max(0.01f, coverTime);
            float reveal = Mathf.Max(0.01f, revealTime);

            try
            {
                // 1) Faixas entram e cobrem a tela
                float total = cover + stagger * (count - 1);
                float t = 0f;
                while (t < total)
                {
                    t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    for (int i = 0; i < count; i++)
                    {
                        float x = Mathf.Clamp01((t - i * stagger) / cover);
                        SetStrip(i, -(1f - MenuEasing.OutCubic(x)));
                    }
                    yield return null;
                }
                for (int i = 0; i < count; i++) SetStrip(i, 0f);

                // 2) Tela coberta: hora de trocar os painéis
                onCovered?.Invoke();
                if (holdTime > 0f) yield return new WaitForSecondsRealtime(holdTime);

                // 3) Faixas aceleram pra fora, revelando a nova tela
                total = reveal + stagger * (count - 1);
                t = 0f;
                while (t < total)
                {
                    t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    for (int i = 0; i < count; i++)
                    {
                        float x = Mathf.Clamp01((t - i * stagger) / reveal);
                        SetStrip(i, x * x * x);
                    }
                    yield return null;
                }
            }
            finally
            {
                for (int i = 0; i < count; i++) SetStrip(i, -1f);
                _group.blocksRaycasts = false;
                _playing = false;
            }
        }

        // phase: -1 = escondida (lado de entrada) | 0 = cobrindo a tela | +1 = escondida (lado de saída)
        private void SetStrip(int index, float phase)
        {
            if (strips[index] == null) return;
            strips[index].anchoredPosition = _basePositions[index] + _axes[index] * (_directions[index] * travel * phase);
        }
    }
}