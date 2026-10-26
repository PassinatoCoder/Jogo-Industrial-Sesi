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
        int acertos = 0;
        int erros = 0;

        foreach (QuadradoChecklist q in prancheta.quadrados)
        {
            // Se o carro não tem esse item, ignora
            if (!carro.TryGetConforme(q.item, out bool conformeReal)) continue;

            if (q.marcadoComoCerto == conformeReal)
            {
                pontos++;
                acertos++;
                Debug.Log($"ACERTOU em {q.item} (+1)");
            }
            else
            {
                pontos--;
                erros++;
                Debug.Log($"ERROU em {q.item} (-1)");
            }
        }

        Debug.Log($"Fim do turno: {acertos} acertos, {erros} erros | Pontuação total: {pontos}");
    }
}