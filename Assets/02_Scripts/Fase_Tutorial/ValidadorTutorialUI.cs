using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class ValidadorTutorialUI : MonoBehaviour
{
    public enum AcaoMonitorada { AndarHorizontal, Pular, Interagir }

    [Header("Regra de Validação")]
    [SerializeField] private AcaoMonitorada acaoEsperada;
    [SerializeField] private float tempoNecessario = 0.5f;

    [Header("Configurações de Animação (Juice)")]
    [SerializeField] private float velocidadeEntradaSaida = 10f;
    [SerializeField] private float amplitudeFlutuacao = 8f; // O quanto ele sobe e desce flutuando
    [SerializeField] private float velocidadeFlutuacao = 4f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 escalaOriginal;
    private Vector2 posicaoOriginal;
    private float tempoAcaoConcluida;
    private bool objetivoCumprido = false;
    private bool animandoEntrada = true;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        escalaOriginal = rectTransform.localScale;
        posicaoOriginal = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        // Prepara o estado inicial invisível e encolhido para a animação de entrada
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.zero;
        tempoAcaoConcluida = 0f;
        objetivoCumprido = false;
        animandoEntrada = true;

        StopAllCoroutines();
        StartCoroutine(AnimarEntrada());
    }

    private IEnumerator AnimarEntrada()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * velocidadeEntradaSaida;
            // Curva suave de crescimento com leve sobreposição elástica
            float progresso = Mathf.Sin(t * Mathf.PI * 0.5f);
            rectTransform.localScale = escalaOriginal * progresso;
            canvasGroup.alpha = t;
            yield return null;
        }
        rectTransform.localScale = escalaOriginal;
        canvasGroup.alpha = 1f;
        animandoEntrada = false;
    }

    private void Update()
    {
        if (objetivoCumprido || animandoEntrada) return;

        // Efeito de flutuação orgânica (Idle) enquanto aguarda o jogador
        float offsetY = Mathf.Sin(Time.unscaledTime * velocidadeFlutuacao) * amplitudeFlutuacao;
        rectTransform.anchoredPosition = posicaoOriginal + new Vector2(0, offsetY);

        bool acaoRealizada = false;

        if (Keyboard.current != null)
        {
            switch (acaoEsperada)
            {
                case AcaoMonitorada.AndarHorizontal:
                    if (Keyboard.current.aKey.isPressed || Keyboard.current.dKey.isPressed ||
                        Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                        acaoRealizada = true;
                    break;
                case AcaoMonitorada.Pular:
                    if (Keyboard.current.spaceKey.wasPressedThisFrame)
                        acaoRealizada = true;
                    break;
                case AcaoMonitorada.Interagir:
                    if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.yKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame)
                        acaoRealizada = true;
                    break;
            }
        }

        if (acaoRealizada)
        {
            tempoAcaoConcluida += Time.unscaledDeltaTime;

            if (tempoAcaoConcluida >= (acaoEsperada == AcaoMonitorada.AndarHorizontal ? tempoNecessario : 0f))
            {
                objetivoCumprido = true;
                StartCoroutine(AnimarSaida());
            }
        }
        else
        {
            tempoAcaoConcluida = 0f;
        }
    }

    private IEnumerator AnimarSaida()
    {
        float t = 1f;
        Vector3 escalaAtual = rectTransform.localScale;

        while (t > 0f)
        {
            // Saída mais rápida e encolhendo ao sumir
            t -= Time.unscaledDeltaTime * (velocidadeEntradaSaida * 1.5f);
            canvasGroup.alpha = t;
            rectTransform.localScale = Vector3.Lerp(Vector3.zero, escalaAtual, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.zero;
        rectTransform.anchoredPosition = posicaoOriginal; // Reseta a posição padrão
        gameObject.SetActive(false);
    }
}