using System.Collections.Generic;
using UnityEngine;

public enum TamanhoBolsa { Pequena = 6, Media = 10, Grande = 16 }

public class InventarioManager : MonoBehaviour
{
    [Header("Configuração do Cinto (Hotbar)")]
    public int capacidadeCinto = 3; // Começa com 3, pode upar pra 5
    public List<ItemData> itensNoCinto = new List<ItemData>();

    [Header("Configuração da Bolsa (Inventário)")]
    public TamanhoBolsa bolsaAtual = TamanhoBolsa.Pequena;
    public List<ItemData> itensNaBolsa = new List<ItemData>();

    [Header("Limites e Pesos")]
    public float pesoMaximoSuportado = 20f; // Passou disso, a bolsa rasga ou a Raquel trava
    private float pesoTotalAtual = 0f;

    private PlayerMovimento playerMovimento;

    private void Awake()
    {
        playerMovimento = GetComponent<PlayerMovimento>();
    }

    // Tenta guardar no Cinto (Aperto rápido de G)
    public bool AdicionarAoCinto(ItemData item)
    {
        if (!item.podeIrNoCinto)
        {
            Debug.Log("Raquel: Isso é pesado demais para o cinto, tem que ir na bolsa!");
            return false;
        }

        if (itensNoCinto.Count < capacidadeCinto)
        {
            itensNoCinto.Add(item);
            RecalcularPeso();
            return true;
        }

        Debug.Log("Raquel: O cinto de ferramentas está lotado!");
        return false;
    }

    // Tenta guardar na Bolsa (Segurar G)
    public bool AdicionarNaBolsa(ItemData item)
    {
        int limiteBolsa = (int)bolsaAtual;

        if (itensNaBolsa.Count < limiteBolsa)
        {
            // Verifica se o peso total vai estourar
            if (pesoTotalAtual + item.peso > pesoMaximoSuportado)
            {
                Debug.Log("Raquel: Minhas costas não vão aguentar carregar tanto peso!");
                return false;
            }

            itensNaBolsa.Add(item);
            RecalcularPeso();
            return true;
        }

        Debug.Log("Raquel: A bolsa está completamente cheia!");
        return false;
    }

    private void RecalcularPeso()
    {
        pesoTotalAtual = 0f;

        foreach (var item in itensNoCinto) pesoTotalAtual += item.peso;
        foreach (var item in itensNaBolsa) pesoTotalAtual += item.peso;

        // Ajusta a velocidade da Raquel baseada no peso proporcional
        if (playerMovimento != null)
        {
            if (pesoTotalAtual <= 5f) playerMovimento.multiplicadorPesoInventario = 1f; // Leve
            else if (pesoTotalAtual <= 12f) playerMovimento.multiplicadorPesoInventario = 0.8f; // Moderado
            else if (pesoTotalAtual <= 19f) playerMovimento.multiplicadorPesoInventario = 0.5f; // Pesado
            else playerMovimento.multiplicadorPesoInventario = 0.3f; // Sobrecarga total (Tartaruga)
        }

        Debug.Log($"Peso total na carga: {pesoTotalAtual}kg");
    }
}