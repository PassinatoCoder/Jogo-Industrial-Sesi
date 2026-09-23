using System.Collections.Generic;
using UnityEngine;

public enum TamanhoBolsa { Pequena = 6, Media = 10, Grande = 16 }

public class InventarioManager : MonoBehaviour
{
    [Header("Item na Mão (Equipado)")]
    public ItemData itemNaMao; // O que ela está segurando agora

    [Header("Cinto (Hotbar)")]
    public int capacidadeCinto = 3;
    public List<ItemData> itensNoCinto = new List<ItemData>();

    [Header("Bolsa")]
    public TamanhoBolsa bolsaAtual = TamanhoBolsa.Pequena;
    public List<ItemData> itensNaBolsa = new List<ItemData>();
    public float pesoMaximoSuportado = 20f;

    private float pesoTotalAtual = 0f;
    private PlayerMovimento playerMovimento;

    private void Awake()
    {
        playerMovimento = GetComponent<PlayerMovimento>();
    }

    public bool AdicionarAoCinto(ItemData item)
    {
        if (!item.podeIrNoCinto) return false;
        if (itensNoCinto.Count < capacidadeCinto)
        {
            itensNoCinto.Add(item);
            RecalcularPeso();
            return true;
        }
        return false;
    }

    public bool AdicionarNaBolsa(ItemData item)
    {
        if (itensNaBolsa.Count < (int)bolsaAtual && (pesoTotalAtual + item.peso <= pesoMaximoSuportado))
        {
            itensNaBolsa.Add(item);
            RecalcularPeso();
            return true;
        }
        return false;
    }

    public void RecalcularPeso()
    {
        pesoTotalAtual = 0f;
        if (itemNaMao != null) pesoTotalAtual += itemNaMao.peso;
        foreach (var item in itensNoCinto) pesoTotalAtual += item.peso;
        foreach (var item in itensNaBolsa) pesoTotalAtual += item.peso;

        if (playerMovimento != null)
        {
            if (pesoTotalAtual <= 5f) playerMovimento.multiplicadorPesoInventario = 1f;
            else if (pesoTotalAtual <= 12f) playerMovimento.multiplicadorPesoInventario = 0.8f;
            else if (pesoTotalAtual <= 19f) playerMovimento.multiplicadorPesoInventario = 0.5f;
            else playerMovimento.multiplicadorPesoInventario = 0.3f;
        }
    }
}