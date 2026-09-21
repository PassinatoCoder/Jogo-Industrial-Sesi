using UnityEngine;
using UnityEngine.Events;

public class SistemaSobrevivencia : MonoBehaviour
{
    [Header("Atributos do Trabalhador")]
    public float integridadeMax = 100f;
    public float staminaMax = 100f;
    public float fomeMax = 100f;

    [Header("Valores Atuais")]
    public float integridadeAtual;
    public float staminaAtual;
    public float fomeAtual;

    [Header("Taxas de Desgaste")]
    [SerializeField, Tooltip("Quanto de fome perde por segundo normal")]
    private float taxaFomePorSegundo = 0.5f;
    [SerializeField, Tooltip("Quanto de stamina perde por segundo correndo")]
    private float taxaGastoStamina = 15f;
    [SerializeField, Tooltip("Quanto de stamina recupera por segundo parado/andando")]
    private float taxaRecuperacaoStamina = 10f;

    [Header("Eventos de UI (Para o Marcos linkar depois)")]
    public UnityEvent<float> AoMudarIntegridade;
    public UnityEvent<float> AoMudarStamina;
    public UnityEvent<float> AoMudarFome;
    public UnityEvent AoZerarIntegridade; // Game Over / Demissão

    // Estado público para o PlayerMovimento saber se ela pode correr
    public bool TemStamina => staminaAtual > 5f;
    public bool EstaComFomeCritica => fomeAtual <= 20f;

    private void Start()
    {
        // Começa o turno 100% (ou quase)
        integridadeAtual = integridadeMax;
        staminaAtual = staminaMax;
        fomeAtual = fomeMax;

        AtualizarUI();
    }

    private void Update()
    {
        DrenarFomePassiva();
        RecuperarStaminaPassiva();
    }

    private void DrenarFomePassiva()
    {
        if (fomeAtual > 0)
        {
            fomeAtual -= taxaFomePorSegundo * Time.deltaTime;
            AoMudarFome?.Invoke(fomeAtual / fomeMax);
        }
    }

    private void RecuperarStaminaPassiva()
    {
        // Se ela estiver com fome crítica, a stamina recupera pela metade (Game Feel do perrengue)
        float multiplicadorFome = EstaComFomeCritica ? 0.5f : 1f;

        if (staminaAtual < staminaMax)
        {
            staminaAtual += (taxaRecuperacaoStamina * multiplicadorFome) * Time.deltaTime;
            staminaAtual = Mathf.Clamp(staminaAtual, 0, staminaMax);
            AoMudarStamina?.Invoke(staminaAtual / staminaMax);
        }
    }

    // Função que o PlayerMovimento vai chamar quando você segurar o SHIFT
    public void GastarStaminaCorrendo()
    {
        if (staminaAtual > 0)
        {
            staminaAtual -= taxaGastoStamina * Time.deltaTime;
            AoMudarStamina?.Invoke(staminaAtual / staminaMax);
        }
    }

    // Função para interagir com a máquina de café/marmita
    public void Alimentar(float quantidade)
    {
        fomeAtual += quantidade;
        fomeAtual = Mathf.Clamp(fomeAtual, 0, fomeMax);
        AoMudarFome?.Invoke(fomeAtual / fomeMax);
    }

    // Sistema de Punição e Acertos (EPIs, Erros na fábrica)
    public void ModificarIntegridade(float valor)
    {
        integridadeAtual += valor;
        integridadeAtual = Mathf.Clamp(integridadeAtual, 0, integridadeMax);
        AoMudarIntegridade?.Invoke(integridadeAtual / integridadeMax);

        if (integridadeAtual <= 0)
        {
            AoZerarIntegridade?.Invoke(); // Chama a tela de "Você foi demitida"
        }
    }

    private void AtualizarUI()
    {
        AoMudarIntegridade?.Invoke(integridadeAtual / integridadeMax);
        AoMudarStamina?.Invoke(staminaAtual / staminaMax);
        AoMudarFome?.Invoke(fomeAtual / fomeMax);
    }
}