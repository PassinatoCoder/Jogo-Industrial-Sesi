using UnityEngine;

/// <summary>
/// Botão clicável na cena (Collider2D, mesmo estilo dos cabos e do BotaoAcionamento).
/// Primeiro clique: estende o braço. Segundo clique (com o braço já parado na posição
/// de trabalho): recolhe o braço, deixando a placa onde o jogador a posicionou.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BotaoBraco : MonoBehaviour
{
    [Header("Referências")]
    public BracoRoboticoController Braco;

    [Header("Feedback visual")]
    [Tooltip("Opcional: SpriteRenderer do botão, pra indicar se ele pode ser clicado agora.")]
    public SpriteRenderer Sprite;
    public Color CorDisponivel = new Color(0.2f, 1f, 0.3f);
    public Color CorEmTransicao = new Color(0.6f, 0.6f, 0.6f);

    void Update()
    {
        AtualizarVisual();
    }

    void OnMouseDown()
    {
        if (Braco == null || Braco.EmTransicao)
            return; // ignora clique enquanto o braço já está subindo/descendo

        Braco.AlternarBraco();
    }

    void AtualizarVisual()
    {
        if (Sprite == null || Braco == null)
            return;

        Sprite.color = Braco.EmTransicao ? CorEmTransicao : CorDisponivel;
    }
}