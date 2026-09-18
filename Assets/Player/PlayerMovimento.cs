using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovimento : MonoBehaviour
{
    [Header("Travas de Sistema (Tutorial/Cutscenes)")]
    public bool movimentoLiberado = false;
    public bool puloLiberado = false;
    public bool agacharLiberado = false;

    [Header("Agachamento")]
    [SerializeField] private float multiplicadorVelocidadeAgachado = 0.5f;

    [Header("Movimentação Horizontal")]
    [SerializeField] private float velocidadeMaxima = 8f;
    [SerializeField] private float aceleracao = 10f;
    [SerializeField] private float desaceleracao = 10f;

    [Header("Física do Pulo (Game Feel)")]
    [SerializeField] private float forcaDoPulo = 15f;
    [SerializeField] private float multiplicadorQueda = 2.5f;
    [SerializeField] private float multiplicadorPuloBaixo = 2f;
    [SerializeField] private float tempoCoyote = 0.15f;
    [SerializeField] private float tempoJumpBuffer = 0.15f;

    [Header("Detecção de Chão")]
    [SerializeField] private Transform pontoPe;
    [SerializeField] private float raioChao = 0.2f;
    [SerializeField] private LayerMask layerChao;

    private Rigidbody2D rb;
    private Vector2 inputDirecao;
    private bool estaNoChao;
    private bool estaAgachado;

    private float contadorTempoCoyote;
    private float contadorJumpBuffer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        ChecarChao();
        AtualizarTimersFisica();
        AplicarGravidadeCustomizada();
    }

    private void FixedUpdate()
    {
        MoverPlayer();
    }

    // --- MÉTODOS DE INPUT SYSTEM ---

    public void AoMover(InputAction.CallbackContext context)
    {
        if (!movimentoLiberado)
        {
            inputDirecao = Vector2.zero;
            return;
        }
        inputDirecao = context.ReadValue<Vector2>();
    }

    public void AoPular(InputAction.CallbackContext context)
    {
        if (!puloLiberado) return;

        if (context.started) contadorJumpBuffer = tempoJumpBuffer;

        // Se soltou o botão no ar, corta o pulo (Pulo variável)
        if (context.canceled && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            contadorTempoCoyote = 0f;
        }
    }

    public void AoAgachar(InputAction.CallbackContext context)
    {
        if (!agacharLiberado) return;

        if (context.performed) estaAgachado = true;
        else if (context.canceled) estaAgachado = false;
    }

    // --- MÉTODOS DE FÍSICA (GAME FEEL) ---

    private void MoverPlayer()
    {
        float velocidadeAlvo = inputDirecao.x * velocidadeMaxima;
        if (estaAgachado) velocidadeAlvo *= multiplicadorVelocidadeAgachado;

        float taxaAceleracao = (Mathf.Abs(velocidadeAlvo) > 0.01f) ? aceleracao : desaceleracao;
        float diferencaVelocidade = velocidadeAlvo - rb.linearVelocity.x;
        float movimentoX = Mathf.Pow(Mathf.Abs(diferencaVelocidade) * taxaAceleracao, 0.9f) * Mathf.Sign(diferencaVelocidade);

        rb.AddForce(movimentoX * Vector2.right);
    }

    private void AtualizarTimersFisica()
    {
        if (estaNoChao) contadorTempoCoyote = tempoCoyote;
        else contadorTempoCoyote -= Time.deltaTime;

        contadorJumpBuffer -= Time.deltaTime;

        // Tenta executar o pulo se o buffer e o coyote estiverem válidos
        if (contadorJumpBuffer > 0f && contadorTempoCoyote > 0f)
        {
            ExecutarPulo();
        }
    }

    private void ExecutarPulo()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Zera a força Y antes de pular para não acumular
        rb.AddForce(Vector2.up * forcaDoPulo, ForceMode2D.Impulse);
        contadorJumpBuffer = 0f;
        contadorTempoCoyote = 0f;
    }

    private void AplicarGravidadeCustomizada()
    {
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (multiplicadorQueda - 1) * Time.deltaTime;
        else if (rb.linearVelocity.y > 0 && !estaNoChao)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (multiplicadorPuloBaixo - 1) * Time.deltaTime;
    }

    private void ChecarChao()
    {
        estaNoChao = Physics2D.OverlapCircle(pontoPe.position, raioChao, layerChao);
    }
}