using UnityEngine;

public class AnimadorSprites : MonoBehaviour
{
    [Header("Sprites da animação (em ordem)")]
    public Sprite[] sprites;

    [Header("Configurações")]
    public float framesPorSegundo = 8f;
    public bool repetir = true;
    public bool tocarAoIniciar = true;

    private SpriteRenderer sr;
    private int frameAtual = 0;
    private float tempo = 0f;
    private bool tocando = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (tocarAoIniciar)
            Tocar();
    }

    void Update()
    {
        if (!tocando || sprites == null || sprites.Length == 0) return;

        tempo += Time.deltaTime;

        if (tempo >= 1f / framesPorSegundo)
        {
            tempo = 0f;
            frameAtual++;

            if (frameAtual >= sprites.Length)
            {
                if (repetir)
                {
                    frameAtual = 0;
                }
                else
                {
                    frameAtual = sprites.Length - 1;
                    tocando = false;
                }
            }

            sr.sprite = sprites[frameAtual];
        }
    }

    // Pode chamar de outros scripts: animador.Tocar();
    public void Tocar()
    {
        if (sprites == null || sprites.Length == 0) return;

        frameAtual = 0;
        tempo = 0f;
        tocando = true;
        sr.sprite = sprites[0];
    }

    public void Parar()
    {
        tocando = false;
    }
}