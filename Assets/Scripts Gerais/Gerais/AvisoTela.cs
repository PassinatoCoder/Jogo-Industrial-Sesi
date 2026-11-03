using UnityEngine;
using UnityEngine.UI;

// Avisos na tela, criados por código na primeira vez que são usados:
// um aviso temporário no topo e uma dica fixa embaixo.
[RequireComponent(typeof(Canvas))]
public class AvisoTela : MonoBehaviour
{
    private static AvisoTela instancia;

    public static AvisoTela Instancia
    {
        get
        {
            if (instancia == null) instancia = FindFirstObjectByType<AvisoTela>();
            if (instancia == null) instancia = new GameObject("AvisoTela", typeof(AvisoTela)).GetComponent<AvisoTela>();
            return instancia;
        }
    }

    private Text textoAviso;
    private Text textoDica;
    private float restante;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstatico()
    {
        instancia = null;
    }

    void Awake()
    {
        instancia = this;
        Montar();
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    void Update()
    {
        if (restante <= 0f) return;

        restante -= Time.unscaledDeltaTime;
        if (restante <= 0f) textoAviso.gameObject.SetActive(false);
    }

    public void Mostrar(string texto, float duracao)
    {
        Mostrar(texto, duracao, new Color(1f, 0.9f, 0.4f));
    }

    public void Mostrar(string texto, float duracao, Color cor)
    {
        textoAviso.text = texto;
        textoAviso.color = cor;
        textoAviso.gameObject.SetActive(true);
        restante = duracao;
    }

    public void DefinirDica(string texto)
    {
        if (textoDica.text != texto) textoDica.text = texto;
        if (!textoDica.gameObject.activeSelf) textoDica.gameObject.SetActive(true);
    }

    public void LimparDica()
    {
        textoDica.gameObject.SetActive(false);
    }

    private void Montar()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600; // acima do inventário (500), abaixo da tela preta (999)

        CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1280f, 720f);
        escala.matchWidthOrHeight = 0.5f;

        Font fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        textoAviso = CriarTexto("Aviso", fonte, 32, FontStyle.Bold,
                                new Vector2(0f, 1f), new Vector2(1f, 1f),
                                new Vector2(40f, -120f), new Vector2(-40f, -40f));

        textoDica = CriarTexto("Dica", fonte, 24, FontStyle.Normal,
                               new Vector2(0f, 0f), new Vector2(1f, 0f),
                               new Vector2(40f, 30f), new Vector2(-40f, 130f));

        textoAviso.gameObject.SetActive(false);
        textoDica.gameObject.SetActive(false);
    }

    private Text CriarTexto(string nome, Font fonte, int tamanho, FontStyle estilo,
                            Vector2 ancoraMin, Vector2 ancoraMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancoraMin;
        rt.anchorMax = ancoraMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Text t = go.GetComponent<Text>();
        t.font = fonte;
        t.fontSize = tamanho;
        t.fontStyle = estilo;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        Outline contorno = go.AddComponent<Outline>();
        contorno.effectColor = Color.black;
        contorno.effectDistance = new Vector2(2f, -2f);

        return t;
    }
}