using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Armario : MonoBehaviour
{
    // Outros scripts podem usar isso para travar o movimento: if (Armario.Aberto) return;
    public static bool Aberto { get; private set; }

    [Header("Obrigatório")]
    [Tooltip("Versão grande do armário (começa desativada). Filhos 'Porta...' e 'Item...' são detectados sozinhos.")]
    public GameObject armarioGrande;

    [Header("Opcional (se vazio, procura filhos pelo nome)")]
    public List<Transform> portas = new List<Transform>();
    public List<Transform> itens = new List<Transform>();

    [Header("Interação")]
    public KeyCode teclaInteracao = KeyCode.E;
    public float distanciaInteracao = 3f;

    [Header("Visual")]
    public float duracaoPortas = 0.8f;
    [Tooltip("Tempo das portas fechando de novo antes do armário sumir")]
    public float duracaoFechar = 0.6f;
    [Tooltip("1 = a porta desliza a própria largura; menos que 1 = fica parcialmente sobreposta ao armário")]
    public float fatorAbertura = 0.9f;
    public float escalaHover = 1.12f;
    [Range(0f, 1f)] public float escurecerFundo = 0.65f;
    public int ordemBase = 500;

    private Camera cam;
    private Transform jogador;
    private Transform raizGrande;
    private Vector3 escalaGrande;
    private GameObject fundoEscuro;
    private SpriteRenderer fundoSR;

    private readonly List<Vector3> portasFechadas = new List<Vector3>();
    private readonly List<Vector3> portasDeslocamento = new List<Vector3>();
    private readonly List<Vector3> itensEscalaOriginal = new List<Vector3>();

    private bool aberto;
    private bool ocupado;

    private void Awake()
    {
        cam = Camera.main;

        if (armarioGrande == null)
        {
            Debug.LogError("Armario: arraste o ArmarioGrande no campo 'Armario Grande'.", this);
            enabled = false;
            return;
        }

        raizGrande = armarioGrande.transform;
        escalaGrande = raizGrande.localScale;

        if (portas.Count == 0) portas = BuscarFilhos(raizGrande, "porta");
        if (itens.Count == 0) itens = BuscarFilhos(raizGrande, "item");

        PrepararPortas();
        PrepararItens();
        PrepararOrdemDeDesenho();
        CriarFundoEscuro();

        armarioGrande.SetActive(false);
    }

    private void OnDisable()
    {
        if (aberto) Aberto = false;
    }

    // ---------------------------------------------------------------- preparação

    private List<Transform> BuscarFilhos(Transform raiz, string prefixo)
    {
        List<Transform> lista = new List<Transform>();
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t != raiz && t.name.ToLower().StartsWith(prefixo))
                lista.Add(t);
        }
        return lista;
    }

    private void PrepararPortas()
    {
        foreach (Transform p in portas)
        {
            portasFechadas.Add(p.localPosition);

            // Lado: esquerda ou direita do centro do armário grande
            float lado = raizGrande.InverseTransformPoint(p.position).x < 0f ? -1f : 1f;

            SpriteRenderer sr = p.GetComponent<SpriteRenderer>();
            float largura = (sr != null && sr.sprite != null)
                ? sr.sprite.bounds.size.x * Mathf.Abs(p.lossyScale.x)
                : 1f;
            float escalaPai = p.parent != null ? Mathf.Abs(p.parent.lossyScale.x) : 1f;

            portasDeslocamento.Add(new Vector3(lado * (largura / escalaPai) * fatorAbertura, 0f, 0f));
        }
    }

    private void PrepararItens()
    {
        foreach (Transform it in itens)
        {
            itensEscalaOriginal.Add(it.localScale);

            // Cria o colisor automaticamente se o item não tiver um
            if (it.GetComponent<Collider2D>() == null)
            {
                BoxCollider2D col = it.gameObject.AddComponent<BoxCollider2D>();
                SpriteRenderer sr = it.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    col.size = sr.sprite.bounds.size;
                    col.offset = sr.sprite.bounds.center;
                }
            }
        }
    }

    private bool EstaDentro(Transform t, List<Transform> grupo)
    {
        foreach (Transform g in grupo)
        {
            if (t == g || t.IsChildOf(g)) return true;
        }
        return false;
    }

    private void PrepararOrdemDeDesenho()
    {
        int camadaTopo = SortingLayer.layers[SortingLayer.layers.Length - 1].id;

        foreach (SpriteRenderer sr in raizGrande.GetComponentsInChildren<SpriteRenderer>(true))
        {
            int nivel = 1;                                  // corpo do armário
            if (EstaDentro(sr.transform, itens)) nivel = 2; // itens
            if (EstaDentro(sr.transform, portas)) nivel = 3; // portas (na frente)

            sr.sortingLayerID = camadaTopo;
            sr.sortingOrder = ordemBase + nivel;
        }
    }

    private void CriarFundoEscuro()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite spr = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        fundoEscuro = new GameObject("FundoEscuroArmario");
        fundoSR = fundoEscuro.AddComponent<SpriteRenderer>();
        fundoSR.sprite = spr;
        fundoSR.color = new Color(0f, 0f, 0f, 0f);
        fundoSR.sortingLayerID = SortingLayer.layers[SortingLayer.layers.Length - 1].id;
        fundoSR.sortingOrder = ordemBase;
        fundoEscuro.SetActive(false);
    }

    // ---------------------------------------------------------------- loop

    private void Update()
    {
        if (ocupado) return;

        if (!aberto)
        {
            if (!Aberto && Input.GetKeyDown(teclaInteracao) && JogadorPerto())
                Interagir();
            return;
        }

        if (Input.GetKeyDown(teclaInteracao) || Input.GetKeyDown(KeyCode.Escape))
        {
            Interagir();
            return;
        }

        TratarItens();
    }

    /// <summary>Pode ser chamado também por outro sistema de interação (botão, etc.).</summary>
    public void Interagir()
    {
        if (ocupado) return;
        StartCoroutine(aberto ? FecharRotina() : AbrirRotina());
    }

    private bool JogadorPerto()
    {
        if (jogador == null)
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go != null) jogador = go.transform;
        }

        if (jogador == null) return true; // sem Player na cena, não bloqueia
        return Vector2.Distance(jogador.position, transform.position) <= distanciaInteracao;
    }

    // ---------------------------------------------------------------- itens

    private void TratarItens()
    {
        Vector2 mouse = cam.ScreenToWorldPoint(Input.mousePosition);

        int hover = -1;
        for (int i = itens.Count - 1; i >= 0; i--)
        {
            if (!itens[i].gameObject.activeSelf) continue;

            Collider2D col = itens[i].GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(mouse))
            {
                hover = i;
                break;
            }
        }

        // Destaque suave do item sob o mouse
        for (int i = 0; i < itens.Count; i++)
        {
            if (!itens[i].gameObject.activeSelf) continue;

            Vector3 alvo = (i == hover) ? itensEscalaOriginal[i] * escalaHover : itensEscalaOriginal[i];
            itens[i].localScale = Vector3.Lerp(itens[i].localScale, alvo, 15f * Time.unscaledDeltaTime);
        }

        if (hover >= 0 && Input.GetMouseButtonDown(0))
            Coletar(hover);
    }

    private void Coletar(int i)
    {
        Transform it = itens[i];
        SpriteRenderer sr = it.GetComponent<SpriteRenderer>();

        if (sr != null && sr.sprite != null)
            Inventario.Adicionar(it.name, sr.sprite, sr.color, it.position, it.lossyScale);

        it.localScale = itensEscalaOriginal[i];
        it.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- abrir / fechar

    private IEnumerator AbrirRotina()
    {
        ocupado = true;
        aberto = true;
        Aberto = true;

        Transform camT = cam.transform;
        float metadeAltura = cam.orthographicSize;
        float metadeLargura = metadeAltura * cam.aspect;

        // Fundo escuro cobrindo a tela inteira
        fundoEscuro.transform.SetParent(camT, false);
        fundoEscuro.transform.localPosition = new Vector3(0f, 0f, 10f);
        fundoEscuro.transform.localScale = new Vector3(metadeLargura * 2f + 1f, metadeAltura * 2f + 1f, 1f);
        fundoEscuro.SetActive(true);

        // Armário grande no centro da tela (preso à câmera)
        raizGrande.SetParent(camT, false);
        raizGrande.localPosition = new Vector3(0f, 0f, 10f);
        raizGrande.localRotation = Quaternion.identity;
        raizGrande.localScale = Vector3.zero;

        for (int i = 0; i < portas.Count; i++)
            portas[i].localPosition = portasFechadas[i];

        armarioGrande.SetActive(true);

        // 1) Armário "aparece" crescendo
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.25f;
            float s = Mathf.SmoothStep(0f, 1f, t);
            raizGrande.localScale = escalaGrande * s;
            fundoSR.color = new Color(0f, 0f, 0f, escurecerFundo * s);
            yield return null;
        }
        raizGrande.localScale = escalaGrande;

        // 2) Portas deslizam para os lados
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracaoPortas;
            float s = Mathf.SmoothStep(0f, 1f, t);
            for (int i = 0; i < portas.Count; i++)
                portas[i].localPosition = portasFechadas[i] + portasDeslocamento[i] * s;
            yield return null;
        }

        ocupado = false;
    }

    private IEnumerator FecharRotina()
    {
        ocupado = true;

        // Garante que nenhum item fique "inchado" do destaque do mouse
        for (int i = 0; i < itens.Count; i++)
            itens[i].localScale = itensEscalaOriginal[i];

        // 1) Portas fecham de novo
        Vector3[] inicio = new Vector3[portas.Count];
        for (int i = 0; i < portas.Count; i++)
            inicio[i] = portas[i].localPosition;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(duracaoFechar, 0.01f);
            float s = Mathf.SmoothStep(0f, 1f, t);
            for (int i = 0; i < portas.Count; i++)
                portas[i].localPosition = Vector3.Lerp(inicio[i], portasFechadas[i], s);
            yield return null;
        }

        for (int i = 0; i < portas.Count; i++)
            portas[i].localPosition = portasFechadas[i];

        // 2) Só então o armário some
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.2f;
            float s = 1f - Mathf.SmoothStep(0f, 1f, t);
            raizGrande.localScale = escalaGrande * s;
            fundoSR.color = new Color(0f, 0f, 0f, escurecerFundo * s);
            yield return null;
        }

        armarioGrande.SetActive(false);
        fundoEscuro.SetActive(false);

        aberto = false;
        Aberto = false;
        ocupado = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaInteracao);
    }
}