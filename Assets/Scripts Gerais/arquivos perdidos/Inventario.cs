using System.Collections.Generic;
using UnityEngine;

public class Inventario : MonoBehaviour
{
    [Header("Aparência (canto superior direito)")]
    public float tamanhoSlot = 0.9f;
    public float espaco = 0.15f;
    public float margem = 0.3f;
    public float velocidade = 10f;
    public int ordemDesenho = 1000;

    private class Dado
    {
        public string nome;
        public Sprite sprite;
        public Color cor;
        public float maiorLado;
        public int dono; // 0 = sem dono (ex.: item do armário); senão, o InstanceID do objeto que o representa
    }

    // Itens coletados: guardados entre cenas, mesmo nas que não têm o objeto Inventario
    private static readonly List<Dado> itens = new List<Dado>();

    // Inventário da cena atual (null em cenas sem o objeto Inventario)
    private static Inventario atual;

    // Objetos visuais desta cena (mesmo índice de 'itens'; pode ter null)
    private readonly List<Transform> slots = new List<Transform>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstatico()
    {
        itens.Clear();
        atual = null;
    }

    private void Awake()
    {
        if (atual != null && atual != this)
        {
            Debug.LogWarning("Já existe um Inventario nesta cena. Este será desativado.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (!enabled) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Mostra o que já foi coletado em cenas anteriores, direto no canto
        for (int i = 0; i < itens.Count; i++)
        {
            Transform slot = CriarSlot(itens[i], cam);
            slot.localPosition = PosicaoDoSlot(i, cam);
            slot.localScale = Vector3.one * EscalaAlvo(itens[i]);
        }

        atual = this;
    }

    private void OnDestroy()
    {
        if (atual == this) atual = null;
    }

    // ---------------------------------------------------------------- API (estática)

    /// <summary>
    /// Guarda o item. Se a cena tem um Inventario, ele voa da posição do mundo até o canto.
    /// 'dono' (opcional): InstanceID do objeto que representa o item, para poder removê-lo depois.
    /// </summary>
    public static void Adicionar(string nome, Sprite sprite, Color cor, Vector3 posicaoMundo, Vector3 escalaMundo, int dono = 0)
    {
        if (sprite == null) return;

        Dado dado = new Dado();
        dado.nome = nome;
        dado.sprite = sprite;
        dado.cor = cor;
        dado.maiorLado = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        dado.dono = dono;
        itens.Add(dado);

        if (atual == null) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            atual.slots.Add(null); // mantém o alinhamento com a lista de itens
            return;
        }

        Transform slot = atual.CriarSlot(dado, cam);
        slot.position = posicaoMundo;
        slot.localScale = escalaMundo;
    }

    /// <summary>Remove o item que foi adicionado com esse 'dono'.</summary>
    public static bool Remover(int dono)
    {
        if (dono == 0) return false;

        for (int i = 0; i < itens.Count; i++)
        {
            if (itens[i].dono == dono)
            {
                RemoverIndice(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>Remove um item pelo nome (ex.: ao usar uma chave numa porta).</summary>
    public static bool Remover(string nome)
    {
        for (int i = 0; i < itens.Count; i++)
        {
            if (itens[i].nome == nome)
            {
                RemoverIndice(i);
                return true;
            }
        }
        return false;
    }

    public static bool Tem(string nome)
    {
        foreach (Dado d in itens)
        {
            if (d.nome == nome) return true;
        }
        return false;
    }

    public static int Quantidade
    {
        get { return itens.Count; }
    }

    /// <summary>Esvazia o inventário (use em "novo jogo").</summary>
    public static void Limpar()
    {
        itens.Clear();

        if (atual != null)
        {
            foreach (Transform s in atual.slots)
            {
                if (s != null) Destroy(s.gameObject);
            }
            atual.slots.Clear();
        }
    }

    private static void RemoverIndice(int i)
    {
        itens.RemoveAt(i);

        if (atual != null && i < atual.slots.Count)
        {
            if (atual.slots[i] != null) Destroy(atual.slots[i].gameObject);
            atual.slots.RemoveAt(i);
        }
    }

    // ---------------------------------------------------------------- visual

    private float EscalaAlvo(Dado dado)
    {
        return tamanhoSlot / Mathf.Max(dado.maiorLado, 0.0001f);
    }

    private Transform CriarSlot(Dado dado, Camera cam)
    {
        GameObject go = new GameObject("Slot_" + dado.nome);
        go.transform.SetParent(cam.transform, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = dado.sprite;
        sr.color = dado.cor;
        sr.sortingLayerID = SortingLayer.layers[SortingLayer.layers.Length - 1].id;
        sr.sortingOrder = ordemDesenho;

        slots.Add(go.transform);
        return go.transform;
    }

    private Vector3 PosicaoDoSlot(int i, Camera cam)
    {
        float metadeAltura = cam.orthographicSize;
        float metadeLargura = metadeAltura * cam.aspect;

        // Canto superior direito, crescendo para a esquerda
        return new Vector3(
            metadeLargura - margem - tamanhoSlot / 2f - i * (tamanhoSlot + espaco),
            metadeAltura - margem - tamanhoSlot / 2f,
            10f);
    }

    private void Update()
    {
        if (slots.Count == 0) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        float suavizar = 1f - Mathf.Exp(-velocidade * Time.unscaledDeltaTime);

        for (int i = 0; i < slots.Count && i < itens.Count; i++)
        {
            if (slots[i] == null) continue;

            slots[i].localPosition = Vector3.Lerp(slots[i].localPosition, PosicaoDoSlot(i, cam), suavizar);
            slots[i].localScale = Vector3.Lerp(slots[i].localScale, Vector3.one * EscalaAlvo(itens[i]), suavizar);
        }
    }
}