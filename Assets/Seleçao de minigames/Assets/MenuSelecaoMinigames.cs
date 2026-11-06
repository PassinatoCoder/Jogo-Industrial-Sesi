using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu independente de seleção de minigames.
/// Coloque este componente em MENU_ROOT numa cena vazia. Canvas, EventSystem,
/// áudio e todos os elementos de interface são criados automaticamente.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuSelecaoMinigames : MonoBehaviour
{
    [Serializable]
    public class LinhaDialogo
    {
        [TextArea(2, 4)] public string texto;
        public AudioClip voz;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Serializable]
    public class OpcaoMinigame
    {
        public string nome = "Novo treinamento";
        [TextArea(2, 3)] public string descricao = "Descrição do exercício.";
        [TextArea(2, 4)] public string falaDoChefe = "Quero atenção total neste exercício.";
        [Tooltip("Nome exato da cena do minigame, incluindo maiúsculas e espaços.")]
        public string cena;
        public Sprite arte;
        public AudioClip vozDoChefe;
        public bool desbloqueado = true;
        public UnityEvent aoIniciar = new UnityEvent();
    }

    [Header("Identidade")]
    [SerializeField] private string titulo = "SELEÇÃO DE MINIGAMES";
    [SerializeField] private string subtitulo = "DIVISÃO DE TREINAMENTO  //  PROGRAMA DE FORMAÇÃO";
    [SerializeField] private string nomeDoChefe = "CHEFE DE TREINAMENTO";
    [SerializeField] private Sprite fundo;
    [SerializeField] private Sprite retratoDoChefe;
    [SerializeField] private Color vermelhoIndustrial = new Color(0.62f, 0.095f, 0.075f, 1f);
    [SerializeField] private Color douradoSelecao = new Color(1f, 0.68f, 0.30f, 1f);

    [Header("Opções — vincule as cenas no Inspector")]
    [SerializeField] private OpcaoMinigame[] opcoes =
    {
        new OpcaoMinigame { nome = "SOLDAGEM", descricao = "Controle do arco, ritmo e precisão sobre a junta.", falaDoChefe = "Soldagem não perdoa pressa. Mão firme, olhos atentos. Quero ver precisão, não força." },
        new OpcaoMinigame { nome = "INSPEÇÃO DE EPIs", descricao = "Reconheça os riscos e escolha a proteção correta.", falaDoChefe = "Antes de tocar em qualquer máquina, você aprende a proteger a própria pele. Não existe atalho para segurança." },
        new OpcaoMinigame { nome = "OPERAÇÃO DE EMPILHADEIRA", descricao = "Movimente cargas sem colocar ninguém em risco.", falaDoChefe = "Carga mal movimentada vira acidente. Respeite o espaço, calcule o peso e não tenha pressa." },
        new OpcaoMinigame { nome = "MANUTENÇÃO INDUSTRIAL", descricao = "Investigue falhas e escolha o procedimento correto.", falaDoChefe = "Máquina sempre avisa antes de parar. Quero que aprenda a escutar antes de sair apertando botão." }
    };

    [Header("Diálogo de entrada — cada linha aceita voz própria")]
    [SerializeField] private LinhaDialogo[] introducao =
    {
        new LinhaDialogo { texto = "Então você é a nova recruta. Espero que tenha vindo para aprender — não para passear." },
        new LinhaDialogo { texto = "Aqui, cada estação testa uma habilidade diferente. Atenção, precisão e responsabilidade." },
        new LinhaDialogo { texto = "Escolha um exercício. Eu vou observar. Quando estiver pronta, confirme e comece." }
    };

    [Header("SFX e música — todos opcionais")]
    [SerializeField] private AudioClip musicaAmbiente;
    [SerializeField] private AudioClip navegarSfx;
    [SerializeField] private AudioClip selecionarSfx;
    [SerializeField] private AudioClip confirmarSfx;
    [SerializeField] private AudioClip voltarSfx;
    [SerializeField] private AudioClip erroSfx;
    [SerializeField] private AudioClip avancarDialogoSfx;
    [Range(0f, 1f)] [SerializeField] private float volumeSfx = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float volumeMusica = 0.32f;

    [Header("Voltar / fluxo")]
    [SerializeField] private string cenaDeRetorno;
    [SerializeField] private UnityEvent aoVoltar = new UnityEvent();
    [SerializeField] private bool tocarDialogoInicial = true;
    [SerializeField] private bool pausarTempoAoAbrir;

    private Canvas canvas;
    private AudioSource audioMusica;
    private AudioSource audioSfx;
    private Text textoDialogo;
    private Text textoNomeChefe;
    private Text textoNomeSelecionado;
    private Text textoDescricaoSelecionada;
    private Text textoAviso;
    private GameObject painelDialogoInicial;
    private GameObject painelConfirmacao;
    private Text textoConfirmacao;
    private Button[] botoes;
    private Outline[] outlines;
    private Image[] acentos;
    private int selecionado;
    private int linhaAtual;
    private bool introducaoAtiva;
    private bool confirmacaoAtiva;
    private bool digitando;
    private string falaCompleta = string.Empty;
    private Coroutine rotinaTexto;
    private float escalaTempoAnterior = 1f;
    private float avisoRestante;

    private static readonly Color TextoMarfim = new Color(0.94f, 0.91f, 0.84f, 1f);
    private static readonly Color TextoSecundario = new Color(0.68f, 0.68f, 0.66f, 1f);
    private static readonly Color PainelEscuro = new Color(0.018f, 0.023f, 0.031f, 0.96f);

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        escalaTempoAnterior = Time.timeScale;
        if (pausarTempoAoAbrir) Time.timeScale = 0f;
        GarantirAudioListener();
        CriarAudio();
        CriarInterface();
        if (tocarDialogoInicial && introducao != null && introducao.Length > 0)
            IniciarIntroducao();
        else
            EntrarNoMenu();
    }

    private void Update()
    {
        if (avisoRestante > 0f)
        {
            avisoRestante -= Time.unscaledDeltaTime;
            if (avisoRestante <= 0f && textoAviso != null) textoAviso.text = string.Empty;
        }

        if (confirmacaoAtiva)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E))
                ConfirmarOpcao();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Backspace))
                FecharConfirmacao();
            return;
        }

        if (introducaoAtiva)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                AvancarIntroducao();
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) MoverSelecao(-1);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) MoverSelecao(1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E)) AbrirConfirmacao();
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Backspace)) Voltar();
    }

    private void OnDestroy()
    {
        if (pausarTempoAoAbrir) Time.timeScale = escalaTempoAnterior;
        if (rotinaTexto != null) StopCoroutine(rotinaTexto);
    }

    private void GarantirAudioListener()
    {
        if (FindObjectOfType<AudioListener>() != null) return;
        gameObject.AddComponent<AudioListener>();
    }

    private void CriarAudio()
    {
        audioMusica = gameObject.AddComponent<AudioSource>();
        audioMusica.playOnAwake = false;
        audioMusica.loop = true;
        audioMusica.spatialBlend = 0f;
        audioMusica.volume = volumeMusica;
        audioMusica.clip = musicaAmbiente != null ? musicaAmbiente : Resources.Load<AudioClip>("MinigameMenu/audio_ambiente");
        if (audioMusica.clip != null) audioMusica.Play();

        audioSfx = gameObject.AddComponent<AudioSource>();
        audioSfx.playOnAwake = false;
        audioSfx.loop = false;
        audioSfx.spatialBlend = 0f;
        audioSfx.volume = 1f;
    }

    private void CriarInterface()
    {
        GarantirEventSystem();
        GameObject canvasGo = new GameObject("UI • Menu de Treinamento", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Sprite bgSprite = fundo != null ? fundo : Resources.Load<Sprite>("MinigameMenu/background_industrial");
        Image bg = CriarImagem("Fundo industrial", canvasGo.transform, bgSprite, Color.white);
        Preencher(bg.rectTransform);
        bg.preserveAspect = false;
        Image sombra = CriarImagem("Vinheta", canvasGo.transform, null, new Color(0.005f, 0.008f, 0.014f, 0.39f));
        Preencher(sombra.rectTransform);
        sombra.raycastTarget = false;

        // Cabeçalho com faixa e risco vermelho.
        Image faixaTopo = CriarImagem("Faixa superior", canvasGo.transform, null, new Color(0.008f, 0.01f, 0.014f, 0.85f));
        DefinirRect(faixaTopo.rectTransform, new Vector2(0f, 0.875f), new Vector2(0.73f, 1f), Vector2.zero, Vector2.zero, Vector2.zero);
        Image risco = CriarImagem("Risco de tinta vermelho", canvasGo.transform, null, vermelhoIndustrial);
        DefinirRect(risco.rectTransform, new Vector2(0.035f, 0.87f), new Vector2(0.42f, 0.878f), Vector2.zero, Vector2.zero, Vector2.zero);
        CriarTexto("Título", canvasGo.transform, titulo, 43, TextoMarfim, FontStyle.Bold,
            new Vector2(0.035f, 0.91f), new Vector2(0.70f, 0.99f), TextAnchor.MiddleLeft);
        CriarTexto("Subtítulo", canvasGo.transform, subtitulo, 14, new Color(0.78f, 0.66f, 0.53f, 1f), FontStyle.Bold,
            new Vector2(0.04f, 0.875f), new Vector2(0.68f, 0.915f), TextAnchor.MiddleLeft);

        Button voltar = CriarBotao("Voltar", canvasGo.transform, "ESC   VOLTAR",
            new Vector2(0.855f, 0.925f), new Vector2(0.975f, 0.978f), new Color(0.035f, 0.04f, 0.047f, 0.9f));
        voltar.onClick.AddListener(Voltar);

        // Retrato grande e independente para substituição fácil por arte final.
        GameObject moldura = CriarPainel("Área do instrutor", canvasGo.transform,
            new Vector2(0.025f, 0.255f), new Vector2(0.475f, 0.86f), new Color(0.01f, 0.015f, 0.023f, 0.25f));
        Sprite retrato = retratoDoChefe != null ? retratoDoChefe : Resources.Load<Sprite>("MinigameMenu/portrait_chefe");
        Image retratoImg = CriarImagem("Retrato do chefe", moldura.transform, retrato, Color.white);
        Preencher(retratoImg.rectTransform, 0f, 0f, 0f, 0f);
        retratoImg.preserveAspect = true;
        Image degradado = CriarImagem("Sombra retrato", moldura.transform, null, new Color(0f, 0f, 0f, 0.18f));
        Preencher(degradado.rectTransform);
        degradado.raycastTarget = false;

        // Caixa de fala do chefe.
        painelDialogoInicial = CriarPainel("Diálogo do chefe", canvasGo.transform,
            new Vector2(0.035f, 0.045f), new Vector2(0.49f, 0.255f), new Color(0.009f, 0.012f, 0.017f, 0.96f));
        Outline bordaDialogo = painelDialogoInicial.AddComponent<Outline>();
        bordaDialogo.effectColor = new Color(vermelhoIndustrial.r, vermelhoIndustrial.g, vermelhoIndustrial.b, 0.8f);
        bordaDialogo.effectDistance = new Vector2(2f, 2f);
        Image etiqueta = CriarImagem("Etiqueta Chefe", canvasGo.transform, null, vermelhoIndustrial);
        DefinirRect(etiqueta.rectTransform, new Vector2(0.05f, 0.225f), new Vector2(0.205f, 0.275f), Vector2.zero, Vector2.zero, Vector2.zero);
        textoNomeChefe = CriarTexto("Nome do instrutor", canvasGo.transform, nomeDoChefe, 17, Color.white, FontStyle.Bold,
            new Vector2(0.06f, 0.232f), new Vector2(0.20f, 0.268f), TextAnchor.MiddleLeft);
        textoDialogo = CriarTexto("Fala do instrutor", painelDialogoInicial.transform, string.Empty, 22, TextoMarfim, FontStyle.Normal,
            new Vector2(0.055f, 0.20f), new Vector2(0.94f, 0.86f), TextAnchor.MiddleLeft);
        textoDialogo.verticalOverflow = VerticalWrapMode.Overflow;
        CriarTexto("Avançar diálogo", painelDialogoInicial.transform, "ENTER / E   •   AVANÇAR", 12, TextoSecundario, FontStyle.Bold,
            new Vector2(0.45f, 0.025f), new Vector2(0.95f, 0.17f), TextAnchor.MiddleRight);

        // Seletor à direita.
        CriarTexto("Rótulo de opções", canvasGo.transform, "ESCOLHA SUA ESTAÇÃO", 16,
            new Color(0.88f, 0.68f, 0.43f, 1f), FontStyle.Bold,
            new Vector2(0.525f, 0.83f), new Vector2(0.96f, 0.87f), TextAnchor.MiddleLeft);
        GameObject lista = new GameObject("Lista de minigames", typeof(RectTransform));
        lista.transform.SetParent(canvasGo.transform, false);
        DefinirRect(lista.GetComponent<RectTransform>(), new Vector2(0.515f, 0.29f), new Vector2(0.965f, 0.825f), Vector2.zero, Vector2.zero, Vector2.zero);

        int total = opcoes == null ? 0 : opcoes.Length;
        botoes = new Button[total];
        outlines = new Outline[total];
        acentos = new Image[total];
        float altura = total <= 4 ? 126f : Mathf.Clamp(510f / Mathf.Max(1, total) - 8f, 74f, 116f);
        float espaco = total <= 4 ? 13f : 9f;
        for (int i = 0; i < total; i++) CriarCartao(i, lista.transform, altura, espaco);

        // Rodapé com descrição do item selecionado e controles.
        GameObject rodape = CriarPainel("Detalhes do treinamento", canvasGo.transform,
            new Vector2(0.515f, 0.095f), new Vector2(0.965f, 0.265f), new Color(0.012f, 0.017f, 0.023f, 0.92f));
        Image filete = CriarImagem("Filete dourado", rodape.transform, null, douradoSelecao);
        DefinirRect(filete.rectTransform, new Vector2(0f, 0f), new Vector2(0.006f, 1f), Vector2.zero, Vector2.zero, Vector2.zero);
        textoNomeSelecionado = CriarTexto("Nome do selecionado", rodape.transform, string.Empty, 21, douradoSelecao, FontStyle.Bold,
            new Vector2(0.035f, 0.57f), new Vector2(0.96f, 0.91f), TextAnchor.MiddleLeft);
        textoDescricaoSelecionada = CriarTexto("Descrição do selecionado", rodape.transform, string.Empty, 16, TextoSecundario, FontStyle.Normal,
            new Vector2(0.035f, 0.20f), new Vector2(0.96f, 0.59f), TextAnchor.MiddleLeft);
        textoAviso = CriarTexto("Aviso de configuração", rodape.transform, string.Empty, 12, new Color(1f, 0.62f, 0.35f, 1f), FontStyle.Bold,
            new Vector2(0.035f, 0.025f), new Vector2(0.96f, 0.19f), TextAnchor.MiddleLeft);
        CriarTexto("Controles", canvasGo.transform, "↑  ↓  NAVEGAR       E  CONFIRMAR       ESC  VOLTAR", 13,
            new Color(0.84f, 0.81f, 0.75f, 0.94f), FontStyle.Bold,
            new Vector2(0.535f, 0.045f), new Vector2(0.97f, 0.088f), TextAnchor.MiddleRight);

        CriarModalConfirmacao();
        selecionado = 0;
        AtualizarSelecao(false);
    }

    private void CriarCartao(int index, Transform parent, float altura, float gap)
    {
        OpcaoMinigame item = opcoes[index];
        GameObject go = new GameObject("Opção • " + (item != null ? item.nome : "Minigame"), typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, altura);
        rt.anchoredPosition = new Vector2(0f, -index * (altura + gap));

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.035f, 0.042f, 0.052f, 0.96f);
        bg.raycastTarget = true;
        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.None;
        int capturado = index;
        btn.onClick.AddListener(() => AoClicarOpcao(capturado));
        EventTrigger trigger = go.AddComponent<EventTrigger>();
        EventTrigger.Entry hoverEntry = new EventTrigger.Entry();
        hoverEntry.eventID = EventTriggerType.PointerEnter;
        hoverEntry.callback.AddListener((data) => SelecionarPorMouse(capturado));
        trigger.triggers.Add(hoverEntry);
        botoes[index] = btn;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = Color.clear;
        outline.effectDistance = new Vector2(3f, 3f);
        outlines[index] = outline;

        Image arte = CriarImagem("Arte do minigame", go.transform, ObterArte(item, index), Color.white);
        DefinirRect(arte.rectTransform, new Vector2(0.015f, 0.08f), new Vector2(0.245f, 0.92f), Vector2.zero, Vector2.zero, Vector2.zero);
        arte.preserveAspect = true;
        Image sombraTexto = CriarImagem("Sombra para leitura", go.transform, null, new Color(0.005f, 0.007f, 0.012f, 0.22f));
        DefinirRect(sombraTexto.rectTransform, new Vector2(0.245f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, Vector2.zero);
        sombraTexto.raycastTarget = false;
        Image acento = CriarImagem("Acento seleção", go.transform, null, douradoSelecao);
        DefinirRect(acento.rectTransform, new Vector2(0.255f, 0.13f), new Vector2(0.261f, 0.87f), Vector2.zero, Vector2.zero, Vector2.zero);
        acento.raycastTarget = false;
        acentos[index] = acento;

        Text numero = CriarTexto("Número", go.transform, (index + 1).ToString("00"), 14,
            new Color(0.82f, 0.71f, 0.58f, 1f), FontStyle.Bold,
            new Vector2(0.265f, 0.70f), new Vector2(0.315f, 0.91f), TextAnchor.MiddleCenter);
        Text nome = CriarTexto("Nome", go.transform, item != null ? item.nome : "MINIGAME", 25, TextoMarfim, FontStyle.Bold,
            new Vector2(0.325f, 0.47f), new Vector2(0.92f, 0.88f), TextAnchor.MiddleLeft);
        nome.resizeTextForBestFit = true;
        nome.resizeTextMinSize = 14;
        nome.resizeTextMaxSize = 25;
        Text descricao = CriarTexto("Descrição", go.transform, item != null ? item.descricao : string.Empty, 15,
            new Color(0.78f, 0.78f, 0.75f, 1f), FontStyle.Normal,
            new Vector2(0.325f, 0.12f), new Vector2(0.93f, 0.49f), TextAnchor.MiddleLeft);
        descricao.resizeTextForBestFit = true;
        descricao.resizeTextMinSize = 10;
        descricao.resizeTextMaxSize = 15;
        Text seta = CriarTexto("Seta", go.transform, "›", 42, douradoSelecao, FontStyle.Bold,
            new Vector2(0.93f, 0.22f), new Vector2(0.99f, 0.78f), TextAnchor.MiddleCenter);
        numero.raycastTarget = false;
        nome.raycastTarget = false;
        descricao.raycastTarget = false;
        seta.raycastTarget = false;
    }

    private Sprite ObterArte(OpcaoMinigame item, int index)
    {
        if (item != null && item.arte != null) return item.arte;
        string[] nomes = { "icon_soldagem", "icon_epis", "icon_empilhadeira", "icon_manutencao" };
        if (index < 0 || index >= nomes.Length) return null;
        return Resources.Load<Sprite>("MinigameMenu/" + nomes[index]);
    }

    private void CriarModalConfirmacao()
    {
        painelConfirmacao = CriarPainel("Modal • confirmar exercício", canvas.transform,
            Vector2.zero, Vector2.one, new Color(0.003f, 0.006f, 0.012f, 0.86f));
        painelConfirmacao.GetComponent<Image>().raycastTarget = true;
        painelConfirmacao.SetActive(false);
        GameObject caixa = CriarPainel("Caixa de confirmação", painelConfirmacao.transform,
            new Vector2(0.235f, 0.29f), new Vector2(0.765f, 0.71f), new Color(0.025f, 0.031f, 0.04f, 1f));
        Outline contorno = caixa.AddComponent<Outline>();
        contorno.effectColor = new Color(vermelhoIndustrial.r, vermelhoIndustrial.g, vermelhoIndustrial.b, 0.95f);
        contorno.effectDistance = new Vector2(3f, 3f);
        Image topo = CriarImagem("Filete vermelho", caixa.transform, null, vermelhoIndustrial);
        DefinirRect(topo.rectTransform, new Vector2(0f, 0.985f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, Vector2.zero);
        CriarTexto("Título confirmação", caixa.transform, "ORDEM DE TREINAMENTO", 28, TextoMarfim, FontStyle.Bold,
            new Vector2(0.06f, 0.75f), new Vector2(0.94f, 0.93f), TextAnchor.MiddleCenter);
        textoConfirmacao = CriarTexto("Pergunta confirmação", caixa.transform, string.Empty, 20, TextoSecundario, FontStyle.Normal,
            new Vector2(0.08f, 0.39f), new Vector2(0.92f, 0.73f), TextAnchor.MiddleCenter);
        Button confirmar = CriarBotao("Iniciar", caixa.transform, "E  •  INICIAR TREINAMENTO",
            new Vector2(0.10f, 0.11f), new Vector2(0.49f, 0.30f), new Color(0.47f, 0.075f, 0.065f, 1f));
        confirmar.onClick.AddListener(ConfirmarOpcao);
        Button cancelar = CriarBotao("Cancelar", caixa.transform, "ESC  •  VOLTAR",
            new Vector2(0.52f, 0.11f), new Vector2(0.90f, 0.30f), new Color(0.11f, 0.13f, 0.15f, 1f));
        cancelar.onClick.AddListener(FecharConfirmacao);
    }

    private void IniciarIntroducao()
    {
        introducaoAtiva = true;
        linhaAtual = 0;
        DefinirBotoesAtivos(false);
        AtualizarSelecao(false);
        MostrarLinha(introducao[linhaAtual]);
    }

    private void AvancarIntroducao()
    {
        if (!introducaoAtiva) return;
        if (digitando)
        {
            CompletarTexto();
            return;
        }
        Tocar(avancarDialogoSfx);
        linhaAtual++;
        if (linhaAtual >= introducao.Length)
        {
            introducaoAtiva = false;
            DefinirBotoesAtivos(true);
            AtualizarSelecao(true);
            return;
        }
        MostrarLinha(introducao[linhaAtual]);
    }

    private void MostrarLinha(LinhaDialogo linha)
    {
        if (linha == null) return;
        textoNomeChefe.text = nomeDoChefe;
        if (rotinaTexto != null) StopCoroutine(rotinaTexto);
        falaCompleta = linha.texto ?? string.Empty;
        rotinaTexto = StartCoroutine(DigitarTexto(falaCompleta));
        if (linha.voz != null) audioSfx.PlayOneShot(linha.voz, linha.volume * volumeSfx);
    }

    private IEnumerator DigitarTexto(string texto)
    {
        digitando = true;
        textoDialogo.text = string.Empty;
        for (int i = 0; i < texto.Length; i++)
        {
            textoDialogo.text += texto[i];
            yield return new WaitForSecondsRealtime(0.012f);
        }
        digitando = false;
        rotinaTexto = null;
    }

    private void CompletarTexto()
    {
        if (!digitando) return;
        if (rotinaTexto != null) StopCoroutine(rotinaTexto);
        rotinaTexto = null;
        textoDialogo.text = falaCompleta;
        digitando = false;
    }

    private void EntrarNoMenu()
    {
        introducaoAtiva = false;
        DefinirBotoesAtivos(true);
        AtualizarSelecao(true);
    }

    private void MoverSelecao(int direcao)
    {
        if (opcoes == null || opcoes.Length == 0) return;
        int tentativa = selecionado;
        for (int i = 0; i < opcoes.Length; i++)
        {
            tentativa = (tentativa + direcao + opcoes.Length) % opcoes.Length;
            if (opcoes[tentativa] != null && opcoes[tentativa].desbloqueado) break;
        }
        if (tentativa == selecionado) return;
        selecionado = tentativa;
        Tocar(navegarSfx);
        AtualizarSelecao(true);
    }


    private void SelecionarPorMouse(int index)
    {
        if (introducaoAtiva || confirmacaoAtiva || opcoes == null || index < 0 || index >= opcoes.Length) return;
        if (selecionado == index) return;
        selecionado = index;
        Tocar(navegarSfx);
        AtualizarSelecao(true);
    }

    private void AoClicarOpcao(int index)
    {
        if (introducaoAtiva || confirmacaoAtiva || opcoes == null || index < 0 || index >= opcoes.Length) return;
        if (selecionado != index)
        {
            selecionado = index;
            Tocar(selecionarSfx);
            AtualizarSelecao(true);
        }
        AbrirConfirmacao();
    }

    private void AtualizarSelecao(bool falar)
    {
        if (opcoes == null || opcoes.Length == 0)
        {
            if (textoNomeSelecionado != null) textoNomeSelecionado.text = "NENHUM TREINAMENTO CADASTRADO";
            if (textoDescricaoSelecionada != null) textoDescricaoSelecionada.text = "Adicione minigames no Inspector do MENU_ROOT.";
            return;
        }
        selecionado = Mathf.Clamp(selecionado, 0, opcoes.Length - 1);
        for (int i = 0; i < opcoes.Length; i++)
        {
            bool ativo = i == selecionado;
            if (outlines != null && i < outlines.Length && outlines[i] != null)
                outlines[i].effectColor = ativo ? new Color(douradoSelecao.r, douradoSelecao.g, douradoSelecao.b, 0.95f) : Color.clear;
            if (acentos != null && i < acentos.Length && acentos[i] != null) acentos[i].gameObject.SetActive(ativo);
            if (botoes != null && i < botoes.Length && botoes[i] != null)
            {
                botoes[i].transform.localScale = ativo ? Vector3.one * 1.018f : Vector3.one;
                Image img = botoes[i].targetGraphic as Image;
                if (img != null) img.color = ativo ? new Color(0.085f, 0.061f, 0.052f, 1f) : new Color(0.045f, 0.052f, 0.063f, 0.96f);
            }
        }
        OpcaoMinigame item = opcoes[selecionado];
        if (item == null) return;
        textoNomeSelecionado.text = item.nome;
        textoDescricaoSelecionada.text = item.descricao;
        textoAviso.text = !item.desbloqueado ? "TREINAMENTO BLOQUEADO" :
            string.IsNullOrWhiteSpace(item.cena) && (item.aoIniciar == null || item.aoIniciar.GetPersistentEventCount() == 0)
                ? "CONFIGURAÇÃO PENDENTE  •  VINCULE A CENA OU UM EVENTO NO INSPECTOR"
                : "E  •  CONFIRMAR EXERCÍCIO";
        if (falar)
        {
            textoNomeChefe.text = nomeDoChefe;
            if (rotinaTexto != null) StopCoroutine(rotinaTexto);
            falaCompleta = item.falaDoChefe ?? string.Empty;
            rotinaTexto = StartCoroutine(DigitarTexto(falaCompleta));
            if (item.vozDoChefe != null) audioSfx.PlayOneShot(item.vozDoChefe, volumeSfx);
        }
    }

    private void AbrirConfirmacao()
    {
        if (opcoes == null || opcoes.Length == 0) { Avisar("Nenhum minigame cadastrado no Inspector."); Tocar(erroSfx); return; }
        OpcaoMinigame item = opcoes[selecionado];
        if (item == null || !item.desbloqueado) { Avisar("Esse treinamento ainda está bloqueado."); Tocar(erroSfx); return; }
        textoConfirmacao.text = "CHEFE: \"" + item.nome + ". Quero foco absoluto. Confirma que está pronta para começar?\"";
        painelConfirmacao.SetActive(true);
        confirmacaoAtiva = true;
        Tocar(selecionarSfx);
    }

    private void ConfirmarOpcao()
    {
        if (!confirmacaoAtiva || opcoes == null || opcoes.Length == 0) return;
        OpcaoMinigame item = opcoes[selecionado];
        if (item == null) return;
        if (!string.IsNullOrWhiteSpace(item.cena))
        {
            if (!Application.CanStreamedLevelBeLoaded(item.cena))
            {
                Avisar("A cena '" + item.cena + "' não está nas configurações de build ou o nome está incorreto.");
                Tocar(erroSfx);
                return;
            }
            Tocar(confirmarSfx);
            if (item.aoIniciar != null) item.aoIniciar.Invoke();
            Time.timeScale = escalaTempoAnterior;
            SceneManager.LoadScene(item.cena);
            return;
        }
        if (item.aoIniciar != null && item.aoIniciar.GetPersistentEventCount() > 0)
        {
            Tocar(confirmarSfx);
            item.aoIniciar.Invoke();
            FecharConfirmacao();
            return;
        }
        Avisar("Vincule o nome exato da cena deste minigame ou configure o evento 'Ao Iniciar'.");
        Tocar(erroSfx);
    }

    private void FecharConfirmacao()
    {
        if (!confirmacaoAtiva) return;
        confirmacaoAtiva = false;
        painelConfirmacao.SetActive(false);
        Tocar(voltarSfx);
    }

    private void Voltar()
    {
        if (confirmacaoAtiva) { FecharConfirmacao(); return; }
        if (introducaoAtiva) { CompletarTexto(); introducaoAtiva = false; DefinirBotoesAtivos(true); AtualizarSelecao(true); return; }
        if (aoVoltar != null && aoVoltar.GetPersistentEventCount() > 0)
        {
            Tocar(voltarSfx);
            aoVoltar.Invoke();
            return;
        }
        if (!string.IsNullOrWhiteSpace(cenaDeRetorno) && Application.CanStreamedLevelBeLoaded(cenaDeRetorno))
        {
            Tocar(voltarSfx);
            Time.timeScale = escalaTempoAnterior;
            SceneManager.LoadScene(cenaDeRetorno);
            return;
        }
        Avisar("Configure 'Cena de Retorno' ou o evento 'Ao Voltar' no Inspector.");
        Tocar(erroSfx);
    }

    private void DefinirBotoesAtivos(bool ativo)
    {
        if (botoes == null) return;
        for (int i = 0; i < botoes.Length; i++) if (botoes[i] != null) botoes[i].interactable = ativo;
    }

    private void Avisar(string mensagem)
    {
        if (textoAviso == null) return;
        textoAviso.text = mensagem;
        avisoRestante = 5f;
    }

    private void Tocar(AudioClip clip)
    {
        if (clip != null && audioSfx != null) audioSfx.PlayOneShot(clip, volumeSfx);
    }

    private void GarantirEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject go = new GameObject("EventSystem • Menu", typeof(EventSystem), typeof(StandaloneInputModule));
        go.transform.SetParent(transform, false);
    }

    private GameObject CriarPainel(string nome, Transform parent, Vector2 min, Vector2 max, Color cor)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        DefinirRect(go.GetComponent<RectTransform>(), min, max, Vector2.zero, Vector2.zero, Vector2.zero);
        Image image = go.GetComponent<Image>();
        image.color = cor;
        image.raycastTarget = false;
        return go;
    }

    private Image CriarImagem(string nome, Transform parent, Sprite sprite, Color cor)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = cor;
        image.raycastTarget = false;
        return image;
    }

    private Text CriarTexto(string nome, Transform parent, string valor, int tamanho, Color cor, FontStyle estilo,
        Vector2 min, Vector2 max, TextAnchor alinhamento)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text texto = go.GetComponent<Text>();
        texto.text = valor;
        texto.font = FontePadrao();
        texto.fontSize = tamanho;
        texto.color = cor;
        texto.fontStyle = estilo;
        texto.alignment = alinhamento;
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.verticalOverflow = VerticalWrapMode.Truncate;
        texto.supportRichText = true;
        texto.raycastTarget = false;
        texto.resizeTextForBestFit = false;
        texto.margin = new RectOffset(5, 5, 3, 3);
        DefinirRect(texto.rectTransform, min, max, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        return texto;
    }

    private Button CriarBotao(string nome, Transform parent, string rotulo, Vector2 min, Vector2 max, Color cor)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        DefinirRect(go.GetComponent<RectTransform>(), min, max, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image image = go.GetComponent<Image>();
        image.color = cor;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = button.colors;
        cb.normalColor = cor;
        cb.highlightedColor = Color.Lerp(cor, douradoSelecao, 0.22f);
        cb.pressedColor = Color.Lerp(cor, Color.black, 0.25f);
        cb.selectedColor = cb.highlightedColor;
        button.colors = cb;
        Text t = CriarTexto("Texto do botão", go.transform, rotulo, 14, TextoMarfim, FontStyle.Bold,
            new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), TextAnchor.MiddleCenter);
        t.raycastTarget = false;
        return button;
    }

    private static Font FontePadrao()
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        return font;
    }

    private static void Preencher(RectTransform rt, float esquerda = 0f, float direita = 0f, float topo = 0f, float baixo = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(esquerda, baixo);
        rt.offsetMax = new Vector2(-direita, -topo);
        rt.localScale = Vector3.one;
    }

    private static void DefinirRect(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        rt.localScale = Vector3.one;
    }
}
