using UnityEngine;

public class GerenciadorTutorial : MonoBehaviour
{
    public static GerenciadorTutorial Instancia { get; private set; }

    [Header("Sistemas Acoplados")]
    [SerializeField] private PlayerMovimento player;
    // Futuramente, adicionaremos a referência do sistema de Interação aqui

    [Header("Monitoramento de Estado (Apenas Leitura)")]
    [SerializeField] private bool movimentoDesbloqueado;
    [SerializeField] private bool puloDesbloqueado;
    [SerializeField] private bool agacharDesbloqueado;
    [SerializeField] private bool interacaoDesbloqueada;

    private void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);

        TravarMecanicasIniciais();
    }

    private void TravarMecanicasIniciais()
    {
        // Garante a regra do Progressive Disclosure: Nada funciona sem ser ensinado.
        movimentoDesbloqueado = false;
        puloDesbloqueado = false;
        agacharDesbloqueado = false;
        interacaoDesbloqueada = false;

        SincronizarSistemas();
    }

    // --- MÉTODOS PÚBLICOS PARA EVENTOS (GATILHOS DO MASCOTE) ---

    public void DesbloquearMovimento()
    {
        movimentoDesbloqueado = true;
        SincronizarSistemas();
        Debug.Log("[Tutorial] Mecânica de Movimento (WASD) Liberada.");
    }

    public void DesbloquearPulo()
    {
        puloDesbloqueado = true;
        SincronizarSistemas();
        Debug.Log("[Tutorial] Mecânica de Pulo (Espaço) Liberada.");
    }

    public void DesbloquearAgachamento()
    {
        agacharDesbloqueado = true;
        SincronizarSistemas();
        Debug.Log("[Tutorial] Mecânica de Agachar (Ctrl) Liberada.");
    }

    public void DesbloquearInteracao()
    {
        interacaoDesbloqueada = true;
        SincronizarSistemas();
        Debug.Log("[Tutorial] Mecânica de Interação (E, Y, F) Liberada.");
    }

    // --- SINCRONIZAÇÃO MASTER ---

    private void SincronizarSistemas()
    {
        if (player != null)
        {
            player.movimentoLiberado = movimentoDesbloqueado;
            player.puloLiberado = puloDesbloqueado;
            player.agacharLiberado = agacharDesbloqueado;
        }

        // Exemplo futuro: if (sistemaInteracao != null) sistemaInteracao.liberado = interacaoDesbloqueada;
    }
}