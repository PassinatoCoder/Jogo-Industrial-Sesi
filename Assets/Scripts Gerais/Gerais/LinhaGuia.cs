using System.Collections.Generic;
using UnityEngine;

// Coloque no mesmo objeto da linha amarela (ou num objeto próprio) que tenha um Collider2D.
// A empilhadeira, fora da zona livre, só anda sobre estas linhas, no eixo delas.
// O sentido escolhido é o "para frente"; voltar é permitido pela empilhadeira (e vai para o log).
[RequireComponent(typeof(Collider2D))]
public class LinhaGuiaEmpilhadeira : MonoBehaviour
{
    public enum Sentido { Direita, Esquerda, Cima, Baixo }

    [SerializeField] private Sentido sentido = Sentido.Direita;
    [Tooltip("Puxa a empilhadeira para o centro da linha. Desligue em áreas largas (ex.: faixa de pedestre).")]
    [SerializeField] private bool alinharAoCentro = true;

    private static readonly List<LinhaGuiaEmpilhadeira> todas = new List<LinhaGuiaEmpilhadeira>();

    private Collider2D area;

    public bool AlinharAoCentro { get { return alinharAoCentro; } }

    public Vector2 Direcao
    {
        get
        {
            switch (sentido)
            {
                case Sentido.Esquerda: return Vector2.left;
                case Sentido.Cima: return Vector2.up;
                case Sentido.Baixo: return Vector2.down;
                default: return Vector2.right;
            }
        }
    }

    public Vector2 Centro { get { return area.bounds.center; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstatico()
    {
        todas.Clear();
    }

    void Awake()
    {
        area = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        if (!todas.Contains(this)) todas.Add(this);
    }

    void OnDisable()
    {
        todas.Remove(this);
    }

    // Usado quando a linha é criada por outro script (ex.: a faixa de pedestre)
    public void Configurar(Sentido novoSentido, bool alinhar)
    {
        sentido = novoSentido;
        alinharAoCentro = alinhar;
    }

    public static bool ExisteAlguma { get { return todas.Count > 0; } }

    // O ponto está na linha, ou a no máximo "folga" de distância dela
    private bool Perto(Vector2 ponto, float folga)
    {
        if (area == null) return false;
        if (area.OverlapPoint(ponto)) return true;
        if (folga <= 0f) return false;

        Vector2 maisProximo = area.ClosestPoint(ponto);
        return (maisProximo - ponto).sqrMagnitude <= folga * folga;
    }

    public static bool EmAlguma(Vector2 ponto, float folga)
    {
        foreach (LinhaGuiaEmpilhadeira l in todas)
        {
            if (l != null && l.Perto(ponto, folga)) return true;
        }
        return false;
    }

    // Entre as linhas próximas do ponto, devolve a que mais combina com a direção apertada
    // (para frente ou para trás: o que importa é o eixo)
    public static LinhaGuiaEmpilhadeira Escolher(Vector2 ponto, Vector2 entrada, float folga)
    {
        LinhaGuiaEmpilhadeira melhor = null;
        float melhorValor = float.NegativeInfinity;

        foreach (LinhaGuiaEmpilhadeira l in todas)
        {
            if (l == null || !l.Perto(ponto, folga)) continue;

            float valor = Mathf.Abs(Vector2.Dot(entrada, l.Direcao));
            if (valor > melhorValor)
            {
                melhorValor = valor;
                melhor = l;
            }
        }

        return melhor;
    }

    void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;

        Vector3 centro = c.bounds.center;
        Vector3 dir = Direcao;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(centro - dir * 0.6f, centro + dir * 0.6f);
        Gizmos.DrawLine(centro + dir * 0.6f, centro + dir * 0.3f + new Vector3(-dir.y, dir.x, 0f) * 0.25f);
        Gizmos.DrawLine(centro + dir * 0.6f, centro + dir * 0.3f - new Vector3(-dir.y, dir.x, 0f) * 0.25f);
    }
}