using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class CinematicMenuText : MonoBehaviour
{
    [Header("Respiração Orgânica")]
    [Tooltip("Velocidade do ciclo de expansão e brilho base.")]
    [SerializeField] private float breathSpeed = 1.2f;
    [Tooltip("Opacidade mínima durante a respiração.")]
    [SerializeField, Range(0.1f, 0.9f)] private float minAlpha = 0.4f;
    [Tooltip("Expansão máxima da escala. Mantenha sutil (ex: 1.02).")]
    [SerializeField, Range(1f, 1.1f)] private float maxScale = 1.03f;

    [Header("Varredura de Luz (Shimmer)")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.6f, 1f);
    [Tooltip("Tempo em segundos entre cada onda de luz.")]
    [SerializeField] private float shimmerInterval = 4f;
    [Tooltip("Velocidade com que a luz percorre o texto.")]
    [SerializeField] private float shimmerSpeed = 1.5f;
    [Tooltip("Largura do feixe de luz.")]
    [SerializeField, Range(0.1f, 1f)] private float shimmerWidth = 0.3f;

    [Header("Saída Cinematográfica")]
    [SerializeField] private float dismissDuration = 0.6f;
    [SerializeField] private float dismissScale = 1.15f;
    [SerializeField] private UnityEvent onDismissed;

    private TMP_Text _text;
    private Vector3 _baseScale;
    private Color _baseColor;
    private bool _isDismissing;
    private float _time;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _baseScale = transform.localScale;
        _baseColor = _text.color;
    }

    private void Update()
    {
        if (_isDismissing) return;

        // Usar tempo não escalonado garante que a UI funcione mesmo com o jogo pausado (Time.timeScale = 0)
        _time += Time.unscaledDeltaTime;

        // Respiração: Senóide normalizada entre 0 e 1
        float breathCycle = (Mathf.Sin(_time * breathSpeed) + 1f) * 0.5f;
        float currentAlpha = Mathf.Lerp(minAlpha, 1f, breathCycle);

        transform.localScale = _baseScale * Mathf.Lerp(1f, maxScale, breathCycle);

        UpdateTextColors(currentAlpha);
    }

    private void UpdateTextColors(float baseAlpha)
    {
        _text.ForceMeshUpdate();
        TMP_TextInfo textInfo = _text.textInfo;
        int characterCount = textInfo.characterCount;

        if (characterCount == 0) return;

        // Progresso do feixe de luz passando pelas letras
        float cycleTime = _time % shimmerInterval;
        float shimmerProgress = (cycleTime * shimmerSpeed) - 0.5f;

        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            // Posição normalizada da letra na frase (0 a 1)
            float charPercent = (float)i / characterCount;

            // Distância da letra para o centro do feixe de luz
            float distance = Mathf.Abs(charPercent - shimmerProgress);
            float shimmerWeight = Mathf.Clamp01(1f - (distance / shimmerWidth));

            // Curva de suavização (SmoothStep) para um gradiente orgânico
            shimmerWeight = shimmerWeight * shimmerWeight * (3f - 2f * shimmerWeight);

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            Color finalColor = Color.Lerp(_baseColor, highlightColor, shimmerWeight);
            finalColor.a = baseAlpha;

            for (int v = 0; v < 4; v++)
            {
                colors[vertexIndex + v] = finalColor;
            }
        }

        // Atualiza apenas as cores na GPU, economizando processamento ao não recalcular posições físicas (vértices)
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }

    public void Dismiss()
    {
        if (_isDismissing) return;
        _isDismissing = true;
        StartCoroutine(DismissRoutine());
    }

    private IEnumerator DismissRoutine()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = _baseScale * dismissScale;

        // Captura o alpha exato no momento do clique para evitar saltos secos de opacidade
        float startAlpha = (Mathf.Sin(_time * breathSpeed) + 1f) * 0.5f;
        startAlpha = Mathf.Lerp(minAlpha, 1f, startAlpha);

        while (elapsed < dismissDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dismissDuration);

            // Curva Easing Out Quart: Saída rápida que desacelera suavemente no final
            float easeOut = 1f - Mathf.Pow(1f - t, 4f);

            transform.localScale = Vector3.Lerp(startScale, targetScale, easeOut);

            Color color = _text.color;
            color.a = Mathf.Lerp(startAlpha, 0f, easeOut);
            _text.color = color;

            yield return null;
        }

        onDismissed?.Invoke();
    }
}