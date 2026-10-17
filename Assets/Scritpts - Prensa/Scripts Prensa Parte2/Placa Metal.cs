using UnityEngine;

/// <summary>
/// Marca este objeto como uma placa de metal agarrável pelo braço robótico.
/// Guarda o estado (prensada / avaliada) e o estado inicial, pra poder ser recriada.
/// Requer um Collider2D marcado como "Is Trigger".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlacaMetal : MonoBehaviour
{
    public bool Prensada { get; private set; }
    public bool Avaliada { get; set; }

    // estado inicial, guardado quando a placa nasce
    public Vector3 PosicaoInicial { get; private set; }
    public Quaternion RotacaoInicial { get; private set; }
    public Color CorInicial { get; private set; }
    public Transform PaiInicial { get; private set; }

    void Awake()
    {
        PosicaoInicial = transform.position;
        RotacaoInicial = transform.rotation;
        PaiInicial = transform.parent;

        SpriteRenderer Sr = GetComponent<SpriteRenderer>();
        CorInicial = Sr != null ? Sr.color : Color.white;
    }

    public void MarcarComoPrensada()
    {
        Prensada = true;
    }

    /// <summary>
    /// Usado pela cópia: assume o estado inicial da placa original
    /// (posição, rotação, cor) e volta a ser uma placa nova, sem prensar.
    /// </summary>
    public void RestaurarComoNova(PlacaMetal Original)
    {
        PosicaoInicial = Original.PosicaoInicial;
        RotacaoInicial = Original.RotacaoInicial;
        CorInicial = Original.CorInicial;
        PaiInicial = Original.PaiInicial;

        Prensada = false;
        Avaliada = false;

        transform.position = PosicaoInicial;
        transform.rotation = RotacaoInicial;

        SpriteRenderer Sr = GetComponent<SpriteRenderer>();
        if (Sr != null)
            Sr.color = CorInicial;
    }

    void Reset()
    {
        // lembrete: o Collider2D deste objeto precisa estar marcado como "Is Trigger",
        // senão o braço vai colidir fisicamente com a placa em vez de agarrá-la
        GetComponent<Collider2D>().isTrigger = true;
    }
}