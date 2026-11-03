using UnityEngine;

public class CameraSegue : MonoBehaviour
{
    [SerializeField] private Transform alvo;
    [SerializeField] private float suavidade = 0.125f;
    [SerializeField] private Vector3 deslocamento = new Vector3(0f, 0f, -10f);

    // Usado pela empilhadeira dirigível para trocar quem a câmera segue
    public void DefinirAlvo(Transform novoAlvo)
    {
        alvo = novoAlvo;
    }

    // Com instantaneo = true a câmera já aparece no novo alvo, sem deslizar pelo mapa
    public void DefinirAlvo(Transform novoAlvo, bool instantaneo)
    {
        alvo = novoAlvo;

        if (instantaneo && alvo != null)
            transform.position = alvo.position + deslocamento;
    }

    void LateUpdate()
    {
        if (alvo == null) return;

        Vector3 posicaoDesejada = alvo.position + deslocamento;
        transform.position = Vector3.Lerp(transform.position, posicaoDesejada, suavidade);
    }
}