using UnityEngine;
using UnityEngine.Serialization;

// Cada item aqui liga um quadrado da prancheta a uma variável do carro
public enum ItemChecklist
{
    Farol,
    Retrovisor,
    Pneus,
    Porta,
    Pintura,
    Modelo
}

// Um caso fixo: uma combinação de verdadeiro/falso definida por você
[System.Serializable]
public class CasoCarro
{
    public string nome;   // só para você se organizar
    public bool farol;
    public bool retrovisor;
    public bool pneus;
    public bool porta;
    public bool pintura;
    [FormerlySerializedAs("parabrisa")] public bool modelo;
}

public class Carro : MonoBehaviour
{
    [Header("Casos fixos deste carro (um é escolhido a cada evento)")]
    public CasoCarro[] casos;

    [Header("Variáveis atuais do carro (true = certo, false = errado)")]
    public bool farol;
    public bool retrovisor;
    public bool pneus;
    public bool porta;
    public bool pintura;
    [FormerlySerializedAs("parabrisa")] public bool modelo;

    // Roda quando o carro nasce, ou seja, a cada evento novo
    void Awake()
    {
        if (casos != null && casos.Length > 0)
        {
            AplicarCaso(casos[Random.Range(0, casos.Length)]);
        }
        // Se a lista de casos estiver vazia, vale o que estiver marcado acima
    }

    private void AplicarCaso(CasoCarro c)
    {
        farol = c.farol;
        retrovisor = c.retrovisor;
        pneus = c.pneus;
        porta = c.porta;
        pintura = c.pintura;
        modelo = c.modelo;
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
            case ItemChecklist.Modelo: conforme = modelo; return true;
        }

        conforme = false;
        return false;
    }
}