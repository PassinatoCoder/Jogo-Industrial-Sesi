using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Sorteia um papel (que já tem seu número de placas), mostra em tela cheia
/// até o jogador apertar Enter, e depois sorteia e carrega a cena.
/// </summary>
public class SorteioPapel : MonoBehaviour
{
    [System.Serializable]
    public class Pedido
    {
        [Tooltip("Sprite do papel mostrado em tela cheia.")]
        public Sprite Papel;
        [Tooltip("Quantidade de placas que esse papel exige.")]
        public int QuantidadePlacas = 1;
    }

    [Header("Papel (tela cheia)")]
    [Tooltip("Image de UI que ocupa a tela toda.")]
    public Image ImagemPapel;

    [Header("Pedidos (papel + quantidade)")]
    public Pedido[] Pedidos;

    [Header("Cenas")]
    [Tooltip("Nomes exatos das cenas (precisam estar no Build Settings). Uma é sorteada.")]
    public string[] Cenas;

    private bool aguardandoEnter;

    void Start()
    {
        SortearPedido();

        ImagemPapel.gameObject.SetActive(true);
        aguardandoEnter = true;
    }

    void Update()
    {
        if (!aguardandoEnter)
            return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            aguardandoEnter = false;
            ImagemPapel.gameObject.SetActive(false);
            SortearECarregarCena();
        }
    }

    void SortearPedido()
    {
        if (Pedidos == null || Pedidos.Length == 0)
        {
            Debug.LogWarning("SorteioPapel: nenhum pedido configurado.");
            return;
        }

        Pedido sorteado = Pedidos[Random.Range(0, Pedidos.Length)];

        ImagemPapel.sprite = sorteado.Papel;
        PedidoPlacas.MetaPlacas = sorteado.QuantidadePlacas;
    }

    void SortearECarregarCena()
    {
        if (Cenas == null || Cenas.Length == 0)
        {
            Debug.LogWarning("SorteioPapel: nenhuma cena configurada.");
            return;
        }

        SceneManager.LoadScene(Cenas[Random.Range(0, Cenas.Length)]);
    }
}