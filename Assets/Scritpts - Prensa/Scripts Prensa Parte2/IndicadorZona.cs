using UnityEngine;

/// <summary>
/// Indicador circular (a "luz") que muda de cor e, opcionalmente, de sprite
/// quando a placa está centralizada corretamente na ZonaPosicionamento vinculada.
/// </summary>
public class IndicadorZona : MonoBehaviour
{
    [Header("Referências")]
    public ZonaPosicionamento Zona;
    public SpriteRenderer Sprite;

    [Header("Cores")]
    public Color CorApagado = new Color(1f, 1f, 1f, 0.3f);
    public Color CorAceso = new Color(0.3f, 1f, 0.3f, 1f);

    [Header("Sprites (opcional)")]
    [Tooltip("Se os dois campos abaixo forem preenchidos, o sprite também troca junto com a cor (ex: círculo apagado -> círculo com brilho/glow).")]
    public Sprite SpriteApagado;
    public Sprite SpriteAceso;

    void Update()
    {
        if (Zona == null || Sprite == null)
            return;

        bool Correto = Zona.PlacaCentralizada;

        Sprite.color = Correto ? CorAceso : CorApagado;

        if (SpriteApagado != null && SpriteAceso != null)
            Sprite.sprite = Correto ? SpriteAceso : SpriteApagado;
    }
}