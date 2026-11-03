using UnityEngine;

public class AvaliadorPontuacao : MonoBehaviour
{
    public Prancheta prancheta;
    public int pontos = 0;

    void OnEnable()
    {
        GerenciadorTurnos.OnTurnoFinalizado += Avaliar;
    }

    void OnDisable()
    {
        GerenciadorTurnos.OnTurnoFinalizado -= Avaliar;
    }

    private void Avaliar(Carro carro)
    {
        bool tudoCerto = true;

        foreach (QuadradoChecklist q in prancheta.quadrados)
        {
            // Se o carro não tem esse item, ignora
            if (!carro.TryGetConforme(q.item, out bool conformeReal)) continue;

            // Variável da prancheta diferente da variável do carro = erro
            if (q.marcadoComoCerto != conformeReal)
            {
                tudoCerto = false;
                Debug.Log($"ERROU em {q.item}");
            }
        }

        if (tudoCerto)
        {
            pontos++;
            Debug.Log($"Tudo certo! (+1) | Pontuação total: {pontos}");
        }
        else
        {
            pontos--;
            Debug.Log($"Teve erro! (-1) | Pontuação total: {pontos}");
        }
    }
}