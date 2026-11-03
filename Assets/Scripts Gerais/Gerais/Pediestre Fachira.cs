using System.Collections;
using UnityEngine;

public class FaixaPedestre : MonoBehaviour
{
    private enum Estagio { Fora, Observando, Atravessando }

    [Header("Estágio 1 - Observar (área antes da faixa)")]
    [SerializeField] private Collider2D[] areasInicio;
    [SerializeField] private KeyCode teclaEsquerda = KeyCode.A;
    [SerializeField] private KeyCode teclaDireita = KeyCode.D;
    [SerializeField] private bool travarHorizontalNoInicio = true;

    [Header("Estágio 2 - Atravessar (a faixa)")]
    [SerializeField] private Collider2D areaFaixa;

    [Header("Empilhadeira (só aparece se ele não olhar)")]
    [SerializeField] private GameObject prefabEmpilhadeira;
    [SerializeField] private Transform pontoSurgimento;
    [SerializeField] private Transform pontoParada;
    [SerializeField] private float velocidadeChegada = 30f;
    [SerializeField] private float velocidadeSaida = 6f;

    [Tooltip("Segundos que a empilhadeira continua parada depois que a protagonista sai da faixa.")]
    [SerializeField] private float esperaAposSairDaFaixa = 1.5f;

    [Header("Quando a protagonista é atingida (tela preta)")]
    [Tooltip("Para onde ela volta. Se ficar vazio, volta para o centro da primeira área de início.")]
    [SerializeField] private Transform pontoRetorno;
    [SerializeField] private bool escurecerTela = true;
    [SerializeField] private float duracaoEscurecerAtingido = 0.1f;
    [SerializeField] private float tempoNaEscuridaoAtingido = 0.4f;
    [SerializeField] private float duracaoClarearAtingido = 0.5f;

    [Header("Empilhadeira dirigível (a faixa conta como linha amarela)")]
    [SerializeField] private bool faixaContaComoLinha = true;
    [SerializeField] private LinhaGuiaEmpilhadeira.Sentido sentidoDaFaixa = LinhaGuiaEmpilhadeira.Sentido.Cima;

    private MovimentoTopDown jogador;
    private Estagio estagio = Estagio.Fora;
    private bool olhouEsquerda;
    private bool olhouDireita;
    private bool sendoAtingido;
    private Empilhadeira empilhadeiraAtual;

    void Awake()
    {
        // A faixa vira um trecho da linha amarela para a empilhadeira dirigível
        if (faixaContaComoLinha && areaFaixa != null)
        {
            LinhaGuiaEmpilhadeira linha = areaFaixa.GetComponent<LinhaGuiaEmpilhadeira>();
            if (linha == null)
            {
                linha = areaFaixa.gameObject.AddComponent<LinhaGuiaEmpilhadeira>();
                linha.Configurar(sentidoDaFaixa, false);
            }
        }
    }

    void Start()
    {
        jogador = FindFirstObjectByType<MovimentoTopDown>();
    }

    void Update()
    {
        if (jogador == null) return;

        // Durante o efeito de ser atingida a faixa fica pausada
        if (sendoAtingido) return;

        // Dentro de uma empilhadeira a faixa não se aplica (encerra o que estava em andamento)
        if (jogador.EmVeiculo)
        {
            if (estagio == Estagio.Observando) EncerrarObservacao();
            else if (estagio == Estagio.Atravessando) EncerrarTravessia();
            return;
        }

        Vector2 pos = jogador.transform.position;
        bool noInicio = EstaNoInicio(pos);
        bool naFaixa = areaFaixa != null && areaFaixa.OverlapPoint(pos);

        switch (estagio)
        {
            case Estagio.Fora:
                if (noInicio) IniciarObservacao();
                else if (naFaixa) IniciarTravessia();
                break;

            case Estagio.Observando:
                if (naFaixa) IniciarTravessia();          // avançou: confere se olhou
                else if (!noInicio) EncerrarObservacao(); // saiu sem avançar
                else LerOlhadas();                        // continua observando
                break;

            case Estagio.Atravessando:
                if (!naFaixa) EncerrarTravessia();
                break;
        }
    }

    // ---------- ESTÁGIO 1: observar ----------
    private void IniciarObservacao()
    {
        estagio = Estagio.Observando;
        olhouEsquerda = false;
        olhouDireita = false;

        // A e D servem para olhar, então não movem a personagem aqui
        if (travarHorizontalNoInicio) jogador.TravarHorizontal = true;
    }

    private void LerOlhadas()
    {
        if (Input.GetKeyDown(teclaEsquerda)) olhouEsquerda = true;
        if (Input.GetKeyDown(teclaDireita)) olhouDireita = true;
    }

    private void EncerrarObservacao()
    {
        jogador.TravarHorizontal = false;
        olhouEsquerda = false;
        olhouDireita = false;
        estagio = Estagio.Fora;
    }

    // ---------- ESTÁGIO 2: atravessar ----------
    private void IniciarTravessia()
    {
        estagio = Estagio.Atravessando;
        jogador.TravarHorizontal = false;

        // Já existe uma empilhadeira (voltou para a faixa enquanto ela esperava): ela segura de novo
        if (empilhadeiraAtual != null)
        {
            empilhadeiraAtual.Liberada = false;
            return;
        }

        // Olhou para os dois lados: atravessa normalmente
        if (olhouEsquerda && olhouDireita) return;

        // Avançou sem olhar: a empilhadeira vem muito rápido e para antes
        ChamarEmpilhadeira();
    }

    private void EncerrarTravessia()
    {
        // Ela saiu da faixa: libera a empilhadeira (que ainda espera um pouco antes de seguir).
        // A referência NÃO é zerada aqui; ela só some quando a empilhadeira é destruída.
        if (empilhadeiraAtual != null)
            empilhadeiraAtual.Liberada = true;

        olhouEsquerda = false;
        olhouDireita = false;
        estagio = Estagio.Fora;
    }

    private void ChamarEmpilhadeira()
    {
        if (empilhadeiraAtual != null) return;

        if (prefabEmpilhadeira == null || pontoSurgimento == null || pontoParada == null)
        {
            Debug.LogWarning("FaixaPedestre: falta prefab, ponto de surgimento ou ponto de parada.");
            return;
        }

        GameObject go = Instantiate(prefabEmpilhadeira, pontoSurgimento.position, Quaternion.identity);
        empilhadeiraAtual = go.GetComponent<Empilhadeira>();

        if (empilhadeiraAtual == null)
        {
            Debug.LogWarning("FaixaPedestre: o prefab não tem o script Empilhadeira.");
            Destroy(go);
            return;
        }

        empilhadeiraAtual.AoAtingirJogador = AoSerAtingido;
        empilhadeiraAtual.PassarPelaFaixa(pontoParada.position, velocidadeChegada,
                                          velocidadeSaida, esperaAposSairDaFaixa);
    }

    // ---------- ATINGIDA: tela preta, volta ao início ----------
    private void AoSerAtingido(MovimentoTopDown atingido)
    {
        if (sendoAtingido || atingido == null) return;
        StartCoroutine(RotinaAtingido(atingido));
    }

    private IEnumerator RotinaAtingido(MovimentoTopDown atingido)
    {
        sendoAtingido = true;

        atingido.PodeMover = false;
        atingido.Parar();
        atingido.TravarHorizontal = false;

        if (escurecerTela)
            yield return FadeTela.Instancia.Escurecer(duracaoEscurecerAtingido);

        // Tudo preto: a empilhadeira some e a protagonista volta
        if (empilhadeiraAtual != null)
            Destroy(empilhadeiraAtual.gameObject);
        empilhadeiraAtual = null;

        atingido.Teleportar(PegarPontoRetorno(atingido));

        olhouEsquerda = false;
        olhouDireita = false;
        estagio = Estagio.Fora;

        if (escurecerTela)
        {
            if (tempoNaEscuridaoAtingido > 0f)
                yield return new WaitForSeconds(tempoNaEscuridaoAtingido);

            yield return FadeTela.Instancia.Clarear(duracaoClarearAtingido);
        }

        atingido.PodeMover = true;
        sendoAtingido = false;
    }

    private Vector2 PegarPontoRetorno(MovimentoTopDown atingido)
    {
        if (pontoRetorno != null) return pontoRetorno.position;

        if (areasInicio != null && areasInicio.Length > 0 && areasInicio[0] != null)
            return areasInicio[0].bounds.center;

        return atingido.transform.position;
    }

    private bool EstaNoInicio(Vector2 pos)
    {
        foreach (Collider2D area in areasInicio)
        {
            if (area != null && area.OverlapPoint(pos)) return true;
        }
        return false;
    }
}