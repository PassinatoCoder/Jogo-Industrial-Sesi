using UnityEngine;
using UnityEngine.Events;

public class GatilhoDialogo : MonoBehaviour
{
    [Header("Configuração do Evento")]
    [SerializeField] private bool iniciarAutomaticamenteAoLigar = false;

    [Header("As Falas")]
    [SerializeField] private LinhaDialogo[] falas;

    [Header("Automação: O que acontece quando o diálogo acaba?")]
    [SerializeField] private UnityEvent aoTerminarDialogo;

    private void Start()
    {
        if (iniciarAutomaticamenteAoLigar)
        {
            DispararDialogo();
        }
    }

    // Você pode chamar isso de fora (por exemplo, quando o jogador apertar 'E' num NPC)
    public void DispararDialogo()
    {
        ControladorDialogoRPG.Instancia.IniciarSequencia(falas, () => aoTerminarDialogo?.Invoke());
    }
}