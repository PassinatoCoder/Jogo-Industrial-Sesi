using System;
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
    public Transform pontoSpawnCarro;
    public Transform pontoSpawnFicha;
    public Prancheta prancheta;
    public KeyCode teclaFinalizar = KeyCode.Return;

    private Carro carroAtual;
    private GameObject fichaAtual;
    private int ultimoIndice = -1;

    void Start()
    {
        SortearProximoCarro();
    }

    void Update()
    {
        if (carroAtual != null && Input.GetKeyDown(teclaFinalizar))
        {
            FinalizarTurno();
        }
    }

    // Público: também pode ser ligado a um botão de UI
    public void FinalizarTurno()
    {
        if (carroAtual == null) return;

        // Avisa quem estiver escutando (o avaliador) ANTES de destruir o carro
        OnTurnoFinalizado?.Invoke(carroAtual);

        Destroy(carroAtual.gameObject);
        carroAtual = null;

        if (fichaAtual != null)
        {
            Destroy(fichaAtual);
            fichaAtual = null;
        }

        if (prancheta != null) prancheta.ResetarChecklist();

        SortearProximoCarro();
    }

    private void SortearProximoCarro()
    {
        if (carros == null || carros.Length == 0)
        {
            Debug.LogWarning("Nenhum carro configurado no GerenciadorTurnos!");
            return;
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

        // Spawna o carro (as variáveis são sorteadas no Awake do Carro)
        GameObject novoCarro = Instantiate(escolhido.carroPrefab, pontoSpawnCarro.position, Quaternion.identity);
        carroAtual = novoCarro.GetComponent<Carro>();

        // Spawna a ficha do carro ideal junto
        if (escolhido.fichaPrefab != null)
        {
            fichaAtual = Instantiate(escolhido.fichaPrefab, pontoSpawnFicha.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"O carro '{escolhido.nome}' está sem ficha configurada!");
        }

        OnCarroSorteado?.Invoke(carroAtual);
    }
}