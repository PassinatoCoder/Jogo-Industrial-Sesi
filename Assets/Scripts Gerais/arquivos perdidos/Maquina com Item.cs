using UnityEngine;
using UnityEngine.SceneManagement;

// Coloque no objeto com que o jogador interage.
// Sem o item: mostra uma mensagem. Com o item: troca de cena.
public class PortaComItem : MonoBehaviour
{
    [Header("Item exigido")]
    [Tooltip("Nome exato do item (nome do GameObject do item no armário, ou o campo Nome do ItemCarregavel)")]
    [SerializeField] private string nomeItem = "Item";

    [Header("Mensagem quando NÃO tem o item")]
    [TextArea(1, 3)]
    [SerializeField] private string mensagem = "Você ainda não pode passar.";
    [Tooltip("Nome do lugar onde o item pode ser encontrado")]
    [SerializeField] private string nomeLocal = "";
    [Tooltip("Palavras que ligam a frase. Resultado: 'Busque [item] no [local]'")]
    [SerializeField] private string textoBusque = "Busque";
    [SerializeField] private string textoNo = "no";
    [SerializeField] private float duracaoMensagem = 4f;

    [Header("Cena de destino (quando TEM o item)")]
    [Tooltip("Nome da cena. Ela precisa estar em File > Build Settings.")]
    [SerializeField] private string cenaDestino = "";
#if UNITY_EDITOR
    [Tooltip("Arraste a cena aqui: o nome é preenchido sozinho no campo acima.")]
    [SerializeField] private UnityEditor.SceneAsset cenaArrastar;
#endif

    [Header("Interação")]
    [SerializeField] private KeyCode teclaInteracao = KeyCode.E;
    [SerializeField] private float distanciaInteracao = 2f;

    private Transform jogador;
    private Collider2D colisor;
    private bool trocando;

    // Mensagem na tela
    private string textoAtual = "";
    private float fimMensagem;
    private GUIStyle estilo;
    private Texture2D fundo;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cenaArrastar != null) cenaDestino = cenaArrastar.name;
    }
#endif

    private void Awake()
    {
        colisor = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (trocando) return;

        if (Input.GetKeyDown(teclaInteracao) && JogadorPerto())
            Interagir();
    }

    /// <summary>Pode ser chamado também por outro sistema de interação (botão, etc.).</summary>
    public void Interagir()
    {
        if (trocando) return;

        bool temItem = string.IsNullOrEmpty(nomeItem) || Inventario.Tem(nomeItem);

        if (temItem)
            IrParaCena();
        else
            MostrarMensagem();
    }

    // ---------------------------------------------------------------- lógica

    private void IrParaCena()
    {
        if (string.IsNullOrEmpty(cenaDestino))
        {
            Debug.LogError("PortaComItem: nenhuma cena de destino foi definida.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(cenaDestino))
        {
            Debug.LogError("PortaComItem: a cena '" + cenaDestino + "' não está em File > Build Settings.", this);
            return;
        }

        trocando = true;
        SceneManager.LoadScene(cenaDestino);
    }

    private void MostrarMensagem()
    {
        string texto = mensagem;

        string busca = textoBusque + " " + nomeItem;
        if (!string.IsNullOrEmpty(nomeLocal))
            busca += " " + textoNo + " " + nomeLocal;

        texto = string.IsNullOrEmpty(texto) ? busca : texto + "\n" + busca;

        textoAtual = texto;
        fimMensagem = Time.unscaledTime + duracaoMensagem;
    }

    private bool JogadorPerto()
    {
        if (jogador == null)
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go != null) jogador = go.transform;
        }

        if (jogador == null) return true; // sem Player na cena, não bloqueia

        Vector2 p = jogador.position;

        if (colisor != null)
            return Vector2.Distance(colisor.ClosestPoint(p), p) <= distanciaInteracao;

        return Vector2.Distance(transform.position, p) <= distanciaInteracao;
    }

    // ---------------------------------------------------------------- mensagem na tela (sem Canvas)

    private void OnGUI()
    {
        if (Time.unscaledTime >= fimMensagem || string.IsNullOrEmpty(textoAtual)) return;

        if (estilo == null) CriarEstilo();

        estilo.fontSize = Mathf.Max(14, Mathf.RoundToInt(Screen.height * 0.035f));

        float largura = Screen.width * 0.7f;
        float altura = estilo.CalcHeight(new GUIContent(textoAtual), largura);
        Rect area = new Rect((Screen.width - largura) / 2f, Screen.height * 0.85f - altura, largura, altura);

        // Some suavemente no último meio segundo
        float restante = fimMensagem - Time.unscaledTime;
        float alfa = Mathf.Clamp01(restante / 0.5f);

        Color anterior = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alfa);
        GUI.Box(area, textoAtual, estilo);
        GUI.color = anterior;
    }

    private void CriarEstilo()
    {
        fundo = new Texture2D(1, 1);
        fundo.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
        fundo.Apply();

        estilo = new GUIStyle(GUI.skin.box);
        estilo.normal.background = fundo;
        estilo.normal.textColor = Color.white;
        estilo.alignment = TextAnchor.MiddleCenter;
        estilo.wordWrap = true;
        estilo.padding = new RectOffset(24, 24, 16, 16);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaInteracao);
    }
}