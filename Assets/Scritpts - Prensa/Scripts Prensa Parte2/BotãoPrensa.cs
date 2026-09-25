using UnityEngine;

public class BotaoPrensa : MonoBehaviour
{
    [SerializeField] private PrensaController prensa;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spritePressionado;

    private void OnEnable()
    {
        prensa.OnPrensaRecolhida += VoltarAoNormal;
    }

    private void OnDisable()
    {
        prensa.OnPrensaRecolhida -= VoltarAoNormal;
    }

    private void OnMouseDown()
    {
        if (!prensa.PodeAtivar) return;

        prensa.AtivarPrensa();
        spriteRenderer.sprite = spritePressionado;
    }

    private void VoltarAoNormal()
    {
        spriteRenderer.sprite = spriteNormal;
    }
}