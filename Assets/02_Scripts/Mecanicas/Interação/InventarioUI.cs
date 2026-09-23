using UnityEngine;
using UnityEngine.InputSystem;

public class InventarioUI : MonoBehaviour
{
    public static bool inventarioAberto = false; // O Guarda de Trânsito Global

    [Header("UI Elementos")]
    public GameObject painelMochila; // O Canvas do inventario

    private void Start()
    {
        painelMochila.SetActive(false);
        inventarioAberto = false;
    }

    public void AoApertarI(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            inventarioAberto = !inventarioAberto;
            painelMochila.SetActive(inventarioAberto);

            // Aqui você pode adicionar um Time.timeScale = 0f se quiser que o jogo pause ao abrir a bolsa
        }
    }

    // --- FUNÇÕES REPASSADAS DE DENTRO DA BOLSA ---
    public void ReceberInputE()
    {
        Debug.Log("UI: Apertou E dentro da mochila (Equipar/Mover item selecionado)");
    }

    public void ReceberInputF()
    {
        Debug.Log("UI: Apertou F dentro da mochila (Usar item selecionado de dentro da bolsa)");
    }

    public void ReceberInputY()
    {
        Debug.Log("UI: Apertou Y dentro da mochila (Inspecionar item selecionado)");
    }
}