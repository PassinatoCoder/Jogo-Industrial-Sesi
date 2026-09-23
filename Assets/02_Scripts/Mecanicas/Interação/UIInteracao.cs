using UnityEngine;
using TMPro;

public class UIInteracao : MonoBehaviour
{
    [Header("UI State Containers (Paineis Pai)")]
    [Tooltip("Container exibido ao aproximar-se de um item no chão.")]
    [SerializeField] private GameObject groundPromptContainer;

    [Tooltip("Container exibido quando o jogador possui um item na mão/equipado.")]
    [SerializeField] private GameObject heldItemOptionsContainer;

    [Header("Text Components")]
    [SerializeField] private TextMeshProUGUI txtGroundAction;
    [SerializeField] private TextMeshProUGUI txtHeldItemActions;
    [SerializeField] private TextMeshProUGUI txtBagCountdown;

    private void Start()
    {
        EsconderTudo();
    }

    public void MostrarAvisoChao(string itemName)
    {
        EsconderTudo();
        if (groundPromptContainer != null) groundPromptContainer.SetActive(true);
        if (txtGroundAction != null) txtGroundAction.text = $"[E] Pegar {itemName}";
    }

    public void MostrarOpcoesMao(ItemData item)
    {
        EsconderTudo();
        if (heldItemOptionsContainer != null) heldItemOptionsContainer.SetActive(true);
        if (txtBagCountdown != null) txtBagCountdown.text = string.Empty;

        if (txtHeldItemActions != null && item != null)
        {
            string displayName = item.jaAprendeu ? item.nomeConhecido : item.nomeDesconhecido;
            txtHeldItemActions.text = $"Equipado: {displayName}\n" +
                                      $"[E] Devolver\n" +
                                      $"[F] Usar / Equipar\n" +
                                      $"[Y] Inspecionar\n" +
                                      $"[G] Cinto (Toque) | Bolsa (Segurar)";
        }
    }

    public void AtualizarContagem(float remainingTime)
    {
        if (txtBagCountdown == null) return;

        if (remainingTime > 0)
        {
            txtBagCountdown.text = $"Guardando na bolsa em {Mathf.CeilToInt(remainingTime)}s...";
        }
        else
        {
            txtBagCountdown.text = "Item guardado!";
        }
    }

    public void CancelarContagem()
    {
        if (txtBagCountdown != null) txtBagCountdown.text = string.Empty;
    }

    public void EsconderTudo()
    {
        if (groundPromptContainer != null) groundPromptContainer.SetActive(false);
        if (heldItemOptionsContainer != null) heldItemOptionsContainer.SetActive(false);
    }
}