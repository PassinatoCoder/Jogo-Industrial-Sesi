using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class QuadradoChecklist : MonoBehaviour
{
    [Header("Qual variável do carro esse quadrado representa")]
    public ItemChecklist item;

    [Header("Variável da prancheta (true = certo, false = errado)")]
    public bool marcadoComoCerto = false;

    [Header("Cores")]
    public Color corCerto = Color.green;
    public Color corErrado = Color.red;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        AtualizarCor();
    }

    // Clicar no quadrado alterna entre true e false
    void OnMouseDown()
    {
        marcadoComoCerto = !marcadoComoCerto;
        AtualizarCor();
    }

    // O gerenciador chama isso no fim de cada turno
    public void Resetar()
    {
        marcadoComoCerto = false;
        AtualizarCor();
    }

    private void AtualizarCor()
    {
        sr.color = marcadoComoCerto ? corCerto : corErrado;
    }
}