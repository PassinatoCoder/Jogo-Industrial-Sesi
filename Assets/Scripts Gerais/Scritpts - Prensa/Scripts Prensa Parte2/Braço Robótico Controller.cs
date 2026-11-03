using UnityEngine;

/// <summary>
/// Controla o braço robótico: estende verticalmente até a posição de trabalho,
/// libera o controle horizontal por A/D (ou setas) enquanto ajusta a placa, agarra
/// a placa automaticamente ao encostar nela (colisão de trigger) e a solta na
/// posição atual assim que o jogador aperta o botão de novo, recolhendo sozinho.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BracoRoboticoController : MonoBehaviour
{
    public enum EstadoBraco
    {
        Recolhido,
        Estendendo,
        AjustandoPlaca,
        Recolhendo
    }

    [Header("Posições")]
    public Transform PosicaoInicial;
    public Transform PosicaoTrabalho;

    [Header("Movimento vertical (estender/recolher)")]
    public float VelocidadeMovimento = 3f;
    public float ToleranciaChegada = 0.02f;

    [Header("Controle horizontal (A/D) durante o ajuste")]
    public float VelocidadeTeclado = 3f;

    [Header("Estado atual")]
    public EstadoBraco Estado = EstadoBraco.Recolhido;

    private Transform PlacaSegurada;

    /// <summary>Verdadeiro apenas quando o braço está parado na posição de trabalho, liberando o controle por teclado.</summary>
    public bool PodeAjustarPlaca => Estado == EstadoBraco.AjustandoPlaca;

    /// <summary>Verdadeiro enquanto o braço está em movimento (usado pelo BotaoBraco pra ignorar cliques nesse período).</summary>
    public bool EmTransicao => Estado == EstadoBraco.Estendendo || Estado == EstadoBraco.Recolhendo;

    void Reset()
    {
        // lembrete: o Collider2D deste objeto precisa estar marcado como "Is Trigger",
        // senão o braço vai colidir fisicamente com a placa em vez de agarrá-la
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Awake()
    {
        // Rigidbody2D cinemático: o braço é movido só por transform, nunca por física,
        // mas precisa de um Rigidbody2D pra que o OnTriggerEnter2D funcione de forma confiável.
        Rigidbody2D Rb = GetComponent<Rigidbody2D>();
        Rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Start()
    {
        if (PosicaoInicial != null)
            transform.position = PosicaoInicial.position;
    }

    void Update()
    {
        switch (Estado)
        {
            case EstadoBraco.Estendendo:
                if (MoverPara(PosicaoTrabalho))
                    Estado = EstadoBraco.AjustandoPlaca;
                break;

            case EstadoBraco.AjustandoPlaca:
                ControlarPorTeclado();
                break;

            case EstadoBraco.Recolhendo:
                if (MoverPara(PosicaoInicial))
                    Estado = EstadoBraco.Recolhido;
                break;
        }
    }

    void ControlarPorTeclado()
    {
        float Direcao = Input.GetAxisRaw("Horizontal"); // A/D e setas esquerda/direita por padrão no Unity
        if (Mathf.Abs(Direcao) > 0f)
            transform.position += new Vector3(Direcao * VelocidadeTeclado * Time.deltaTime, 0f, 0f);
    }

    void OnTriggerEnter2D(Collider2D Outro)
    {
        if (Estado != EstadoBraco.AjustandoPlaca)
            return;

        if (PlacaSegurada != null)
            return; // já está segurando uma placa

        PlacaMetal Placa = Outro.GetComponent<PlacaMetal>();
        if (Placa == null)
            return;

        PlacaSegurada = Placa.transform;

        // acopla de verdade: a placa vira filha do braço, então passa a se mover
        // junto automaticamente (mesma lógica de transform do Unity), sem nenhum
        // código de posição rodando na placa. "true" mantém a posição de mundo
        // atual, então ela não pula/teleporta ao ser acoplada.
        PlacaSegurada.SetParent(transform, true);
    }

    /// <summary>
    /// Chamado pelo BotaoBraco. Alterna entre estender e recolher.
    /// Ao recolher, solta a placa (se estiver segurando uma) na posição atual.
    /// Cliques são ignorados enquanto o braço já está em movimento.
    /// </summary>
    public void AlternarBraco()
    {
        if (EmTransicao)
            return;

        if (Estado == EstadoBraco.Recolhido)
        {
            Estado = EstadoBraco.Estendendo;
        }
        else if (Estado == EstadoBraco.AjustandoPlaca)
        {
            if (PlacaSegurada != null)
            {
                // desacopla: a placa deixa de ser filha do braço e não se move mais,
                // ficando parada exatamente na posição de mundo em que estava
                PlacaSegurada.SetParent(null, true);
                PlacaSegurada = null;
            }

            Estado = EstadoBraco.Recolhendo;
        }
    }

    bool MoverPara(Transform Alvo)
    {
        if (Alvo == null)
            return true;

        transform.position = Vector3.MoveTowards(transform.position, Alvo.position, VelocidadeMovimento * Time.deltaTime);
        return Vector3.Distance(transform.position, Alvo.position) < ToleranciaChegada;
    }
}