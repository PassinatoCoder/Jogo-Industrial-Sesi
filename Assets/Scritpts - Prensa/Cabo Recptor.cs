using UnityEngine;

/// <summary>
/// Marca um soquete/receptor e a qual cabo ele pertence,
/// para impedir que a ponta errada encaixe nele. Versão 2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CaboReceptor : MonoBehaviour
{
    public CaboPrensa CaboDono;

    void Reset()
    {
        // lembrete: o Collider2D deste objeto precisa estar marcado como "Is Trigger"
        GetComponent<Collider2D>().isTrigger = true;
    }
}