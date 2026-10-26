using System.Collections.Generic;
using UnityEngine;

public class Prancheta : MonoBehaviour
{
    public float velocidade = 8f;
    public float yBaixo = -6f;   // posição da prancheta abaixada
    public float yCima = -2f;    // posição da prancheta levantada

    [HideInInspector] public List<QuadradoChecklist> quadrados = new List<QuadradoChecklist>();

    private float yAlvo;

    void Awake()
    {
        quadrados = new List<QuadradoChecklist>(GetComponentsInChildren<QuadradoChecklist>());
    }

    void Start()
    {
        // Começa abaixada
        yAlvo = yBaixo;
        Vector3 pos = transform.position;
        pos.y = yBaixo;
        transform.position = pos;
    }

    void Update()
    {
        // Um toque basta: define o destino e ela vai sozinha
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            yAlvo = yCima;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            yAlvo = yBaixo;
        }

        Vector3 pos = transform.position;
        pos.y = Mathf.MoveTowards(pos.y, yAlvo, velocidade * Time.deltaTime);
        transform.position = pos;
    }

    public void ResetarChecklist()
    {
        foreach (QuadradoChecklist q in quadrados)
        {
            q.Resetar();
        }
    }
}