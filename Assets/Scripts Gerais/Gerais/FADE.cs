using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeTela : MonoBehaviour
{
    private static FadeTela instancia;
    private CanvasGroup grupo;

    public static FadeTela Instancia
    {
        get
        {
            if (instancia == null) instancia = FindFirstObjectByType<FadeTela>();
            if (instancia == null) instancia = Criar();
            return instancia;
        }
    }

    private static FadeTela Criar()
    {
        GameObject raiz = new GameObject("FadeTela", typeof(Canvas), typeof(CanvasGroup), typeof(FadeTela));

        Canvas canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        GameObject preto = new GameObject("Preto", typeof(RectTransform), typeof(Image));
        preto.transform.SetParent(raiz.transform, false);

        RectTransform rt = preto.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = preto.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        return raiz.GetComponent<FadeTela>();
    }

    void Awake()
    {
        instancia = this;
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;
    }

    public IEnumerator Escurecer(float duracao)
    {
        return Animar(1f, duracao);
    }

    public IEnumerator Clarear(float duracao)
    {
        return Animar(0f, duracao);
    }

    private IEnumerator Animar(float alvo, float duracao)
    {
        float inicio = grupo.alpha;
        float t = 0f;

        while (t < duracao)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(inicio, alvo, t / duracao);
            yield return null;
        }

        grupo.alpha = alvo;
    }
}