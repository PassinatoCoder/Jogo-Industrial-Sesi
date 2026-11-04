using UnityEngine;

public class Porta : MonoBehaviour
{
    [Header("Interação")]
    public string tagPlayer = "Player";
    public float distanciaInteracao = 2f;
    public KeyCode teclaInteracao = KeyCode.E;

    [Header("Rotação")]
    [Tooltip("2D: (0,0,90) | 3D: (0,90,0). Use sinal negativo para girar ao contrário.")]
    public Vector3 anguloAberto = new Vector3(0f, 0f, 90f);
    public float velocidade = 4f;

    [Header("Estado")]
    public bool aberta = false;

    private Quaternion rotacaoFechada;
    private Quaternion rotacaoAberta;
    private Transform player;

    void Start()
    {
        rotacaoFechada = transform.localRotation;
        rotacaoAberta = rotacaoFechada * Quaternion.Euler(anguloAberto);

        GameObject obj = GameObject.FindGameObjectWithTag(tagPlayer);
        if (obj != null)
            player = obj.transform;
        else
            Debug.LogWarning("Porta: nenhum objeto com a tag '" + tagPlayer + "' encontrado.");
    }

    void Update()
    {
        if (player != null &&
            Vector3.Distance(transform.position, player.position) <= distanciaInteracao &&
            Input.GetKeyDown(teclaInteracao))
        {
            aberta = !aberta;
        }

        Quaternion alvo = aberta ? rotacaoAberta : rotacaoFechada;
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            alvo,
            Time.deltaTime * velocidade
        );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaInteracao);
    }
}   