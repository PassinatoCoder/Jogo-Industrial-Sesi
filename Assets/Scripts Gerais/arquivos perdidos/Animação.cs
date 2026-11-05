using UnityEngine;

public class AnimacaoProtagonista : MonoBehaviour
{
    [Header("Sprites (arraste os PNGs na ordem)")]
    public Sprite[] parado;          // idle - usado em qualquer direção
    public Sprite[] andandoLado;     // andando para esquerda/direita
    public Sprite[] andandoCostas;   // andando para cima (de costas)
    public Sprite[] andandoFrente;   // andando para baixo (de frente)

    [Header("Velocidade das animações")]
    public float fpsParado = 4f;
    public float fpsAndando = 8f;

    private SpriteRenderer sr;
    private Sprite[] animacaoAtual;
    private int frame = 0;
    private float tempo = 0f;
    private float fpsAtual = 4f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        TrocarAnimacao(parado, fpsParado);
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool movendo = (h != 0 || v != 0);

        // Flip conforme o lado para onde avança (invertido)
        if (h > 0) sr.flipX = true;
        else if (h < 0) sr.flipX = false;

        // Escolhe a animação
        if (!movendo)
        {
            TrocarAnimacao(parado, fpsParado);
        }
        else if (Mathf.Abs(v) > Mathf.Abs(h))
        {
            // movimento mais vertical
            if (v > 0)
                TrocarAnimacao(andandoCostas, fpsAndando);   // subindo
            else
                TrocarAnimacao(andandoFrente, fpsAndando);   // descendo
        }
        else
        {
            // movimento horizontal (ou diagonal perfeita): usa a de lado
            TrocarAnimacao(andandoLado, fpsAndando);
        }

        Animar();
    }

    void TrocarAnimacao(Sprite[] nova, float fps)
    {
        if (nova == null || nova.Length == 0) return;
        if (animacaoAtual == nova) return;

        animacaoAtual = nova;
        fpsAtual = fps;
        frame = 0;
        tempo = 0f;
        sr.sprite = animacaoAtual[0];
    }

    void Animar()
    {
        if (animacaoAtual == null || animacaoAtual.Length == 0) return;

        tempo += Time.deltaTime;

        if (tempo >= 1f / fpsAtual)
        {
            tempo = 0f;
            frame = (frame + 1) % animacaoAtual.Length;
            sr.sprite = animacaoAtual[frame];
        }
    }
}