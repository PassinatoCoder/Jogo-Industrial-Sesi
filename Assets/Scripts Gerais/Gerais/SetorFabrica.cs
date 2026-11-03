using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Coloque num objeto de cada setor da fábrica (o objeto precisa ficar sempre ativo).
public class SetorFabrica : MonoBehaviour
{
    [SerializeField] private string nome = "Setor";

    [Tooltip("Filho do setor (faíscas, luz vermelha, fumaça...) que só aparece durante o problema. " +
             "Não use o próprio objeto do setor.")]
    [SerializeField] private GameObject alertaVisual;

    [Tooltip("Depois de desligar para resolver um problema, o setor volta a ligar sozinho após tantos segundos. " +
             "0 = só liga quando a chave for religada.")]
    [SerializeField] private float religarSozinhoApos = 10f;

    [Header("Eventos (opcional)")]
    [SerializeField] private UnityEvent aoComecarProblema;
    [SerializeField] private UnityEvent aoResolverProblema;
    [SerializeField] private UnityEvent aoDesligar;
    [SerializeField] private UnityEvent aoLigar;
    [Tooltip("Desligou um setor que não tinha problema (bom lugar para uma penalidade).")]
    [SerializeField] private UnityEvent aoDesligarSemProblema;

    private Coroutine religar;

    public string Nome { get { return string.IsNullOrEmpty(nome) ? gameObject.name : nome; } }
    public bool Ligado { get; private set; }
    public bool ComProblema { get; private set; }

    // Aumenta a cada problema novo (serve para saber se o problema é o mesmo de antes)
    public int NumeroDoProblema { get; private set; }

    void Awake()
    {
        Ligado = true;
        if (alertaVisual != null) alertaVisual.SetActive(false);
    }

    // Só começa se o setor estiver ligado e sem problema
    public bool IniciarProblema()
    {
        if (!Ligado || ComProblema) return false;

        ComProblema = true;
        NumeroDoProblema++;

        if (alertaVisual != null) alertaVisual.SetActive(true);
        aoComecarProblema.Invoke();
        return true;
    }

    // É o que a chave do painel chama. Desligar um setor com problema resolve o problema.
    public void DefinirLigado(bool ligado)
    {
        if (Ligado == ligado) return;
        Ligado = ligado;

        if (religar != null)
        {
            StopCoroutine(religar);
            religar = null;
        }

        if (ligado)
        {
            aoLigar.Invoke();
            return;
        }

        aoDesligar.Invoke();

        if (ComProblema)
        {
            ResolverProblema();

            if (religarSozinhoApos > 0f)
                religar = StartCoroutine(ReligarDepois());
        }
        else
        {
            aoDesligarSemProblema.Invoke();
        }
    }

    private void ResolverProblema()
    {
        ComProblema = false;
        if (alertaVisual != null) alertaVisual.SetActive(false);
        aoResolverProblema.Invoke();
    }

    private IEnumerator ReligarDepois()
    {
        yield return new WaitForSeconds(religarSozinhoApos);

        religar = null;
        DefinirLigado(true);
    }
}