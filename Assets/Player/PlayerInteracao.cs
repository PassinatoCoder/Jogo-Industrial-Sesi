using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteracao : MonoBehaviour
{
    private ItemFisico itemProximo; // O item que está colidindo com a Raquel no chão
    private InventarioManager inventario;

    [Header("Lógica do Botão G")]
    [SerializeField] private float tempoParaConsiderarSegurar = 0.4f;
    private float tempoApertandoG = 0f;
    private bool estaSegurandoG = false;

    private void Awake()
    {
        inventario = GetComponent<InventarioManager>();
    }

    // Detecta quando encosta num item no chão
    private void OnTriggerEnter2D(Collider2D collision)
    {
        ItemFisico item = collision.GetComponent<ItemFisico>();
        if (item != null)
        {
            itemProximo = item;
            Debug.Log($"Item próximo detectado: {item.dadosDoItem.nomeDesconhecido}. Pressione Y para inspecionar ou G para pegar.");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        ItemFisico item = collision.GetComponent<ItemFisico>();
        if (item == itemProximo)
        {
            itemProximo = null;
        }
    }

    public void AoApertarInspecionar(InputAction.CallbackContext context)
    {
        if (context.performed && itemProximo != null)
        {
            itemProximo.Inspecionar();
        }
    }

    // A MÁGICA DO BOTÃO G (Toque rápido = Cinto | Segurar = Bolsa)
    public void AoApertarGuardar(InputAction.CallbackContext context)
    {
        if (itemProximo == null) return;

        if (context.started)
        {
            estaSegurandoG = true;
            tempoApertandoG = 0f;
        }
        else if (context.canceled)
        {
            estaSegurandoG = false;

            if (tempoApertandoG < tempoParaConsiderarSegurar)
            {
                // Toque rápido -> Tenta ir pro Cinto
                ItemData dados = itemProximo.PegarItem();
                bool guardou = inventario.AdicionarAoCinto(dados);
                if (!guardou) { /* Devolve pro chão se falhar */ }
                itemProximo = null;
            }
        }
    }

    private void Update()
    {
        if (estaSegurandoG && itemProximo != null)
        {
            tempoApertandoG += Time.deltaTime;

            if (tempoApertandoG >= tempoParaConsiderarSegurar)
            {
                estaSegurandoG = false;
                // Segurou -> Tenta ir pra Bolsa
                ItemData dados = itemProximo.PegarItem();
                inventario.AdicionarNaBolsa(dados);
                itemProximo = null;
            }
        }
    }
}