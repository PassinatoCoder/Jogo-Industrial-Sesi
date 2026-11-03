using UnityEngine;

// Coloque em cada chave/botão da vista do painel (precisa de Collider2D para o clique do mouse).
[RequireComponent(typeof(Collider2D))]
public class ChaveDoSetor : MonoBehaviour
{
    [SerializeField] private SetorFabrica setor;
    [SerializeField] private SpriteRenderer corpo;

    [Header("Visual")]
    [Tooltip("Opcionais: se os dois existirem, o sprite troca entre ligada e desligada.")]
    [SerializeField] private Sprite spriteLigada;
    [SerializeField] private Sprite spriteDesligada;
    [SerializeField] private Color corLigada = new Color(0.35f, 0.85f, 0.35f);
    [SerializeField] private Color corDesligada = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color corProblemaA = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color corProblemaB = new Color(1f, 0.65f, 0.1f);
    [SerializeField] private float velocidadePisca = 6f;

    [Tooltip("Opcional: objeto (moldura, seta) que aparece quando a chave está selecionada. " +
             "Se vazio, a chave só aumenta um pouco.")]
    [SerializeField] private GameObject destaque;

    private Vector3 escalaOriginal;

    public SetorFabrica Setor { get { return setor; } }

    void Awake()
    {
        if (corpo == null) corpo = GetComponentInChildren<SpriteRenderer>();
        escalaOriginal = transform.localScale;
        if (destaque != null) destaque.SetActive(false);
    }

    void Start()
    {
        if (setor == null)
            Debug.LogWarning("ChaveDoSetor '" + name + "': falta ligar o SetorFabrica.");
    }

    void Update()
    {
        if (setor == null || corpo == null) return;

        if (spriteLigada != null && spriteDesligada != null)
            corpo.sprite = setor.Ligado ? spriteLigada : spriteDesligada;

        if (setor.ComProblema)
            corpo.color = Color.Lerp(corProblemaA, corProblemaB, Mathf.PingPong(Time.time * velocidadePisca, 1f));
        else
            corpo.color = setor.Ligado ? corLigada : corDesligada;
    }

    public void DefinirSelecionada(bool selecionada)
    {
        if (destaque != null) destaque.SetActive(selecionada);
        else transform.localScale = escalaOriginal * (selecionada ? 1.12f : 1f);
    }

    public void Alternar()
    {
        if (setor != null) setor.DefinirLigado(!setor.Ligado);
    }
}