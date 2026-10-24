using UnityEngine;
using System;

public class PrensaController : MonoBehaviour
{
    [Header("Movimento da Prensa")]
    [SerializeField] private float velocidadePressao = 3f;

    [Header("Pontos (GameObjects vazios)")]
    [SerializeField] private Transform pontoInicial;
    [SerializeField] private Transform pontoFinal;

    [Header("Feedback visual (placeholder)")]
    [SerializeField] private Color corPlacaPrensada = Color.red;

    public event Action OnPrensaRecolhida;

    private Vector3 posicaoInicial;
    private Vector3 posicaoFinal;

    private enum EstadoPrensa { Parada, Descendo, Subindo }
    private EstadoPrensa estado = EstadoPrensa.Parada;

    public bool PodeAtivar => estado == EstadoPrensa.Parada;

    private void Start()
    {
        // Se não houver ponto inicial, usa a posição atual da prensa
        posicaoInicial = pontoInicial != null ? pontoInicial.position : transform.position;

        if (pontoFinal != null)
        {
            posicaoFinal = pontoFinal.position;
        }
        else
        {
            Debug.LogWarning("PrensaController: 'Ponto Final' não foi definido. A prensa não vai descer.", this);
            posicaoFinal = posicaoInicial;
        }

        // Coloca a prensa no ponto inicial
        transform.position = posicaoInicial;
    }

    private void Update()
    {
        switch (estado)
        {
            case EstadoPrensa.Descendo:
                transform.position = Vector3.MoveTowards(transform.position, posicaoFinal, velocidadePressao * Time.deltaTime);
                if (transform.position == posicaoFinal)
                {
                    estado = EstadoPrensa.Subindo; // não bateu na placa, mas chegou no fim mesmo assim
                }
                break;

            case EstadoPrensa.Subindo:
                transform.position = Vector3.MoveTowards(transform.position, posicaoInicial, velocidadePressao * Time.deltaTime);
                if (transform.position == posicaoInicial)
                {
                    estado = EstadoPrensa.Parada;
                    OnPrensaRecolhida?.Invoke();
                }
                break;
        }
    }

    public void AtivarPrensa()
    {
        if (!PodeAtivar) return;
        estado = EstadoPrensa.Descendo;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (estado != EstadoPrensa.Descendo) return;

        if (collision.gameObject.CompareTag("Placa"))
        {
            SpriteRenderer sr = collision.gameObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = corPlacaPrensada;
            }

            PlacaMetal placa = collision.GetComponentInParent<PlacaMetal>();
            if (placa != null)
            {
                placa.MarcarComoPrensada(); // passa a ter o estado "prensada"
            }

            estado = EstadoPrensa.Subindo; // já prensou, volta sozinha
        }
    }

    // Mostra o trajeto no editor (linha entre os pontos)
    private void OnDrawGizmosSelected()
    {
        if (pontoInicial == null || pontoFinal == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pontoInicial.position, pontoFinal.position);
        Gizmos.DrawWireSphere(pontoInicial.position, 0.15f);
        Gizmos.DrawWireSphere(pontoFinal.position, 0.15f);
    }
}