using UnityEngine;

public class ContadorCarros : MonoBehaviour
{
    [Header("Meta")]
    public int minimoCarros = 5;        // quantos carros precisam ser analisados
    public int carrosAnalisados = 0;    // contagem atual

    [Header("Objetos")]
    public BotaoEncerrarMinigame botaoSair;   // fica sempre visível, só muda de cor

    void OnEnable()
    {
        GerenciadorTurnos.OnTurnoFinalizado += ContarCarro;
    }

    void OnDisable()
    {
        GerenciadorTurnos.OnTurnoFinalizado -= ContarCarro;
    }

    void Start()
    {
        // Caso a meta já comece batida (por exemplo, minimo = 0)
        if (botaoSair != null && carrosAnalisados >= minimoCarros)
        {
            botaoSair.Liberar();
        }
    }

    private void ContarCarro(Carro carro)
    {
        carrosAnalisados++;

        Debug.Log($"Carros analisados: {carrosAnalisados}/{minimoCarros}");

        if (carrosAnalisados >= minimoCarros && botaoSair != null && !botaoSair.EstaLiberado)
        {
            botaoSair.Liberar();
            Debug.Log("Meta atingida! Botão de sair liberado.");
        }
    }
}