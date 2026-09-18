using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia os 4 cabos do minigame: sorteia, ao iniciar, quais deles
/// começam desconectados, e detecta a vitória quando todos são conectados.
/// </summary>
public class CaboManager : MonoBehaviour
{
    public static CaboManager Instancia { get; private set; }

    [Header("Os 4 cabos da prensa")]
    public CaboPrensa[] Cabos;

    [Header("Dificuldade")]
    [Tooltip("Quantos dos 4 cabos começam desconectados")]
    [Range(0, 4)]
    public int QuantidadeDesconectadaInicial = 2;

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        SortearEstadoInicial();
    }

    void SortearEstadoInicial()
    {
        List<int> Indices = new List<int>();
        for (int i = 0; i < Cabos.Length; i++)
            Indices.Add(i);

        // embaralha (Fisher-Yates)
        for (int i = 0; i < Indices.Count; i++)
        {
            int j = Random.Range(i, Indices.Count);
            (Indices[i], Indices[j]) = (Indices[j], Indices[i]);
        }

        int quantidade = Mathf.Min(QuantidadeDesconectadaInicial, Cabos.Length);
        HashSet<int> Desconectados = new HashSet<int>(Indices.GetRange(0, quantidade));

        for (int i = 0; i < Cabos.Length; i++)
        {
            bool conectado = !Desconectados.Contains(i);
            Cabos[i].DefinirEstadoInicial(conectado);
        }
    }

    /// <summary>Chamado por cada CaboPrensa toda vez que ele conecta.</summary>
    public void NotificarConexao()
    {
        foreach (var cabo in Cabos)
        {
            if (!cabo.EstaConectado)
                return; // ainda falta algum
        }

        Debug.Log("Todos os cabos conectados! Prensa liberada.");
        // TODO: dispare aqui seu evento de vitória (UnityEvent, animação da prensa, etc.)
    }
}