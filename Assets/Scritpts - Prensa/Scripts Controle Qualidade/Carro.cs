using UnityEngine;

// Cada item aqui liga um quadrado da prancheta a uma variável do carro
public enum ItemChecklist
{
    Farol,
    Retrovisor,
    Pneus,
    Porta,
    Pintura,
    Parabrisa
}

public class Carro : MonoBehaviour
{
    [Header("Variáveis do carro (true = certo, false = errado)")]
    public bool farol;
    public bool retrovisor;
    public bool pneus;
    public bool porta;
    public bool pintura;
    public bool parabrisa;

    [Header("Chance de cada variável vir errada")]
    [Range(0f, 1f)]
    public float chanceDeErro = 0.4f;

    // Roda sempre que o carro nasce, então cada evento gera um carro diferente
    void Awake()
    {
        Sortear();
    }

    public void Sortear()
    {
        farol = SortearVariavel();
        retrovisor = SortearVariavel();
        pneus = SortearVariavel();
        porta = SortearVariavel();
        pintura = SortearVariavel();
        parabrisa = SortearVariavel();
    }

    private bool SortearVariavel()
    {
        return Random.value > chanceDeErro;
    }

    // O avaliador usa esse método para ler a variável do carro
    public bool TryGetConforme(ItemChecklist item, out bool conforme)
    {
        switch (item)
        {
            case ItemChecklist.Farol: conforme = farol; return true;
            case ItemChecklist.Retrovisor: conforme = retrovisor; return true;
            case ItemChecklist.Pneus: conforme = pneus; return true;
            case ItemChecklist.Porta: conforme = porta; return true;
            case ItemChecklist.Pintura: conforme = pintura; return true;
            case ItemChecklist.Parabrisa: conforme = parabrisa; return true;
        }

        conforme = false;
        return false;
    }
}