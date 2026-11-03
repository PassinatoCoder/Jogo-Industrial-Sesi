using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LinhaPerigosa : MonoBehaviour
{
    public enum Tipo { Amarela, Preta }

    [SerializeField] private Tipo tipo = Tipo.Amarela;

    [Header("Posição (as duas linhas)")]
    [SerializeField] private float distanciaSegura = 2f;
    [SerializeField] private bool escurecerTela = true;

    [Header("Linha preta (rápida)")]
    [SerializeField] private float duracaoEscurecerPreta = 0.1f;
    [SerializeField] private float tempoNaEscuridaoPreta = 0.1f;
    [SerializeField] private float duracaoClarearPreta = 0.25f;

    [Header("Linha amarela - tela escura")]
    [SerializeField] private float tempoNaEscuridao = 0.4f;
    [SerializeField] private float duracaoClarear = 0.5f;

    [Header("Linha amarela - empilhadeira")]
    [SerializeField] private float tempoAjusteAoCentro = 0.1f;
    [SerializeField] private GameObject prefabEmpilhadeira;
    [SerializeField] private bool empilhadeiraVemDoOutroLado = false;
    [SerializeField] private float distanciaSurgimento = 12f;
    [SerializeField] private float velocidadeEmpilhadeira = 25f;

    [Tooltip("Distância entre a empilhadeira e o jogador em que ela some e a tela fica preta na hora.")]
    [SerializeField] private float distanciaParaEscurecer = 5f;

    // Preto instantâneo (valor mínimo para evitar divisão por zero no fade)
    private const float duracaoPretoInstantaneo = 0.01f;

    private Collider2D area;
    private Empilhadeira empilhadeiraDaCena;

    void Awake()
    {
        area = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        MovimentoTopDown jogador = other.GetComponentInParent<MovimentoTopDown>();
        if (jogador == null || jogador.EmPerigo) return;

        StartCoroutine(RotinaPerigo(jogador, other));
    }

    private IEnumerator RotinaPerigo(MovimentoTopDown jogador, Collider2D colisorJogador)
    {
        jogador.EmPerigo = true;
        jogador.PodeMover = false;
        jogador.Parar();

        // Diferença entre o pivot da protagonista e o centro do collider dela
        Vector2 c = colisorJogador.bounds.center;
        Vector2 desloc = c - (Vector2)jogador.transform.position;

        // Geometria da linha (funciona para linhas retas, sem rotação)
        Bounds b = area.bounds;
        bool horizontal = b.size.x >= b.size.y;

        Vector2 centro;
        Vector2 pontoSeguro;
        Vector2 eixoDaLinha;

        if (horizontal)
        {
            float lado = c.y >= b.center.y ? 1f : -1f;
            centro = new Vector2(c.x, b.center.y);
            pontoSeguro = new Vector2(c.x, b.center.y + lado * (b.extents.y + distanciaSegura));
            eixoDaLinha = Vector2.right;
        }
        else
        {
            float lado = c.x >= b.center.x ? 1f : -1f;
            centro = new Vector2(b.center.x, c.y);
            pontoSeguro = new Vector2(b.center.x + lado * (b.extents.x + distanciaSegura), c.y);
            eixoDaLinha = Vector2.up;
        }

        Vector2 destinoDoPivot = pontoSeguro - desloc;

        if (tipo == Tipo.Preta)
            yield return RotinaPreta(jogador, destinoDoPivot);
        else
            yield return RotinaAmarela(jogador, centro - desloc, destinoDoPivot, eixoDaLinha);

        jogador.PodeMover = true;
        jogador.EmPerigo = false;
    }

    // ---------- LINHA PRETA: pisou, tela preta, voltou ----------
    private IEnumerator RotinaPreta(MovimentoTopDown jogador, Vector2 destino)
    {
        if (escurecerTela)
            yield return FadeTela.Instancia.Escurecer(duracaoEscurecerPreta);

        jogador.Teleportar(destino);

        if (escurecerTela)
        {
            if (tempoNaEscuridaoPreta > 0f)
                yield return new WaitForSeconds(tempoNaEscuridaoPreta);

            yield return FadeTela.Instancia.Clarear(duracaoClarearPreta);
        }
    }

    // ---------- LINHA AMARELA: trava no meio, empilhadeira vem, ela some e a tela fica preta na hora, volta ----------
    private IEnumerator RotinaAmarela(MovimentoTopDown jogador, Vector2 meioDaLinha,
                                      Vector2 destino, Vector2 eixoDaLinha)
    {
        // 1) Trava bem no meio da linha
        yield return jogador.DeslizarAte(meioDaLinha, tempoAjusteAoCentro);

        // 2) Empilhadeira vem até a distância do gatilho
        yield return RotinaEmpilhadeira(jogador, eixoDaLinha);

        // 3) Chegou perto: a empilhadeira some e a tela fica preta total NA HORA
        RemoverEmpilhadeira();

        if (escurecerTela)
            yield return FadeTela.Instancia.Escurecer(duracaoPretoInstantaneo);

        jogador.Teleportar(destino);

        if (escurecerTela)
        {
            yield return new WaitForSeconds(tempoNaEscuridao);
            yield return FadeTela.Instancia.Clarear(duracaoClarear);
        }
    }

    // Termina assim que a empilhadeira chega à distância do gatilho
    private IEnumerator RotinaEmpilhadeira(MovimentoTopDown jogador, Vector2 eixoDaLinha)
    {
        if (prefabEmpilhadeira == null || prefabEmpilhadeira.GetComponent<Empilhadeira>() == null)
        {
            Debug.LogWarning("LinhaPerigosa: o prefab da empilhadeira não existe ou não tem o script Empilhadeira.");
            yield break;
        }

        // A empilhadeira vem pelo eixo da própria linha
        float sinal = empilhadeiraVemDoOutroLado ? 1f : -1f;
        Vector3 origem = jogador.transform.position + (Vector3)(eixoDaLinha * sinal * distanciaSurgimento);

        GameObject go = Instantiate(prefabEmpilhadeira, origem, Quaternion.identity);
        empilhadeiraDaCena = go.GetComponent<Empilhadeira>();
        Empilhadeira empilhadeira = empilhadeiraDaCena;

        bool chegouPerto = false;
        empilhadeira.AtacarJogador(jogador.transform, velocidadeEmpilhadeira,
                                   distanciaParaEscurecer, () => chegouPerto = true);

        yield return new WaitUntil(() => chegouPerto || empilhadeira == null);
    }

    private void RemoverEmpilhadeira()
    {
        if (empilhadeiraDaCena != null)
            Destroy(empilhadeiraDaCena.gameObject);

        empilhadeiraDaCena = null;
    }
}