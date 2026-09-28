using System.Collections;
using UnityEngine;

/// <summary>
/// Zona de verificação. Faz duas confirmações na placa:
/// 1) está centralizada na zona; 2) já foi prensada.
/// Se as duas forem verdadeiras, a placa muda de cor e, após o tempo padrão,
/// o GameObject é destruído.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ZonaAvaliacao : MonoBehaviour
{
    [Header("Confirmação de centro")]
    [Tooltip("Distância máxima entre o centro da placa e o centro da zona.")]
    public float ToleranciaCentralizacao = 0.3f;

    [Header("Feedback")]
    public Color CorAprovada = new Color(0.3f, 1f, 0.3f, 1f);
    public float TempoAntesDeDestruir = 0.6f;

    private Collider2D Zona;

    void Awake()
    {
        Zona = GetComponent<Collider2D>();
    }

    void Update()
    {
        // pergunta à física o que está sobre a zona (não depende de eventos de trigger)
        Bounds Area = Zona.bounds;
        Collider2D[] Encontrados = Physics2D.OverlapBoxAll(Area.center, Area.size, 0f);

        foreach (Collider2D Col in Encontrados)
        {
            PlacaMetal Placa = Col.GetComponentInParent<PlacaMetal>();
            if (Placa == null || Placa.Avaliada)
                continue;

            // confirmação 1: está no centro?
            float Distancia = Vector2.Distance(Placa.transform.position, transform.position);
            bool Centralizada = Distancia <= ToleranciaCentralizacao;

            // confirmação 2: está prensada?
            bool Prensada = Placa.Prensada;

            if (Centralizada && Prensada)
            {
                Placa.Avaliada = true;
                StartCoroutine(AprovarEDestruir(Placa));
            }
        }
    }

    IEnumerator AprovarEDestruir(PlacaMetal Placa)
    {
        SpriteRenderer Sr = Placa.GetComponent<SpriteRenderer>();
        if (Sr != null)
            Sr.color = CorAprovada;

        yield return new WaitForSeconds(TempoAntesDeDestruir);

        if (Placa == null)
            yield break;

        // cria a nova placa na posição inicial antes de destruir a atual
        GameObject Nova = Instantiate(Placa.gameObject, Placa.PosicaoInicial, Placa.RotacaoInicial, Placa.PaiInicial);
        Nova.name = Placa.name.Replace("(Clone)", "").Trim();
        Nova.GetComponent<PlacaMetal>().RestaurarComoNova(Placa);

        Destroy(Placa.gameObject);
    }
}