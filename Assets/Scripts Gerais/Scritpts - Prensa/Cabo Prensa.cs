using UnityEngine;

/// <summary>
/// Controla um único cabo: sua cordinha (LineRenderer), seu estado
/// (conectado/desconectado) e o feedback visual de cor. Versão 2D.
/// </summary>
public class CaboPrensa : MonoBehaviour
{
    [Header("Referências")]
    public Transform PontoFixo;      // onde a cordinha nasce (na máquina)
    public Transform Ponta;          // objeto que o jogador arrasta
    public Transform Receptor;       // onde a ponta deve encaixar
    public LineRenderer Fio;

    [Header("Posições possíveis quando desconectado (mesmo Z da cena)")]
    public Vector3[] PosicoesDesconectado;

    [Header("Feedback visual")]
    public Color CorConectado = new Color(0.2f, 1f, 0.3f);
    public Color CorDesconectado = new Color(1f, 0.2f, 0.2f);

    [Tooltip("Opcional: um SpriteRenderer no receptor (ex: um 'aro' ou luz) que muda de cor junto")]
    public SpriteRenderer IndicadorReceptor;

    private bool Conectado;
    public bool EstaConectado => Conectado;

    void Awake()
    {
        Fio.positionCount = 2;

        // garante que a linha fique no plano 2D (Z=0) mesmo se os
        // transforms tiverem alguma leve diferença de profundidade
        Fio.useWorldSpace = true;
    }

    void Update()
    {
        AtualizarFio();
    }

    void AtualizarFio()
    {
        Fio.SetPosition(0, PontoFixo.position);
        Fio.SetPosition(1, Ponta.position);
    }

    /// <summary>
    /// Chamado pelo CaboManager no início da partida para sortear
    /// se este cabo começa conectado ou não.
    /// </summary>
    public void DefinirEstadoInicial(bool conectado)
    {
        Conectado = conectado;

        if (conectado)
        {
            Ponta.position = Receptor.position;
        }
        else if (PosicoesDesconectado != null && PosicoesDesconectado.Length > 0)
        {
            int indice = Random.Range(0, PosicoesDesconectado.Length);
            Ponta.position = PosicoesDesconectado[indice];
        }

        AtualizarVisual();
    }

    public void Conectar()
    {
        if (Conectado) return;

        Conectado = true;
        AtualizarVisual();

        if (CaboManager.Instancia != null)
            CaboManager.Instancia.NotificarConexao();
    }

    /// <summary>Útil se quiser permitir desconectar de novo (ex: sabotagem aleatória contínua).</summary>
    public void Desconectar()
    {
        if (!Conectado) return;

        Conectado = false;
        AtualizarVisual();
    }

    void AtualizarVisual()
    {
        Color cor = Conectado ? CorConectado : CorDesconectado;

        Fio.startColor = cor;
        Fio.endColor = cor;

        if (IndicadorReceptor != null)
            IndicadorReceptor.color = cor;
    }
}