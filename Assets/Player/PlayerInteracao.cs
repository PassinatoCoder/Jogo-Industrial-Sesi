using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteracao : MonoBehaviour
{
    private ItemFisico itemProximoNoChao;
    private InventarioManager inventario;

    [Header("Interface (RDR2 Style)")]
    public UIInteracao uiInteracao;

    [Header("Lógica de Segurar G")]
    public float tempoParaBolsa = 3f;
    private float tempoPressionado = 0f;
    private bool estaPressionandoG = false;

    private void Awake()
    {
        inventario = GetComponent<InventarioManager>();
    }

    // ==========================================
    // DETECÇÃO NO MUNDO 2D
    // ==========================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        ItemFisico item = collision.GetComponent<ItemFisico>();
        if (item != null && inventario != null && inventario.itemNaMao == null)
        {
            itemProximoNoChao = item;
            if (uiInteracao != null && item.dadosDoItem != null)
            {
                string nomeHUD = item.dadosDoItem.jaAprendeu ? item.dadosDoItem.nomeConhecido : item.dadosDoItem.nomeDesconhecido;
                uiInteracao.MostrarAvisoChao(nomeHUD);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        ItemFisico item = collision.GetComponent<ItemFisico>();
        if (item == itemProximoNoChao)
        {
            itemProximoNoChao = null;
            if (inventario != null && inventario.itemNaMao == null && uiInteracao != null)
            {
                uiInteracao.EsconderTudo();
            }
        }
    }

    // ==========================================
    // CONTROLES DE AÇÃO
    // ==========================================

    public void AoApertarPegar_E(InputAction.CallbackContext context)
    {
        if (!context.performed || inventario == null) return;

        // SE TEM NA MÃO -> Devolve ao chão
        if (inventario.itemNaMao != null)
        {
            DevolverItem();
        }
        // SE NÃO TEM NA MÃO E ESTÁ PERTO DE ALGO -> Pega
        else if (itemProximoNoChao != null)
        {
            inventario.itemNaMao = itemProximoNoChao.PegarItem();
            itemProximoNoChao = null;
            inventario.RecalcularPeso();
            if (uiInteracao != null) uiInteracao.MostrarOpcoesMao(inventario.itemNaMao);
        }
    }

    public void AoApertarUsar_F(InputAction.CallbackContext context)
    {
        if (!context.performed || inventario == null || inventario.itemNaMao == null) return;

        Debug.Log($"Usando/Equipando a ferramenta: {inventario.itemNaMao.nomeConhecido}");
    }

    public void AoApertarInspecionar_Y(InputAction.CallbackContext context)
    {
        if (!context.performed || inventario == null || inventario.itemNaMao == null) return;

        ItemData item = inventario.itemNaMao;
        if (item.jaAprendeu)
        {
            Debug.Log($"Raquel: Isto é um {item.nomeConhecido}. {item.descricaoConhecida}");
        }
        else
        {
            Debug.Log($"Raquel: O que é isto? {item.descricaoDesconhecida}");
            item.jaAprendeu = true;

            if (uiInteracao != null) uiInteracao.MostrarOpcoesMao(item);
        }
    }

    // ==========================================
    // LÓGICA DA TECLA G (Toque vs Segurar 3s)
    // ==========================================

    public void AoApertarGuardar_G(InputAction.CallbackContext context)
    {
        if (inventario == null || inventario.itemNaMao == null) return;

        if (context.started)
        {
            estaPressionandoG = true;
            tempoPressionado = tempoParaBolsa;
        }
        else if (context.canceled)
        {
            estaPressionandoG = false;
            if (uiInteracao != null) uiInteracao.CancelarContagem();

            if (tempoPressionado > 0f)
            {
                if (inventario.AdicionarAoCinto(inventario.itemNaMao))
                {
                    FinalizarAcaoGuardar();
                }
            }
        }
    }

    private void Update()
    {
        if (estaPressionandoG && inventario != null && inventario.itemNaMao != null)
        {
            tempoPressionado -= Time.deltaTime;
            if (uiInteracao != null) uiInteracao.AtualizarContagem(tempoPressionado);

            if (tempoPressionado <= 0f)
            {
                estaPressionandoG = false;
                if (inventario.AdicionarNaBolsa(inventario.itemNaMao))
                {
                    FinalizarAcaoGuardar();
                }
                else
                {
                    if (uiInteracao != null) uiInteracao.CancelarContagem();
                }
            }
        }
    }

    // ==========================================
    // FUNÇÕES AUXILIARES
    // ==========================================
    private void FinalizarAcaoGuardar()
    {
        inventario.itemNaMao = null;
        if (uiInteracao != null) uiInteracao.EsconderTudo();

        if (itemProximoNoChao != null && uiInteracao != null && itemProximoNoChao.dadosDoItem != null)
        {
            string nomeHUD = itemProximoNoChao.dadosDoItem.jaAprendeu ? itemProximoNoChao.dadosDoItem.nomeConhecido : itemProximoNoChao.dadosDoItem.nomeDesconhecido;
            uiInteracao.MostrarAvisoChao(nomeHUD);
        }
    }

    private void DevolverItem()
    {
        Debug.Log("Largar o item no chão.");

        inventario.itemNaMao = null;
        inventario.RecalcularPeso();
        if (uiInteracao != null) uiInteracao.EsconderTudo();
    }
}