using UnityEngine;

// Coloque num objeto da cena (com sprite) que represente a pilha. Um Collider2D é opcional:
// se existir, a distância é medida até a borda dele.
public class PilhaDeItens : MonoBehaviour
{
    [SerializeField] private ItemCarregavel prefabItem;
    [SerializeField] private int quantidade = 5;
    [SerializeField] private bool infinita = false;
    [SerializeField] private bool esconderQuandoVazia = false;

    [Header("Pegar")]
    [SerializeField] private KeyCode teclaPegar = KeyCode.F;
    [SerializeField] private float distanciaParaPegar = 1.5f;

    private MovimentoTopDown jogador;
    private CarregadorDeItens carregador;
    private Collider2D colisor;

    public int Quantidade { get { return quantidade; } }

    void Awake()
    {
        colisor = GetComponent<Collider2D>();
    }

    void Start()
    {
        jogador = FindFirstObjectByType<MovimentoTopDown>();
        if (jogador != null) carregador = jogador.GetComponent<CarregadorDeItens>();

        if (carregador == null)
            Debug.LogWarning("PilhaDeItens: a protagonista precisa ter o script CarregadorDeItens.");

        if (prefabItem == null)
            Debug.LogWarning("PilhaDeItens: falta o prefab do item.");
    }

    void Update()
    {
        if (jogador == null || carregador == null || prefabItem == null) return;
        if (!Input.GetKeyDown(teclaPegar)) return;

        if (InventarioUI.EstaAberto) return;
        if (carregador.Carregando) return;
        if (!infinita && quantidade <= 0) return;
        if (!JogadorPodeAgir() || !JogadorPerto()) return;

        ItemCarregavel novo = Instantiate(prefabItem, transform.position, Quaternion.identity);
        carregador.Pegar(novo);

        if (!infinita) quantidade--;

        if (!infinita && quantidade <= 0 && esconderQuandoVazia)
            gameObject.SetActive(false);
    }

    private bool JogadorPodeAgir()
    {
        if (!jogador.gameObject.activeInHierarchy) return false;
        return !jogador.EmVeiculo && !jogador.EmPerigo && jogador.PodeMover;
    }

    private bool JogadorPerto()
    {
        Vector2 p = jogador.transform.position;

        if (colisor != null)
            return Vector2.Distance(colisor.ClosestPoint(p), p) <= distanciaParaPegar;

        return Vector2.Distance(transform.position, p) <= distanciaParaPegar;
    }
}