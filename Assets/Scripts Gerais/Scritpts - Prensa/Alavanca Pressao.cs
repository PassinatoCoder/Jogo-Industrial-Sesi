using UnityEngine;
using TMPro;

/// <summary>
/// Alavanca de pressão da prensa: o jogador segura o clique e move o mouse
/// para cima/baixo para subir/descer a pressão. Versão 2D (Collider2D).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AlavancaPressao : MonoBehaviour
{
    [Header("Pressão")]
    public float Pressao = 0f;
    public float PressaoMinima = 0f;
    public float PressaoMaxima = 20f;
    public float PressaoAlvo = 10f;
    public float Tolerancia = 0.1f;

    [Header("Controle")]
    public float Velocidade = 5f;
    [Tooltip("Taxa constante (por segundo) com que a pressão sobe/desce enquanto a alavanca é mantida pra cima ou pra baixo.")]
    public float VelocidadeVariacaoConstante = 3f;
    [Tooltip("Deslocamento mínimo do sprite para considerar que a alavanca está 'pra cima' ou 'pra baixo'.")]
    public float ToleranciaDirecao = 0.01f;

    [Header("UI")]
    public TMP_Text TextoPressao;
    public Color CorNormal = Color.white;
    public Color CorCorreta = new Color(0.2f, 1f, 0.3f);

    [Header("Efeito visual do sprite")]
    [Tooltip("Transform do sprite que vai subir/descer. Se vazio, usa o próprio transform deste objeto.")]
    public Transform SpriteAlavanca;
    [Tooltip("Quanto o sprite se desloca no eixo Y (em unidades do mundo) no máximo.")]
    public float AmplitudeMovimento = 0.15f;
    [Tooltip("Velocidade com que o sprite acompanha o movimento do mouse.")]
    public float VelocidadeMovimentoSprite = 8f;
    [Tooltip("Velocidade com que o sprite volta à posição neutra ao soltar.")]
    public float VelocidadeRetorno = 6f;

    private bool Segurando;
    private Vector3 PosicaoBaseSprite;
    private float OffsetAtual;

    /// <summary>Verdadeiro quando a pressão está dentro da tolerância do alvo (10T por padrão).</summary>
    public bool PressaoCorreta => Mathf.Abs(Pressao - PressaoAlvo) < Tolerancia;

    void Awake()
    {
        if (SpriteAlavanca == null)
            SpriteAlavanca = transform;

        PosicaoBaseSprite = SpriteAlavanca.localPosition;
    }

    void Start()
    {
        if (TextoPressao == null)
        {
            Debug.LogWarning($"[{nameof(AlavancaPressao)}] 'TextoPressao' não foi atribuído no Inspector. " +
                              "O texto de pressão não será exibido até que um TMP_Text seja vinculado.", this);
        }

        AtualizarTexto();
    }

    void Update()
    {
        if (Segurando)
        {
            float Movimento = Input.GetAxis("Mouse Y");

            // Pra cima (Movimento > 0) a pressão sobe; pra baixo (Movimento < 0) ela desce.
            Pressao += Movimento * Velocidade * Time.deltaTime;
            Pressao = Mathf.Clamp(Pressao, PressaoMinima, PressaoMaxima);

            AtualizarTexto();

            // Acumula o deslocamento do sprite enquanto segura, sem voltar sozinho.
            // Quando o mouse para de se mover (Movimento == 0), o offset fica parado onde está.
            OffsetAtual += Movimento * VelocidadeMovimentoSprite * Time.deltaTime;
            OffsetAtual = Mathf.Clamp(OffsetAtual, -AmplitudeMovimento, AmplitudeMovimento);
        }
        else
        {
            // Só retorna suavemente à posição neutra depois de soltar a alavanca.
            OffsetAtual = Mathf.Lerp(OffsetAtual, 0f, Time.deltaTime * VelocidadeRetorno);
        }

        SpriteAlavanca.localPosition = PosicaoBaseSprite + new Vector3(0f, OffsetAtual, 0f);
    }

    void OnMouseDown()
    {
        Segurando = true;
    }

    void OnMouseUp()
    {
        Segurando = false;
    }

    void AtualizarTexto()
    {
        if (TextoPressao == null)
            return;

        TextoPressao.text = Pressao.ToString("F1") + " T";
        TextoPressao.color = PressaoCorreta ? CorCorreta : CorNormal;
    }
}