using UnityEngine;

/// <summary>
/// Conta quantas placas já foram aprovadas e destruídas.
/// Ao chegar na meta, libera a saída do minigame.
/// </summary>
public class ContadorPlacas : MonoBehaviour
{
    [Header("Meta")]
    public int MetaPlacas = 5;

    [Header("Estado atual")]
    public int PlacasDestruidas = 0;

    public bool MetaAtingida => PlacasDestruidas >= MetaPlacas;

    void Awake()
    {
        // Se veio do sorteio do papel, usa a meta dele.
        // Se abriu a cena direto (teste), mantém o valor do Inspector.
        if (PedidoPlacas.MetaPlacas > 0)
            MetaPlacas = PedidoPlacas.MetaPlacas;
    }

    public void RegistrarPlacaDestruida()
    {
        PlacasDestruidas++;
        Debug.Log("Placas destruídas: " + PlacasDestruidas + "/" + MetaPlacas);
    }
}