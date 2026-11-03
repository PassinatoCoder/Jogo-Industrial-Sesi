using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FichaZoom : MonoBehaviour
{
    [Header("Ampliação")]
    public float escalaZoom = 2.5f;     // quantas vezes a ficha cresce
    public float velocidade = 10f;      // velocidade da animação
    public float deslocamentoZ = -1f;   // aproxima da câmera para ficar na frente da prancheta
    public int ordemExtra = 100;        // sobe a ordem de desenho para ficar por cima de tudo

    private Vector3 posicaoInicial;
    private Vector3 escalaInicial;
    private Vector3 posicaoAlvo;
    private Vector3 escalaAlvo;
    private bool ampliada = false;

    private SpriteRenderer[] sprites;
    private int[] ordensOriginais;

    void Start()
    {
        // Guarda o lugar onde o gerenciador colocou a ficha
        posicaoInicial = transform.position;
        escalaInicial = transform.localScale;
        posicaoAlvo = posicaoInicial;
        escalaAlvo = escalaInicial;

        sprites = GetComponentsInChildren<SpriteRenderer>();
        ordensOriginais = new int[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            ordensOriginais[i] = sprites[i].sortingOrder;
        }
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, posicaoAlvo, velocidade * Time.deltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, escalaAlvo, velocidade * Time.deltaTime);
    }

    void OnMouseDown()
    {
        ampliada = !ampliada;

        if (ampliada)
        {
            // Centro da tela = posição da câmera
            Vector3 centro = Camera.main.transform.position;
            posicaoAlvo = new Vector3(centro.x, centro.y, posicaoInicial.z + deslocamentoZ);
            escalaAlvo = escalaInicial * escalaZoom;
        }
        else
        {
            posicaoAlvo = posicaoInicial;
            escalaAlvo = escalaInicial;
        }

        AtualizarOrdem();
    }

    private void AtualizarOrdem()
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i].sortingOrder = ampliada ? ordensOriginais[i] + ordemExtra : ordensOriginais[i];
        }
    }
}