using System.Collections;
using UnityEngine;

public class MovimentoTopDown : MonoBehaviour
{
    [SerializeField] private float velocidade = 5f;

    // Outros scripts desligam isso quando a protagonista não pode andar
    public bool PodeMover { get; set; } = true;

    // Usado pela faixa de pedestre (A e D viram "olhar para os lados")
    public bool TravarHorizontal { get; set; }

    // Indica que uma sequência de perigo (linha amarela/preta) está em andamento
    public bool EmPerigo { get; set; }

    // Indica que a protagonista está dentro de uma empilhadeira
    public bool EmVeiculo { get; set; }

    private Rigidbody2D rb;
    private Vector2 direcao;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!PodeMover)
        {
            direcao = Vector2.zero;
            return;
        }

        float x = TravarHorizontal ? 0f : Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        direcao = new Vector2(x, y).normalized;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = direcao * velocidade;
    }

    public void Parar()
    {
        direcao = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    public void Teleportar(Vector2 destino)
    {
        rb.position = destino;
        transform.position = new Vector3(destino.x, destino.y, transform.position.z);
        rb.linearVelocity = Vector2.zero;
    }

    // Desliza rapidamente até o destino (usado para travar no meio da linha)
    public IEnumerator DeslizarAte(Vector2 destino, float duracao)
    {
        Parar();

        Vector2 inicio = rb.position;
        float t = 0f;

        while (t < duracao)
        {
            t += Time.deltaTime;
            Teleportar(Vector2.Lerp(inicio, destino, t / duracao));
            yield return null;
        }

        Teleportar(destino);
    }
}