using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Botão de acionamento da prensa. Só pode ser clicado quando a alavanca de
/// pressão está no valor correto E todos os cabos estão conectados.
/// Objeto clicável na cena (Collider2D), no mesmo estilo dos cabos.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BotaoAcionamento : MonoBehaviour
{
    [Header("Referências")]
    public AlavancaPressao Alavanca;
    public CaboPrensa[] Cabos;

    [Header("Feedback visual")]
    [Tooltip("Opcional: SpriteRenderer do botão, para indicar visualmente se está habilitado.")]
    public SpriteRenderer Sprite;
    public Color CorHabilitado = new Color(0.2f, 1f, 0.3f);
    public Color CorDesabilitado = new Color(0.6f, 0.6f, 0.6f);

    [Header("Troca de cena")]
    [Tooltip("Nome da cena a ser carregada ao acionar o botão (precisa estar adicionada em File > Build Settings > Scenes In Build). Deixe vazio para não trocar de cena.")]
    public string CenaAoAcionar;

    [Header("Evento")]
    [Tooltip("Chamado quando o botão é clicado com todas as condições satisfeitas (antes da troca de cena).")]
    public UnityEvent AoAcionar;

    /// <summary>Verdadeiro quando a alavanca está correta e todos os cabos conectados.</summary>
    public bool PodeAcionar
    {
        get
        {
            if (Alavanca == null || !Alavanca.PressaoCorreta)
                return false;

            if (Cabos == null || Cabos.Length == 0)
                return false;

            foreach (CaboPrensa Cabo in Cabos)
            {
                if (Cabo == null || !Cabo.EstaConectado)
                    return false;
            }

            return true;
        }
    }

    void Update()
    {
        AtualizarVisual();
    }

    void OnMouseDown()
    {
        if (!PodeAcionar)
            return;

        AoAcionar?.Invoke();

        if (!string.IsNullOrEmpty(CenaAoAcionar))
            SceneManager.LoadScene(CenaAoAcionar);
    }

    void AtualizarVisual()
    {
        if (Sprite == null)
            return;

        Sprite.color = PodeAcionar ? CorHabilitado : CorDesabilitado;
    }
}