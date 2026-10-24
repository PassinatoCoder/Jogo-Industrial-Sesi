using UnityEngine;

/// <summary>
/// Controla a placa de metal enquanto o braço robótico estiver parado na posição
/// de trabalho (Braco.PodeAjustarPlaca == true). Aceita arraste com o mouse E
/// movimento horizontal pelas teclas A/D (ou setas esquerda/direita).
/// Fora do estado de ajuste, a placa fica travada onde está — inclusive depois
/// que o braço recolhe, mantendo a posição ajustada.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ControlePlaca : MonoBehaviour
{
    [Header("Referências")]
    public BracoRoboticoController Braco;

    [Header("Controle por teclado (A/D)")]
    [Tooltip("Velocidade de movimento horizontal ao segurar A/D (ou setas).")]
    public float VelocidadeTeclado = 3f;

    private Camera Cam;
    private bool Arrastando;
    private float Z; // profundidade original em relação à câmera

    void Start()
    {
        Cam = Camera.main;
        Z = transform.position.z;
    }

    void Update()
    {
        if (Braco == null || !Braco.PodeAjustarPlaca)
            return;

        // não mexe pelo teclado enquanto o mouse estiver arrastando, pra não brigar os dois controles
        if (Arrastando)
            return;

        float Direcao = Input.GetAxisRaw("Horizontal"); // A/D e setas esquerda/direita por padrão no Unity
        if (Mathf.Abs(Direcao) > 0f)
            transform.position += new Vector3(Direcao * VelocidadeTeclado * Time.deltaTime, 0f, 0f);
    }

    void OnMouseDown()
    {
        if (Braco == null || !Braco.PodeAjustarPlaca)
            return;

        Arrastando = true;
    }

    void OnMouseDrag()
    {
        if (!Arrastando)
            return;

        // se o braço saiu do estado de ajuste no meio do arraste (ex: jogador clicou
        // no botão), solta a placa imediatamente na posição atual
        if (Braco == null || !Braco.PodeAjustarPlaca)
        {
            Arrastando = false;
            return;
        }

        Vector3 Mouse = Input.mousePosition;
        Mouse.z = Cam.WorldToScreenPoint(transform.position).z;

        Vector3 Ponto = Cam.ScreenToWorldPoint(Mouse);
        Ponto.z = Z;

        transform.position = Ponto;
    }

    void OnMouseUp()
    {
        Arrastando = false;
    }
}