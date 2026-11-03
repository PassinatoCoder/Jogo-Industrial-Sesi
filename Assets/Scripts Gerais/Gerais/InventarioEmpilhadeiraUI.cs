using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Painel do inventário da empilhadeira, criado por código na primeira vez que é usado.
// Controles: W/S ou setas escolhem, ENTER/Espaço confirmam, TAB (ou Esc) fecha.
[RequireComponent(typeof(Canvas))]
public class InventarioUI : MonoBehaviour
{
    private struct Linha
    {
        public string texto;
        public int entrada;   // -1 = texto comum; senão, índice em "entradas"
    }

    private struct Entrada
    {
        public string texto;
        public Action acao;   // null = não dá para fazer agora
        public string aviso;  // mostrado quando tenta confirmar uma entrada sem ação
    }

    private static InventarioUI instancia;

    public static InventarioUI Instancia
    {
        get
        {
            if (instancia == null) instancia = FindFirstObjectByType<InventarioUI>();
            if (instancia == null) instancia = new GameObject("InventarioUI", typeof(InventarioUI)).GetComponent<InventarioUI>();
            return instancia;
        }
    }

    public static bool EstaAberto
    {
        get { return instancia != null && instancia.painel != null && instancia.painel.activeSelf; }
    }

    // Evita que o mesmo TAB abra e feche o painel no mesmo frame
    public static bool MudouNesteFrame
    {
        get { return instancia != null && instancia.frameDaMudanca == Time.frameCount; }
    }

    private GameObject painel;
    private Text textoTitulo;
    private Text textoCorpo;
    private Text textoRodape;

    private InventarioEmpilhadeira atual;
    private MovimentoTopDown jogador;
    private CarregadorDeItens mao;

    private readonly List<Entrada> entradas = new List<Entrada>();
    private readonly List<Linha> linhas = new List<Linha>();
    private int selecionado;
    private string mensagem = "";
    private int frameDaMudanca = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstatico()
    {
        instancia = null;
    }

    void Awake()
    {
        instancia = this;
        if (painel == null) Montar();
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    // ---------- MONTAR O PAINEL ----------
    private void Montar()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // abaixo da tela preta (999)

        CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1280f, 720f);
        escala.matchWidthOrHeight = 0.5f;

        Font fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        painel = new GameObject("Painel", typeof(RectTransform), typeof(Image));
        painel.transform.SetParent(transform, false);

        RectTransform rp = painel.GetComponent<RectTransform>();
        rp.anchorMin = new Vector2(0.5f, 0.5f);
        rp.anchorMax = new Vector2(0.5f, 0.5f);
        rp.pivot = new Vector2(0.5f, 0.5f);
        rp.sizeDelta = new Vector2(560f, 460f);

        Image fundo = painel.GetComponent<Image>();
        fundo.color = new Color(0f, 0f, 0f, 0.85f);
        fundo.raycastTarget = false;

        textoTitulo = CriarTexto("Titulo", painel.transform, fonte, 30, FontStyle.Bold,
                                 new Vector2(0f, 1f), new Vector2(1f, 1f),
                                 new Vector2(20f, -70f), new Vector2(-20f, -15f));

        textoCorpo = CriarTexto("Corpo", painel.transform, fonte, 20, FontStyle.Normal,
                                Vector2.zero, Vector2.one,
                                new Vector2(20f, 70f), new Vector2(-20f, -80f));

        textoRodape = CriarTexto("Rodape", painel.transform, fonte, 16, FontStyle.Italic,
                                 Vector2.zero, new Vector2(1f, 0f),
                                 new Vector2(20f, 15f), new Vector2(-20f, 55f));
        textoRodape.color = new Color(0.75f, 0.75f, 0.75f, 1f);

        painel.SetActive(false);
    }

    private Text CriarTexto(string nome, Transform pai, Font fonte, int tamanho, FontStyle estilo,
                            Vector2 ancoraMin, Vector2 ancoraMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(pai, false);

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
        t.alignment = TextAnchor.UpperLeft;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    // ---------- ABRIR / FECHAR ----------
    public void Abrir(InventarioEmpilhadeira inventario, MovimentoTopDown protagonista, CarregadorDeItens carregador)
    {
        atual = inventario;
        jogador = protagonista;
        mao = carregador;
        selecionado = 0;
        mensagem = "";

        jogador.Parar();
        jogador.PodeMover = false;

        textoTitulo.text = atual.Titulo;
        textoRodape.text = "W/S ou setas: escolher    ENTER: confirmar    " + atual.Tecla + " / ESC: fechar";

        frameDaMudanca = Time.frameCount;
        painel.SetActive(true);
        Atualizar();
    }

    public void Fechar()
    {
        if (painel == null || !painel.activeSelf) return;

        painel.SetActive(false);
        frameDaMudanca = Time.frameCount;

        if (jogador != null) jogador.PodeMover = true;

        atual = null;
        jogador = null;
        mao = null;
    }

    // ---------- ENTRADA DO TECLADO ----------
    void Update()
    {
        if (painel == null || !painel.activeSelf) return;

        if (atual == null || jogador == null || mao == null)
        {
            Fechar();
            return;
        }

        bool fecharComTab = Input.GetKeyDown(atual.Tecla) && frameDaMudanca != Time.frameCount;
        if (fecharComTab || Input.GetKeyDown(KeyCode.Escape))
        {
            Fechar();
            return;
        }

        int n = entradas.Count;

        if (n > 0 && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)))
        {
            selecionado = (selecionado - 1 + n) % n;
            mensagem = "";
            Atualizar();
        }
        else if (n > 0 && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
        {
            selecionado = (selecionado + 1) % n;
            mensagem = "";
            Atualizar();
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                 Input.GetKeyDown(KeyCode.Space))
        {
            Confirmar();
        }
    }

    private void Confirmar()
    {
        if (entradas.Count == 0) return;

        Entrada e = entradas[selecionado];

        if (e.acao == null) mensagem = e.aviso;
        else e.acao();

        Atualizar();
    }

    // ---------- AÇÕES ----------
    private void GuardarDaMao()
    {
        ItemCarregavel item = mao.Largar();
        if (item == null) return;

        if (!atual.Guardar(item))
        {
            mao.Pegar(item); // não coube: volta para a mão
            mensagem = "A empilhadeira está cheia.";
            return;
        }

        mensagem = "Guardou: " + item.Nome;
    }

    private void PegarDoInventario(int indice)
    {
        ItemCarregavel item = atual.Retirar(indice);
        if (item == null) return;

        mao.Pegar(item); // reaparece ao lado dela
        mensagem = "Pegou: " + item.Nome;
    }

    // ---------- DESENHAR O CONTEÚDO ----------
    private void AdicionarTexto(string texto)
    {
        Linha l = new Linha();
        l.texto = texto;
        l.entrada = -1;
        linhas.Add(l);
    }

    private void AdicionarEntrada(string texto, Action acao, string aviso)
    {
        Entrada e = new Entrada();
        e.texto = texto;
        e.acao = acao;
        e.aviso = aviso;
        entradas.Add(e);

        Linha l = new Linha();
        l.texto = texto;
        l.entrada = entradas.Count - 1;
        linhas.Add(l);
    }

    private void Atualizar()
    {
        if (atual == null || mao == null) return;

        entradas.Clear();
        linhas.Clear();

        AdicionarTexto("Na mão: " + (mao.Carregando ? mao.Item.Nome : "nada"));

        if (mao.Carregando)
        {
            if (atual.Cheio) AdicionarEntrada("Guardar na empilhadeira", null, "A empilhadeira está cheia.");
            else AdicionarEntrada("Guardar na empilhadeira", GuardarDaMao, null);
        }

        AdicionarTexto("");
        AdicionarTexto("Dentro da empilhadeira (" + atual.Itens.Count + "/" + atual.Capacidade + "):");

        if (atual.Itens.Count == 0) AdicionarTexto("   (vazio)");

        for (int i = 0; i < atual.Itens.Count; i++)
        {
            int indice = i;
            string nome = atual.Itens[i].Nome;

            if (mao.Carregando) AdicionarEntrada("Pegar: " + nome, null, "Sua mão está ocupada.");
            else AdicionarEntrada("Pegar: " + nome, () => PegarDoInventario(indice), null);
        }

        selecionado = entradas.Count > 0 ? Mathf.Clamp(selecionado, 0, entradas.Count - 1) : 0;

        StringBuilder sb = new StringBuilder();
        foreach (Linha l in linhas) sb.AppendLine(Formatar(l));

        if (!string.IsNullOrEmpty(mensagem))
        {
            sb.AppendLine();
            sb.AppendLine("<color=#7FD6FF>" + mensagem + "</color>");
        }

        textoCorpo.text = sb.ToString();
    }

    private string Formatar(Linha l)
    {
        if (l.entrada < 0) return l.texto;

        Entrada e = entradas[l.entrada];
        bool selecionada = l.entrada == selecionado;
        bool ativa = e.acao != null;

        string prefixo = selecionada ? "> " : "   ";
        string cor = ativa ? (selecionada ? "#FFD54A" : "#FFFFFF")
                           : (selecionada ? "#B08A55" : "#808080");

        return "<color=" + cor + ">" + prefixo + e.texto + "</color>";
    }
}