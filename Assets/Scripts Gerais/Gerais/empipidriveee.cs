using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EmpilhadeiraDirigivel : MonoBehaviour
{
    [Header("Entrar e sair")]
    [SerializeField] private KeyCode teclaEntrarSair = KeyCode.E;
    [SerializeField] private float distanciaParaEntrar = 1.5f;
    [SerializeField] private float distanciaSaida = 1.8f;
    [SerializeField] private float raioLivreSaida = 0.4f;
    [SerializeField] private float velocidadeMaximaParaSair = 0.5f;

    [Header("Direção")]
    [SerializeField] private float velocidadeMaxima = 7f;
    [SerializeField] private float aceleracao = 20f;
    [SerializeField] private float frenagem = 25f;

    [Header("Fora da zona livre (só linhas amarelas)")]
    [Tooltip("Distância máxima do centro da empilhadeira até a linha para ela ser 'capturada' pela linha.")]
    [SerializeField] private float folgaDaLinha = 0.5f;
    [SerializeField] private float forcaAlinhamento = 10f;
    [Tooltip("Distância mínima à frente que é testada para não sair do caminho permitido.")]
    [SerializeField] private float antecipacao = 0.2f;
    [Tooltip("Se ligado, a empilhadeira pode voltar (contra o sentido) na linha em que está.")]
    [SerializeField] private bool permitirVoltarNaLinha = true;
    [Tooltip("Escreve no Console quando ela começa a voltar numa linha.")]
    [SerializeField] private bool avisarNoLog = true;

    [Header("Sprites (opcional)")]
    [SerializeField] private Sprite spriteLado;   // desenhado olhando para a direita
    [SerializeField] private Sprite spriteCima;
    [SerializeField] private Sprite spriteBaixo;

    // Só uma empilhadeira é dirigida por vez
    private static EmpilhadeiraDirigivel veiculoAtual;

    private Rigidbody2D rb;
    private Collider2D colisor;
    private SpriteRenderer sr;
    private MovimentoTopDown jogador;
    private CameraSegue cameraSegue;
    private Vector2 entrada;
    private bool estavaVoltando;

    // Só restringe se a cena tiver pelo menos uma zona livre
    private bool RegrasAtivas { get { return ZonaLivreEmpilhadeira.ExisteAlguma; } }

    private Vector2 PosicaoCentro { get { return colisor.bounds.center; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstatico()
    {
        veiculoAtual = null;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        colisor = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.bodyType = RigidbodyType2D.Kinematic; // parada: ninguém consegue empurrar
    }

    void Start()
    {
        jogador = FindFirstObjectByType<MovimentoTopDown>();
        cameraSegue = FindFirstObjectByType<CameraSegue>();

        if (jogador == null)
            Debug.LogWarning("EmpilhadeiraDirigivel: não encontrei a protagonista (MovimentoTopDown) na cena.");
    }

    void Update()
    {
        if (jogador == null) return;

        if (veiculoAtual == this)
        {
            entrada = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            AtualizarVisual(entrada);

            if (Input.GetKeyDown(teclaEntrarSair)) Sair();
        }
        else if (veiculoAtual == null && Input.GetKeyDown(teclaEntrarSair) && JogadorPerto())
        {
            Entrar();
        }
    }

    void FixedUpdate()
    {
        if (veiculoAtual != this) return;

        Vector2 pos = PosicaoCentro;
        Vector2 alvo = entrada * velocidadeMaxima;
        LinhaGuiaEmpilhadeira linha = null;
        bool voltando = false;

        // Fora da zona livre: só anda sobre a linha, no eixo dela
        if (RegrasAtivas && !ZonaLivreEmpilhadeira.Contem(pos))
        {
            linha = LinhaGuiaEmpilhadeira.Escolher(pos, entrada, folgaDaLinha);
            alvo = Vector2.zero;

            if (linha != null)
            {
                float projecao = Vector2.Dot(entrada, linha.Direcao);

                if (projecao > 0.1f)
                {
                    alvo = linha.Direcao * velocidadeMaxima;
                }
                else if (projecao < -0.1f && permitirVoltarNaLinha)
                {
                    alvo = -linha.Direcao * velocidadeMaxima;
                    voltando = true;
                }
            }
        }

        AvisarVoltaNaLinha(voltando, linha);

        // Acelera na direção apertada e freia quando solta
        float taxa = alvo.sqrMagnitude > 0.01f ? aceleracao : frenagem;
        Vector2 v = Vector2.MoveTowards(rb.linearVelocity, alvo, taxa * Time.fixedDeltaTime);

        if (linha != null)
        {
            Vector2 dir = linha.Direcao;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            // Só no eixo da linha (para trás só se for permitido)
            float aoLongo = Vector2.Dot(v, dir);
            if (!permitirVoltarNaLinha) aoLongo = Mathf.Max(0f, aoLongo);

            // Puxa suavemente para o centro da linha
            float lateral = 0f;
            if (linha.AlinharAoCentro)
            {
                float erro = Vector2.Dot(linha.Centro - pos, perp);
                lateral = Mathf.Clamp(erro * forcaAlinhamento, -velocidadeMaxima, velocidadeMaxima);
            }

            v = dir * aoLongo + perp * lateral;
        }

        rb.linearVelocity = FiltrarMovimento(pos, v);
    }

    // Avisa no Console uma vez, quando começa a voltar na linha
    private void AvisarVoltaNaLinha(bool voltando, LinhaGuiaEmpilhadeira linha)
    {
        if (voltando && !estavaVoltando && avisarNoLog && linha != null)
        {
            Debug.Log("EmpilhadeiraDirigivel: voltando na linha '" + linha.name +
                      "' (contra o sentido permitido).", linha);
        }

        estavaVoltando = voltando;
    }

    // ---------- REGRAS DE ONDE PODE ESTAR ----------
    private bool PosicaoValida(Vector2 p)
    {
        return ZonaLivreEmpilhadeira.Contem(p) || LinhaGuiaEmpilhadeira.EmAlguma(p, folgaDaLinha);
    }

    // Corta o movimento que levaria para fora da zona livre / das linhas
    private Vector2 FiltrarMovimento(Vector2 pos, Vector2 v)
    {
        if (!RegrasAtivas || v.sqrMagnitude < 0.0001f) return v;

        float passo = Mathf.Max(antecipacao, v.magnitude * Time.fixedDeltaTime * 2f);

        if (PosicaoValida(pos + v.normalized * passo)) return v;

        // Tenta deslizar só num dos eixos (para costear a borda da zona)
        if (Mathf.Abs(v.x) > 0.01f && PosicaoValida(pos + new Vector2(Mathf.Sign(v.x) * passo, 0f)))
            return new Vector2(v.x, 0f);

        if (Mathf.Abs(v.y) > 0.01f && PosicaoValida(pos + new Vector2(0f, Mathf.Sign(v.y) * passo)))
            return new Vector2(0f, v.y);

        return Vector2.zero;
    }

    // ---------- ENTRAR ----------
    private bool JogadorPerto()
    {
        if (!jogador.gameObject.activeInHierarchy) return false;
        if (jogador.EmPerigo || !jogador.PodeMover) return false;

        Vector2 p = jogador.transform.position;
        Vector2 maisProximo = colisor.ClosestPoint(p);
        return Vector2.Distance(maisProximo, p) <= distanciaParaEntrar;
    }

    private void Entrar()
    {
        veiculoAtual = this;
        estavaVoltando = false;

        jogador.Parar();
        jogador.PodeMover = false;
        jogador.EmVeiculo = true;
        jogador.gameObject.SetActive(false);

        rb.bodyType = RigidbodyType2D.Dynamic;

        if (cameraSegue != null) cameraSegue.DefinirAlvo(transform);
    }

    // ---------- SAIR ----------
    private void Sair()
    {
        // Precisa estar quase parada
        if (rb.linearVelocity.magnitude > velocidadeMaximaParaSair) return;

        Vector2 ponto;
        if (!AcharPontoDeSaida(out ponto))
        {
            Debug.Log("EmpilhadeiraDirigivel: sem espaço livre para sair aqui.");
            return;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        entrada = Vector2.zero;
        estavaVoltando = false;

        jogador.gameObject.SetActive(true);
        jogador.Teleportar(ponto);
        jogador.EmVeiculo = false;
        jogador.PodeMover = true;

        if (cameraSegue != null) cameraSegue.DefinirAlvo(jogador.transform);

        veiculoAtual = null;
    }

    private bool AcharPontoDeSaida(out Vector2 ponto)
    {
        Vector2[] direcoes = { Vector2.right, Vector2.left, Vector2.down, Vector2.up };

        foreach (Vector2 d in direcoes)
        {
            Vector2 candidato = (Vector2)transform.position + d * distanciaSaida;
            if (PontoLivre(candidato))
            {
                ponto = candidato;
                return true;
            }
        }

        ponto = transform.position;
        return false;
    }

    private bool PontoLivre(Vector2 p)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(p, raioLivreSaida);

        foreach (Collider2D h in hits)
        {
            if (h.attachedRigidbody == rb) continue;                            // a própria empilhadeira
            if (h.GetComponentInParent<LinhaPerigosa>() != null) return false;  // nunca sai em cima de linha
            if (!h.isTrigger) return false;                                     // parede ou objeto sólido
        }

        return true;
    }

    // ---------- VISUAL ----------
    private void AtualizarVisual(Vector2 dir)
    {
        if (sr == null || dir.sqrMagnitude < 0.01f) return;

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            if (spriteLado != null) sr.sprite = spriteLado;
            sr.flipX = dir.x < 0f;
        }
        else
        {
            Sprite s = dir.y > 0f ? spriteCima : spriteBaixo;
            if (s != null) sr.sprite = s;
            sr.flipX = false;
        }
    }
}