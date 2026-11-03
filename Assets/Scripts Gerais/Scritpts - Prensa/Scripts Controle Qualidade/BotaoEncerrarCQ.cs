using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class BotaoEncerrarMinigame : MonoBehaviour
{
    [Header("Para onde o botão leva")]
    public string nomeCenaDestino;     // nome exato da cena (precisa estar no Build Settings)

    [Header("Cores")]
    public Color corBloqueado = new Color(0.5f, 0.5f, 0.5f, 1f);   // cinza: ainda não pode sair
    public Color corLiberado = Color.green;                        // verde: já pode sair

    [Header("Efeito de troca de cor")]
    public float velocidade = 8f;

    private SpriteRenderer sr;
    private Color corAlvo;
    private bool liberado = false;
    private bool iniciado = false;

    public bool EstaLiberado
    {
        get { return liberado; }
    }

    void Awake()
    {
        Iniciar();
    }

    // Seguro de chamar várias vezes e em qualquer ordem
    private void Iniciar()
    {
        if (iniciado) return;
        iniciado = true;

        sr = GetComponent<SpriteRenderer>();
        corAlvo = corBloqueado;
        sr.color = corBloqueado;
    }

    void Update()
    {
        if (sr == null) return;

        // Transição suave de cor
        sr.color = Color.Lerp(sr.color, corAlvo, velocidade * Time.deltaTime);
    }

    // O contador chama isso quando a meta de carros é atingida
    public void Liberar()
    {
        Iniciar();
        liberado = true;
        corAlvo = corLiberado;
    }

    void OnMouseDown()
    {
        if (!liberado)
        {
            Debug.Log("Ainda não analisou carros suficientes para sair.");
            return;
        }

        if (string.IsNullOrEmpty(nomeCenaDestino))
        {
            Debug.LogWarning("O botão de sair está sem nome de cena de destino!");
            return;
        }

        SceneManager.LoadScene(nomeCenaDestino);
    }
}