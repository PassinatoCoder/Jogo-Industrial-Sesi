using UnityEngine;
using UnityEngine.SceneManagement;

// Coloque no objeto de interação. Perto dele, apertar E troca de cena.
public class IrParaCena : MonoBehaviour
{
    [Tooltip("Nome da cena (precisa estar em File > Build Settings)")]
    [SerializeField] private string cena = "";
#if UNITY_EDITOR
    [Tooltip("Arraste a cena aqui: o nome é preenchido sozinho no campo acima.")]
    [SerializeField] private UnityEditor.SceneAsset arrastarCena;

    private void OnValidate()
    {
        if (arrastarCena != null) cena = arrastarCena.name;
    }
#endif

    [SerializeField] private KeyCode tecla = KeyCode.E;
    [SerializeField] private float distancia = 2f;

    private Transform jogador;

    private void Update()
    {
        if (!Input.GetKeyDown(tecla) || !JogadorPerto()) return;

        if (!Application.CanStreamedLevelBeLoaded(cena))
        {
            Debug.LogError("IrParaCena: a cena '" + cena + "' não está em File > Build Settings.", this);
            return;
        }

        SceneManager.LoadScene(cena);
    }

    private bool JogadorPerto()
    {
        if (jogador == null)
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go != null) jogador = go.transform;
        }

        if (jogador == null) return true; // sem Player na cena, não bloqueia
        return Vector2.Distance(jogador.position, transform.position) <= distancia;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distancia);
    }
}