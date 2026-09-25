using UnityEngine;
using System;

public class PrensaController : MonoBehaviour
{
    [Header("Movimento da Prensa")]
    [SerializeField] private float velocidadePressao = 3f;
    [SerializeField] private Vector3 posicaoInicial;
    [SerializeField] private Vector3 posicaoFinal;

    [Header("Feedback visual (placeholder)")]
    [SerializeField] private Color corPlacaPrensada = Color.red;

    public event Action OnPrensaRecolhida;

    private enum EstadoPrensa { Parada, Descendo, Subindo }
    private EstadoPrensa estado = EstadoPrensa.Parada;

    public bool PodeAtivar => estado == EstadoPrensa.Parada;

    private void Start()
    {
        posicaoInicial = transform.position;
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

            estado = EstadoPrensa.Subindo; // já prensou, volta sozinha
        }
    }
}