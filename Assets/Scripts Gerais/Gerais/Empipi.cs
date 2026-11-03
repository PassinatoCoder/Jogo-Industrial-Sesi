using System;
using System.Collections;
using UnityEngine;

public class Empilhadeira : MonoBehaviour
{
    [Header("Linha amarela")]
    [SerializeField] private float tempoMaximoDeVida = 5f;

    [Header("Faixa de pedestre")]
    [SerializeField] private float distanciaFrenagem = 4f;
    [SerializeField] private float tempoParaSumir = 2f;

    // A faixa de pedestre muda para true quando a protagonista sai da faixa
    public bool Liberada { get; set; }

    // Quem quiser reagir a atropelamento define isso (a faixa de pedestre faz). Na linha amarela fica vazio.
    public Action<MovimentoTopDown> AoAtingirJogador { get; set; }

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    // O collider da prefab precisa estar como Trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        if (AoAtingirJogador == null) return;

        MovimentoTopDown jogador = other.GetComponentInParent<MovimentoTopDown>();
        if (jogador == null) return;

        AoAtingirJogador(jogador);
    }

    // ---------- LINHA AMARELA: vem em velocidade total, sem frear ----------
    // Avisa (aoChegarPerto) quando chega a "distanciaGatilho" do alvo e continua correndo.
    // Quem chamou é responsável por destruir a empilhadeira (ela também some sozinha por segurança).
    public void AtacarJogador(Transform alvo, float velocidade, float distanciaGatilho, Action aoChegarPerto)
    {
        StartCoroutine(RotinaAtacar(alvo, velocidade, distanciaGatilho, aoChegarPerto));
    }

    private IEnumerator RotinaAtacar(Transform alvo, float velocidade, float distanciaGatilho, Action aoChegarPerto)
    {
        Vector3 dir = Vector3.right;
        bool avisou = false;
        bool direcaoFixa = false;
        float tempo = 0f;

        while (tempo < tempoMaximoDeVida)
        {
            if (alvo != null)
            {
                Vector3 destino = alvo.position;
                destino.z = transform.position.z;

                Vector3 d = destino - transform.position;
                float dist = d.magnitude;

                // Chegou na distância do gatilho: avisa uma única vez
                if (!avisou && dist <= distanciaGatilho)
                {
                    avisou = true;
                    if (aoChegarPerto != null) aoChegarPerto();
                }

                // Mira no alvo; quando está colada nele, trava a direção e segue reto
                if (!direcaoFixa)
                {
                    if (dist > 0.5f)
                    {
                        dir = d.normalized;
                        Virar(dir);
                    }
                    else
                    {
                        direcaoFixa = true;
                    }
                }
            }

            transform.position += dir * velocidade * Time.deltaTime;
            tempo += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    // ---------- FAIXA DE PEDESTRE: chega MUITO rápido, freia seco antes, espera, segue reto e some ----------
    public void PassarPelaFaixa(Vector3 pontoParada, float velocidadeChegada,
                                float velocidadeSaida, float esperaAposLiberar)
    {
        StartCoroutine(RotinaFaixa(pontoParada, velocidadeChegada, velocidadeSaida, esperaAposLiberar));
    }

    private IEnumerator RotinaFaixa(Vector3 pontoParada, float velocidadeChegada,
                                    float velocidadeSaida, float esperaAposLiberar)
    {
        pontoParada.z = transform.position.z;
        Vector3 dir = (pontoParada - transform.position).normalized;
        Virar(dir);

        // Chegada em velocidade total e freada com desaceleração constante
        float dist = Vector3.Distance(transform.position, pontoParada);
        while (dist > 0.02f)
        {
            float v = Mathf.Min(velocidadeChegada, velocidadeChegada * Mathf.Sqrt(dist / distanciaFrenagem));
            v = Mathf.Max(v, 1.5f);

            transform.position = Vector3.MoveTowards(transform.position, pontoParada, v * Time.deltaTime);
            dist = Vector3.Distance(transform.position, pontoParada);
            yield return null;
        }

        transform.position = pontoParada;

        // Espera a protagonista sair da faixa...
        yield return new WaitUntil(() => Liberada);

        // ...e ainda fica parada por mais um tempo antes de seguir em frente
        if (esperaAposLiberar > 0f)
            yield return new WaitForSeconds(esperaAposLiberar);

        // Segue reto e some depois de 2 segundos
        float t = 0f;
        while (t < tempoParaSumir)
        {
            transform.position += dir * velocidadeSaida * Time.deltaTime;
            t += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Virar(Vector3 dir)
    {
        if (sr != null && Mathf.Abs(dir.x) > 0.01f)
            sr.flipX = dir.x < 0f;
    }
}