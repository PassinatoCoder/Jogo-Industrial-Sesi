using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
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
    private BoxCollider2D colisor;
    private Unity.Cinemachine.CinemachineImpulseSource impulseSource;

    private Vector2 inputDirecao;
    private bool estaNoChao;
    private bool estaAgachado;

    private float contadorTempoCoyote;
    private float contadorJumpBuffer;

    // Tamanhos do collider (de pé x agachado)
    private Vector2 tamanhoOriginal;
    private Vector2 offsetOriginal;
    private Vector2 tamanhoAgachado;
    private Vector2 offsetAgachado;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        colisor = GetComponent<BoxCollider2D>();
        impulseSource = GetComponent<Unity.Cinemachine.CinemachineImpulseSource>();

        tamanhoOriginal = colisor.size;
        offsetOriginal = colisor.offset;
        tamanhoAgachado = new Vector2(tamanhoOriginal.x, tamanhoOriginal.y / 2f);
        offsetAgachado = new Vector2(offsetOriginal.x, offsetOriginal.y - (tamanhoOriginal.y / 4f));
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

        if (context.canceled && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            contadorTempoCoyote = 0f;
        }
    }

    public void AoAgachar(InputAction.CallbackContext context)
    {
        if (!agacharLiberado) return;

        if (context.performed)
        {
            estaAgachado = true;
            colisor.size = tamanhoAgachado;
            colisor.offset = offsetAgachado;
        }
        else if (context.canceled)
        {
            estaAgachado = false;
            colisor.size = tamanhoOriginal;
            colisor.offset = offsetOriginal;
        }
    }

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

        if (contadorJumpBuffer > 0f && contadorTempoCoyote > 0f)
        {
            ExecutarPulo();
        }
    }

    private void ExecutarPulo()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * forcaDoPulo, ForceMode2D.Impulse);

        // Game Feel: Treme a tela levemente ao saltar
        impulseSource?.GenerateImpulse(0.4f);

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
        bool estavaNoChao = estaNoChao;
        estaNoChao = Physics2D.OverlapCircle(pontoPe.position, raioChao, layerChao);

        // Game Feel: Se bateu no chão vindo de uma queda, dispara o Screen Shake de impacto
        if (!estavaNoChao && estaNoChao && rb.linearVelocity.y < -0.1f)
        {
            impulseSource?.GenerateImpulse(0.7f);
        }
    }
}