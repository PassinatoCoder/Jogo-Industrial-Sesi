using System.Collections;
using UnityEngine;

// Coloque no terminal do painel geral, na fábrica (um Collider2D é opcional).
// Ao usar, a câmera vai para a vista do painel; ao sair, volta para a protagonista.
public class PainelGeral : MonoBehaviour
{
    [Header("Terminal (onde a protagonista usa o painel)")]
    [SerializeField] private KeyCode teclaUsar = KeyCode.F;
    [SerializeField] private float distanciaParaUsar = 1.5f;

    [Header("Painel (para onde a câmera vai)")]
    [Tooltip("Ponto no centro da vista do painel (deixe em Z = 0).")]
    [SerializeField] private Transform pontoDaCamera;
    [Tooltip("Se vazio, usa todas as ChaveDoSetor da cena, da esquerda para a direita.")]
    [SerializeField] private ChaveDoSetor[] chaves;

    [Header("Transição")]
    [Tooltip("Escurece a tela, pula a câmera e clareia. Desligado, a câmera desliza pelo mapa.")]
    [SerializeField] private bool usarFade = true;
    [SerializeField] private float duracaoEscurecer = 0.25f;
    [SerializeField] private float duracaoClarear = 0.25f;

    private MovimentoTopDown jogador;
    private CameraSegue cameraSegue;
    private Camera cam;
    private Collider2D colisor;
    private int selecionada;
    private bool emUso;
    private bool emTransicao;

    void Awake()
    {
        colisor = GetComponent<Collider2D>();
    }

    void Start()
    {
        jogador = FindFirstObjectByType<MovimentoTopDown>();
        cameraSegue = FindFirstObjectByType<CameraSegue>();

        if (cameraSegue != null) cam = cameraSegue.GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (chaves == null || chaves.Length == 0)
        {
            chaves = FindObjectsByType<ChaveDoSetor>(FindObjectsSortMode.None);
            System.Array.Sort(chaves, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        }

        if (pontoDaCamera == null)
            Debug.LogWarning("PainelGeral: falta o ponto da câmera (a vista do painel).");

        if (chaves.Length == 0)
            Debug.LogWarning("PainelGeral: não encontrei nenhuma ChaveDoSetor.");
    }

    void Update()
    {
        if (jogador == null || cameraSegue == null || pontoDaCamera == null) return;
        if (chaves == null || chaves.Length == 0 || emTransicao) return;

        if (!emUso)
        {
            if (Input.GetKeyDown(teclaUsar) && PodeUsar())
                StartCoroutine(Entrar());
            return;
        }

        LerEntrada();
        AtualizarDica();
    }

    private bool PodeUsar()
    {
        if (!jogador.gameObject.activeInHierarchy) return false;
        if (jogador.EmVeiculo || jogador.EmPerigo || !jogador.PodeMover) return false;
        if (InventarioUI.EstaAberto) return false;

        Vector2 p = jogador.transform.position;

        if (colisor != null)
            return Vector2.Distance(colisor.ClosestPoint(p), p) <= distanciaParaUsar;

        return Vector2.Distance(transform.position, p) <= distanciaParaUsar;
    }

    // ---------- ENTRAR / SAIR ----------
    private IEnumerator Entrar()
    {
        emTransicao = true;

        jogador.Parar();
        jogador.PodeMover = false;

        if (usarFade) yield return FadeTela.Instancia.Escurecer(duracaoEscurecer);

        cameraSegue.DefinirAlvo(pontoDaCamera, usarFade);

        selecionada = PrimeiraComProblema();
        MarcarSelecionada();

        if (usarFade) yield return FadeTela.Instancia.Clarear(duracaoClarear);

        emUso = true;
        emTransicao = false;
    }

    private IEnumerator Sair()
    {
        emTransicao = true;
        emUso = false;

        if (usarFade) yield return FadeTela.Instancia.Escurecer(duracaoEscurecer);

        cameraSegue.DefinirAlvo(jogador.transform, usarFade);

        foreach (ChaveDoSetor c in chaves)
        {
            if (c != null) c.DefinirSelecionada(false);
        }

        AvisoTela.Instancia.LimparDica();

        if (usarFade) yield return FadeTela.Instancia.Clarear(duracaoClarear);

        jogador.PodeMover = true;
        emTransicao = false;
    }

    // ---------- ENTRADA NO PAINEL ----------
    private void LerEntrada()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(teclaUsar))
        {
            StartCoroutine(Sair());
            return;
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            Mover(-1);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) ||
                 Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            Mover(1);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                 Input.GetKeyDown(KeyCode.Space))
        {
            Alternar(selecionada);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            CliqueDoMouse();
        }
    }

    private void Mover(int passo)
    {
        int n = chaves.Length;
        selecionada = (selecionada + passo + n) % n;
        MarcarSelecionada();
    }

    private void CliqueDoMouse()
    {
        if (cam == null) return;

        Vector2 p = cam.ScreenToWorldPoint(Input.mousePosition);
        Collider2D acertou = Physics2D.OverlapPoint(p);
        if (acertou == null) return;

        ChaveDoSetor chave = acertou.GetComponentInParent<ChaveDoSetor>();
        int indice = System.Array.IndexOf(chaves, chave);
        if (indice < 0) return;

        selecionada = indice;
        MarcarSelecionada();
        Alternar(indice);
    }

    private void Alternar(int indice)
    {
        ChaveDoSetor chave = chaves[indice];
        if (chave == null || chave.Setor == null) return;

        SetorFabrica setor = chave.Setor;
        bool tinhaProblema = setor.ComProblema;
        bool estavaLigado = setor.Ligado;

        chave.Alternar();

        if (tinhaProblema && !setor.ComProblema)
            AvisoTela.Instancia.Mostrar("Problema no setor " + setor.Nome + " resolvido!", 3f,
                                        new Color(0.45f, 1f, 0.5f));
        else if (estavaLigado && !setor.Ligado)
            AvisoTela.Instancia.Mostrar("Você desligou o setor " + setor.Nome + " sem nenhum problema.", 3f,
                                        new Color(1f, 0.85f, 0.3f));
        else if (!estavaLigado && setor.Ligado)
            AvisoTela.Instancia.Mostrar("Setor " + setor.Nome + " ligado.", 2f, Color.white);
    }

    // ---------- SELEÇÃO E TEXTO ----------
    private int PrimeiraComProblema()
    {
        for (int i = 0; i < chaves.Length; i++)
        {
            if (chaves[i] != null && chaves[i].Setor != null && chaves[i].Setor.ComProblema) return i;
        }
        return 0;
    }

    private void MarcarSelecionada()
    {
        for (int i = 0; i < chaves.Length; i++)
        {
            if (chaves[i] != null) chaves[i].DefinirSelecionada(i == selecionada);
        }
    }

    private void AtualizarDica()
    {
        string estado = "";
        SetorFabrica s = chaves[selecionada] != null ? chaves[selecionada].Setor : null;

        if (s != null)
            estado = s.Nome + ": " + (s.Ligado ? "LIGADO" : "DESLIGADO") + (s.ComProblema ? "  - PROBLEMA!" : "");

        AvisoTela.Instancia.DefinirDica(estado + "\nA/D ou setas: escolher   ENTER ou clique: alternar   ESC / " +
                                        teclaUsar + ": voltar");
    }
}