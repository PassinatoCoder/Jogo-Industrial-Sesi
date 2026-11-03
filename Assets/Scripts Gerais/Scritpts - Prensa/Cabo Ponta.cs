using UnityEngine;

/// <summary>
/// Fica na "ponta" do cabo (objeto com Collider2D + Rigidbody2D Kinematic).
/// Permite arrastar com o mouse e só encaixa se cair no Receptor correspondente.
/// Versão 2D: usa OnMouseDown/Drag/Up (que funcionam com Collider2D também)
/// e OnTriggerEnter2D para detectar o encaixe.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class CaboPonta : MonoBehaviour
{
    public CaboPrensa Cabo;

    private Camera Cam;
    private bool Arrastando;
    private float Z; // profundidade original do objeto em relação à câmera

    void Start()
    {
        Cam = Camera.main;
        Z = transform.position.z;
    }

    void OnMouseDown()
    {
        if (Cabo.EstaConectado)
            return; // ponta já encaixada não pode ser arrastada de novo

        Arrastando = true;
    }

    void OnMouseDrag()
    {
        if (!Arrastando)
            return;

        Vector3 Mouse = Input.mousePosition;
        Mouse.z = Cam.WorldToScreenPoint(transform.position).z; // mantém a mesma profundidade da câmera

        Vector3 Ponto = Cam.ScreenToWorldPoint(Mouse);
        Ponto.z = Z; // trava o eixo Z (essencial em 2D)

        transform.position = Ponto;
    }

    void OnMouseUp()
    {
        Arrastando = false;
    }

    void OnTriggerEnter2D(Collider2D Outro)
    {
        if (Cabo.EstaConectado)
            return;

        CaboReceptor Receptor = Outro.GetComponent<CaboReceptor>();
        if (Receptor == null)
            return;

        // com 4 cabos, é importante checar se é o receptor DESTE cabo,
        // senão qualquer ponta encaixaria em qualquer soquete
        if (Receptor.CaboDono != Cabo)
            return;

        transform.position = Receptor.transform.position;
        Cabo.Conectar();
        Arrastando = false;
    }
}