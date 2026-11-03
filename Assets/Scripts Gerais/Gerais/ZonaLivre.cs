using System.Collections.Generic;
using UnityEngine;

// Coloque num objeto com um Collider2D (Is Trigger) cobrindo a área de direção livre.
[RequireComponent(typeof(Collider2D))]
public class ZonaLivreEmpilhadeira : MonoBehaviour
{
    private static readonly List<ZonaLivreEmpilhadeira> todas = new List<ZonaLivreEmpilhadeira>();

    public static bool ExisteAlguma { get { return todas.Count > 0; } }

    private Collider2D area;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstatico()
    {
        todas.Clear();
    }

    void Awake()
    {
        area = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        if (!todas.Contains(this)) todas.Add(this);
    }

    void OnDisable()
    {
        todas.Remove(this);
    }

    public static bool Contem(Vector2 ponto)
    {
        foreach (ZonaLivreEmpilhadeira z in todas)
        {
            if (z != null && z.area != null && z.area.OverlapPoint(ponto)) return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
}