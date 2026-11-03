using System.Collections.Generic;
using UnityEngine;

// Coloque na empilhadeira dirigível (ela já tem Collider2D).
// Guarda os itens escondidos e abre o painel com TAB quando a protagonista está perto, a pé.
[RequireComponent(typeof(Collider2D))]
public class InventarioEmpilhadeira : MonoBehaviour
{
    [SerializeField] private KeyCode tecla = KeyCode.Tab;
    [SerializeField] private int capacidade = 6;
    [SerializeField] private float distanciaParaAbrir = 2f;
    [SerializeField] private string titulo = "Empilhadeira";

    private readonly List<ItemCarregavel> itens = new List<ItemCarregavel>();

    private MovimentoTopDown jogador;
    private CarregadorDeItens carregador;
    private Collider2D colisor;

    public KeyCode Tecla { get { return tecla; } }
    public string Titulo { get { return titulo; } }
    public int Capacidade { get { return capacidade; } }
    public bool Cheio { get { return itens.Count >= capacidade; } }
    public IReadOnlyList<ItemCarregavel> Itens { get { return itens; } }

    void Awake()
    {
        colisor = GetComponent<Collider2D>();
    }

    void Start()
    {
        jogador = FindFirstObjectByType<MovimentoTopDown>();
        if (jogador != null) carregador = jogador.GetComponent<CarregadorDeItens>();

        if (carregador == null)
            Debug.LogWarning("InventarioEmpilhadeira: a protagonista precisa ter o script CarregadorDeItens.");
    }

    void Update()
    {
        if (jogador == null || carregador == null) return;
        if (!Input.GetKeyDown(tecla)) return;

        // Se o painel já está aberto (ou acabou de abrir/fechar neste frame), quem cuida é ele
        if (InventarioUI.EstaAberto || InventarioUI.MudouNesteFrame) return;

        if (!JogadorPodeAbrir()) return;

        InventarioUI.Instancia.Abrir(this, jogador, carregador);
    }

    private bool JogadorPodeAbrir()
    {
        if (!jogador.gameObject.activeInHierarchy) return false;
        if (jogador.EmVeiculo || jogador.EmPerigo || !jogador.PodeMover) return false;

        Vector2 p = jogador.transform.position;
        return Vector2.Distance(colisor.ClosestPoint(p), p) <= distanciaParaAbrir;
    }

    // O item some do mapa e fica escondido dentro da empilhadeira
    public bool Guardar(ItemCarregavel item)
    {
        if (item == null || Cheio) return false;

        itens.Add(item);
        item.GuardarEm(transform);
        return true;
    }

    // Tira da lista e devolve o item (ainda escondido; quem receber é que o faz reaparecer)
    public ItemCarregavel Retirar(int indice)
    {
        if (indice < 0 || indice >= itens.Count) return null;

        ItemCarregavel item = itens[indice];
        itens.RemoveAt(indice);
        return item;
    }
}