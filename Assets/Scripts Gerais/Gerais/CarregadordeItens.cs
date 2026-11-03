using UnityEngine;

// Coloque na protagonista. É a "mão" dela: segura um item de cada vez, ao lado do corpo.
public class CarregadorDeItens : MonoBehaviour
{
    [Tooltip("Posição do item em relação à protagonista (x positivo = para o lado que ela está olhando).")]
    [SerializeField] private Vector2 deslocamentoMao = new Vector2(0.6f, 0f);

    [Tooltip("O item troca de lado conforme ela anda para a esquerda ou para a direita.")]
    [SerializeField] private bool ladoSegueMovimento = true;

    private ItemCarregavel item;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float lado = 1f;

    public ItemCarregavel Item { get { return item; } }
    public bool Carregando { get { return item != null; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (item == null) return;

        if (ladoSegueMovimento && rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            lado = Mathf.Sign(rb.linearVelocity.x);

        item.transform.localPosition = PosicaoNaMao();
    }

    private Vector3 PosicaoNaMao()
    {
        return new Vector3(deslocamentoMao.x * lado, deslocamentoMao.y, 0f);
    }

    // Segura o item (só se a mão estiver livre)
    public bool Pegar(ItemCarregavel novo)
    {
        if (novo == null || item != null) return false;

        item = novo;
        item.SegurarEm(transform, PosicaoNaMao(), sr);
        return true;
    }

    // Tira o item da mão e entrega para quem chamou, que decide onde ele vai ficar
    public ItemCarregavel Largar()
    {
        ItemCarregavel saiu = item;
        item = null;
        return saiu;
    }
}