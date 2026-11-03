using UnityEngine;

public class BotaoPrensa : MonoBehaviour
{
    [SerializeField] private PrensaController prensa;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color corNormal = Color.white;
    [SerializeField] private Color corPressionado = new Color(0.7f, 0.7f, 0.7f, 1f);

    private void OnEnable()
    {
        prensa.OnPrensaRecolhida += VoltarAoNormal;
    }

    private void OnDisable()
    {
        prensa.OnPrensaRecolhida -= VoltarAoNormal;
    }

    private void Start()
    {
        spriteRenderer.color = corNormal;
    }

    private void OnMouseDown()
    {
        if (!prensa.PodeAtivar) return;

        prensa.AtivarPrensa();
        spriteRenderer.color = corPressionado;
    }

    private void VoltarAoNormal()
    {
        spriteRenderer.color = corNormal;
    }
}