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

    public void RegistrarPlacaDestruida()
    {
        PlacasDestruidas++;
        Debug.Log("Placas destruídas: " + PlacasDestruidas + "/" + MetaPlacas);
    }
}