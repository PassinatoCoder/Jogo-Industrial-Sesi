using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class CarroComFicha
{
    public string nome;              // só para você se organizar no Inspector
    public GameObject carroPrefab;   // prefab do carro (com o script Carro)
    public GameObject fichaPrefab;   // prefab da ficha com o ideal desse carro
}

public class GerenciadorTurnos : MonoBehaviour
{
    // Eventos que outros scripts podem escutar
    public static event Action<Carro> OnCarroSorteado;
    public static event Action<Carro> OnTurnoFinalizado;

    public CarroComFicha[] carros;

    [Header("Pontos de movimento do carro")]
    public Transform pontoEntrada;   // fora da tela, de onde o carro entra
    public Transform pontoParada;    // onde o carro fica durante a checagem
    public Transform pontoSaida;     // fora da tela, para onde o carro sai
    public float velocidadeCarro = 8f;

    [Header("Ficha e prancheta")]
    public Transform pontoSpawnFicha;
    public Prancheta prancheta;

    public KeyCode teclaFinalizar = KeyCode.Return;

    private Carro carroAtual;
    private GameObject fichaAtual;
    private int ultimoIndice = -1;
    private bool emTransicao = false;

    void Start()
    {
        StartCoroutine(EntrarNovoCarro());
    }

    void Update()
    {
        // Enquanto o manual está aberto (ou acabou de fechar), o Enter não finaliza o turno
        if (ManualMinigame.BloqueiaInput) return;

        // Bloqueia o Enter enquanto o carro está entrando ou saindo
        if (!emTransicao && carroAtual != null && Input.GetKeyDown(teclaFinalizar))
        {
            FinalizarTurno();
        }
    }

    // Público: também pode ser ligado a um botão de UI
    public void FinalizarTurno()
    {
        if (emTransicao || carroAtual == null) return;
        StartCoroutine(RotinaFinalizar());
    }

    private IEnumerator RotinaFinalizar()
    {
        emTransicao = true;

        // Avisa o avaliador ANTES de o carro sair (as variáveis ainda estão valendo)
        OnTurnoFinalizado?.Invoke(carroAtual);

        if (fichaAtual != null)
        {
            Destroy(fichaAtual);
            fichaAtual = null;
        }

        if (prancheta != null) prancheta.ResetarChecklist();

        // Carro antigo sai da tela e só então é destruído
        yield return StartCoroutine(MoverCarro(carroAtual.transform, pontoSaida.position));
        Destroy(carroAtual.gameObject);
        carroAtual = null;

        // Novo carro entra
        yield return StartCoroutine(EntrarNovoCarro());
    }

    private IEnumerator EntrarNovoCarro()
    {
        emTransicao = true;

        if (carros == null || carros.Length == 0)
        {
            Debug.LogWarning("Nenhum carro configurado no GerenciadorTurnos!");
            emTransicao = false;
            yield break;
        }

        int indice = UnityEngine.Random.Range(0, carros.Length);

        // Evita repetir o mesmo carro duas vezes seguidas
        if (carros.Length > 1)
        {
            while (indice == ultimoIndice)
            {
                indice = UnityEngine.Random.Range(0, carros.Length);
            }
        }

        ultimoIndice = indice;
        CarroComFicha escolhido = carros[indice];

        // O carro nasce fora da tela (as variáveis são sorteadas no Awake dele)
        GameObject novoCarro = Instantiate(escolhido.carroPrefab, pontoEntrada.position, Quaternion.identity);
        carroAtual = novoCarro.GetComponent<Carro>();

        OnCarroSorteado?.Invoke(carroAtual);

        // Entra deslizando até o ponto de parada
        yield return StartCoroutine(MoverCarro(novoCarro.transform, pontoParada.position));

        // A ficha aparece quando o carro chega
        if (escolhido.fichaPrefab != null)
        {
            fichaAtual = Instantiate(escolhido.fichaPrefab, pontoSpawnFicha.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"O carro '{escolhido.nome}' está sem ficha configurada!");
        }

        emTransicao = false;
    }

    private IEnumerator MoverCarro(Transform alvo, Vector3 destino)
    {
        while (alvo != null && Vector3.Distance(alvo.position, destino) > 0.01f)
        {
            alvo.position = Vector3.MoveTowards(alvo.position, destino, velocidadeCarro * Time.deltaTime);
            yield return null;
        }

        if (alvo != null) alvo.position = destino;
    }
}