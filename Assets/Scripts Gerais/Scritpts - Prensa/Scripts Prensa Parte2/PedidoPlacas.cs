using UnityEngine;


/// <summary>
/// Guarda quantas placas o jogador precisa fazer (definido pelo papel sorteado).
/// 0 = ainda não sorteado. É estático, então continua valendo depois da troca de cena.
/// </summary>
public static class PedidoPlacas
{
    public static int MetaPlacas = 0;
}