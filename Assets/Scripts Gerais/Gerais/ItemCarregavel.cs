using UnityEngine;

// Coloque no prefab do item. Cuida de como ele fica na mão e de quando some no inventário.
public class ItemCarregavel : MonoBehaviour
{
    [SerializeField] private string nome = "Item";

    private Collider2D[] colisores;
    private Rigidbody2D corpo;
    private SpriteRenderer[] visuais;

    public string Nome { get { return string.IsNullOrEmpty(nome) ? gameObject.name : nome; } }

    void Awake()
    {
        colisores = GetComponentsInChildren<Collider2D>(true);
        corpo = GetComponent<Rigidbody2D>();
        visuais = GetComponentsInChildren<SpriteRenderer>(true);
    }

    // Aparece anexado ao dono (a protagonista), na posição indicada
    public void SegurarEm(Transform dono, Vector3 posicaoLocal, SpriteRenderer referencia)
    {
        gameObject.SetActive(true);
        transform.SetParent(dono, false);
        transform.localPosition = posicaoLocal;
        transform.localRotation = Quaternion.identity;

        AtivarFisica(false);

        // Desenha por cima da protagonista
        if (referencia != null && visuais != null)
        {
            foreach (SpriteRenderer v in visuais)
            {
                v.sortingLayerID = referencia.sortingLayerID;
                v.sortingOrder = referencia.sortingOrder + 1;
            }
        }
    }

    // Some do mapa e fica escondido dentro do "pai" (a empilhadeira)
    public void GuardarEm(Transform pai)
    {
        AtivarFisica(false);
        transform.SetParent(pai, false);
        transform.localPosition = Vector3.zero;
        gameObject.SetActive(false);
    }

    // Sem colisor nem física: assim o item na mão não aciona linhas, faixas nem empurra nada
    private void AtivarFisica(bool ativa)
    {
        if (colisores != null)
        {
            foreach (Collider2D c in colisores)
            {
                if (c != null) c.enabled = ativa;
            }
        }

        if (corpo != null) corpo.simulated = ativa;
    }
}