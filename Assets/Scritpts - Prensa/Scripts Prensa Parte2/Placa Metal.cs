using UnityEngine;

/// <summary>
/// Marca este objeto como uma placa de metal agarrável pelo braço robótico.
/// Não precisa de nenhuma lógica própria — o BracoRoboticoController detecta
/// esse componente ao encostar nela e passa a controlar sua posição.
/// Requer um Collider2D marcado como "Is Trigger" pra colisão ser detectada.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlacaMetal : MonoBehaviour
{
    void Reset()
    {
        // lembrete: o Collider2D deste objeto precisa estar marcado como "Is Trigger",
        // senão o braço vai colidir fisicamente com a placa em vez de agarrá-la
        GetComponent<Collider2D>().isTrigger = true;
    }
}