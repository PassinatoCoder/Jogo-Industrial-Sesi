using UnityEngine;

/// <summary>
/// Zona verde semitransparente que demarca a área correta de posicionamento da placa.
/// Use um quadrado com SpriteRenderer verde (alpha reduzido, ex: A ~ 80-120) e um
/// Collider2D marcado como "Is Trigger" no mesmo tamanho do quadrado.
/// A zona só considera a posição "correta" quando o centro da placa está a uma
/// distância mínima do centro desta zona (ToleranciaCentralizacao), não basta
/// só estar tocando/sobrepondo.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ZonaPosicionamento : MonoBehaviour
{
    [Header("Tolerância")]
    [Tooltip("Distância máxima entre o centro da placa e o centro desta zona pra considerar 'centralizada'.")]
    public float ToleranciaCentralizacao = 0.15f;

    private Transform PlacaDentro;

    /// <summary>Verdadeiro quando há uma placa dentro da zona E ela está centralizada dentro da tolerância.</summary>
    public bool PlacaCentralizada { get; private set; }

    void Reset()
    {
        // lembrete: o Collider2D desta zona precisa estar marcado como "Is Trigger"
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D Outro)
    {
        if (Outro.GetComponent<PlacaMetal>() != null)
            PlacaDentro = Outro.transform;
    }

    void OnTriggerExit2D(Collider2D Outro)
    {
        if (Outro.transform == PlacaDentro)
        {
            PlacaDentro = null;
            PlacaCentralizada = false;
        }
    }

    void Update()
    {
        if (PlacaDentro == null)
        {
            PlacaCentralizada = false;
            return;
        }

        float Distancia = Vector2.Distance(PlacaDentro.position, transform.position);
        PlacaCentralizada = Distancia <= ToleranciaCentralizacao;
    }
}