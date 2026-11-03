using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class EventoSetor : UnityEvent<SetorFabrica> { }

// Coloque num objeto vazio da cena. Sorteia problemas nos setores, sem ritmo fixo.
public class ControladorDeSetores : MonoBehaviour
{
    [Tooltip("Se vazio, usa todos os SetorFabrica da cena.")]
    [SerializeField] private SetorFabrica[] setores;
    [SerializeField] private bool ativo = true;

    [Header("Quando os problemas acontecem (segundos)")]
    [SerializeField] private float atrasoInicialMinimo = 30f;
    [SerializeField] private float atrasoInicialMaximo = 90f;
    [SerializeField] private float intervaloMinimo = 60f;
    [SerializeField] private float intervaloMaximo = 240f;

    [Tooltip("1 = os intervalos se espalham por igual. Menor que 1 favorece esperas longas; maior que 1 favorece esperas curtas.")]
    [SerializeField] private float curvaDosIntervalos = 1f;

    [Tooltip("Chance de o próximo problema vir logo em seguida (quebra qualquer ritmo).")]
    [Range(0f, 1f)]
    [SerializeField] private float chanceDeSeguidos = 0.15f;
    [SerializeField] private float intervaloSeguidoMinimo = 6f;
    [SerializeField] private float intervaloSeguidoMaximo = 20f;

    [SerializeField] private int maximoSimultaneo = 2;

    [Header("Problema ignorado (opcional)")]
    [Tooltip("Segundos até o problema 'estourar'. 0 = sem limite.")]
    [SerializeField] private float tempoLimite = 0f;
    [SerializeField] private EventoSetor aoEstourar;

    private SetorFabrica ultimoSetor;

    void Start()
    {
        if (setores == null || setores.Length == 0)
            setores = FindObjectsByType<SetorFabrica>(FindObjectsSortMode.None);

        if (setores.Length == 0)
        {
            Debug.LogWarning("ControladorDeSetores: não encontrei nenhum SetorFabrica na cena.");
            return;
        }

        StartCoroutine(Ciclo());
    }

    private IEnumerator Ciclo()
    {
        yield return new WaitForSeconds(Random.Range(atrasoInicialMinimo, atrasoInicialMaximo));

        while (true)
        {
            if (!ativo)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            if (ProblemasAtivos() < maximoSimultaneo)
            {
                SetorFabrica escolhido = Sortear();
                if (escolhido != null) Disparar(escolhido);
            }

            yield return new WaitForSeconds(ProximaEspera());
        }
    }

    private float ProximaEspera()
    {
        if (Random.value < chanceDeSeguidos)
            return Random.Range(intervaloSeguidoMinimo, intervaloSeguidoMaximo);

        float t = Mathf.Pow(Random.value, curvaDosIntervalos);
        return Mathf.Lerp(intervaloMinimo, intervaloMaximo, t);
    }

    private int ProblemasAtivos()
    {
        int total = 0;
        foreach (SetorFabrica s in setores)
        {
            if (s != null && s.ComProblema) total++;
        }
        return total;
    }

    // Só setores ligados e sem problema; evita repetir o mesmo setor duas vezes seguidas
    private SetorFabrica Sortear()
    {
        List<SetorFabrica> candidatos = new List<SetorFabrica>();

        foreach (SetorFabrica s in setores)
        {
            if (s != null && s.Ligado && !s.ComProblema) candidatos.Add(s);
        }

        if (candidatos.Count == 0) return null;
        if (candidatos.Count > 1 && ultimoSetor != null) candidatos.Remove(ultimoSetor);

        return candidatos[Random.Range(0, candidatos.Count)];
    }

    private void Disparar(SetorFabrica setor)
    {
        if (!setor.IniciarProblema()) return;

        ultimoSetor = setor;

        Debug.Log("ControladorDeSetores: problema no setor '" + setor.Nome + "'.");
        AvisoTela.Instancia.Mostrar("ALERTA: problema no setor " + setor.Nome + "!", 5f,
                                    new Color(1f, 0.4f, 0.35f));

        if (tempoLimite > 0f) StartCoroutine(Vigiar(setor));
    }

    private IEnumerator Vigiar(SetorFabrica setor)
    {
        int numero = setor.NumeroDoProblema;

        yield return new WaitForSeconds(tempoLimite);

        // Só estoura se ainda for o mesmo problema e ninguém resolveu
        if (setor != null && setor.ComProblema && setor.NumeroDoProblema == numero)
        {
            Debug.LogWarning("ControladorDeSetores: o problema no setor '" + setor.Nome + "' estourou.");
            AvisoTela.Instancia.Mostrar("O setor " + setor.Nome + " não foi desligado a tempo!", 5f,
                                        new Color(1f, 0.2f, 0.2f));
            aoEstourar.Invoke(setor);
        }
    }

    // Para testar sem esperar (clique com o botão direito no título do script no Inspector)
    [ContextMenu("Forçar um problema agora")]
    public void ForcarProblema()
    {
        SetorFabrica escolhido = Sortear();
        if (escolhido != null) Disparar(escolhido);
    }

    public void ForcarProblema(SetorFabrica setor)
    {
        if (setor != null) Disparar(setor);
    }

    public void Ativar(bool valor)
    {
        ativo = valor;
    }
}