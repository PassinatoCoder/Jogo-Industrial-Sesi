using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ManualMinigame : MonoBehaviour
{
    [Header("Objetos do manual (GameObjects, sem Canvas)")]
    public Transform manual;          // objeto pai do manual (começa na cena, o script desativa)
    public GameObject[] paginas;      // as 2 páginas, filhas do manual
    public GameObject fundo;          // sprite escuro grande com Collider2D (fora do manual)

    [Header("Efeito")]
    public float velocidade = 10f;
    public float deslocamentoZ = -3f;  // aproxima da câmera para ficar na frente de tudo
    public int ordemExtra = 100;       // sobe a ordem de desenho enquanto aberto

    // Outros scripts consultam isso para ignorar o Enter enquanto o manual está em uso
    public static bool BloqueiaInput
    {
        get { return estaAberto || Time.frameCount == frameFechou; }
    }
    private static bool estaAberto = false;
    private static int frameFechou = -1;

    private Collider2D colIcone;
    private Collider2D[] colsManual;
    private Camera cam;

    private Vector3 escalaAberta;
    private Vector3 deslocamentoCentro;   // distância entre o pivô do manual e o centro das páginas
    private Vector3 posicaoAlvo;
    private Vector3 escalaAlvo;
    private int paginaAtual = 0;

    private SpriteRenderer[] spritesManual;
    private int[] ordensManual;
    private SpriteRenderer[] spritesFundo;
    private int[] ordensFundo;

    void Start()
    {
        estaAberto = false;
        frameFechou = -1;

        cam = Camera.main;
        colIcone = GetComponent<Collider2D>();

        // Guarda o tamanho do manual como você montou na cena
        escalaAberta = manual.localScale;
        colsManual = manual.GetComponentsInChildren<Collider2D>(true);

        // Descobre o centro real do manual pelo collider do objeto pai
        Collider2D colPrincipal = manual.GetComponent<Collider2D>();
        if (colPrincipal != null)
        {
            deslocamentoCentro = colPrincipal.bounds.center - manual.position;
            deslocamentoCentro.z = 0f;
        }
        else
        {
            deslocamentoCentro = Vector3.zero;
            Debug.LogWarning("O objeto Manual está sem Collider2D: ele pode abrir deslocado do centro da tela.");
        }

        spritesManual = manual.GetComponentsInChildren<SpriteRenderer>(true);
        ordensManual = new int[spritesManual.Length];
        for (int i = 0; i < spritesManual.Length; i++) ordensManual[i] = spritesManual[i].sortingOrder;

        if (fundo != null)
        {
            spritesFundo = fundo.GetComponentsInChildren<SpriteRenderer>(true);
            ordensFundo = new int[spritesFundo.Length];
            for (int i = 0; i < spritesFundo.Length; i++) ordensFundo[i] = spritesFundo[i].sortingOrder;
            fundo.SetActive(false);
        }

        manual.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mundo = cam.ScreenToWorldPoint(Input.mousePosition);

            if (!estaAberto)
            {
                if (colIcone.OverlapPoint(mundo)) Abrir();
            }
            else if (!ClicouNoManual(mundo))
            {
                Fechar();   // clicou fora do manual
            }
        }

        if (estaAberto)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Fechar();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                MostrarPagina(paginaAtual - 1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                MostrarPagina(paginaAtual + 1);
            }
        }

        Animar();
    }

    private void Abrir()
    {
        estaAberto = true;

        Vector3 centro = cam.transform.position;
        float zManual = transform.position.z + deslocamentoZ;

        // Se estava totalmente fechado, começa pequeno em cima do ícone
        if (!manual.gameObject.activeSelf)
        {
            manual.position = transform.position;
            manual.localScale = Vector3.zero;
            manual.gameObject.SetActive(true);
        }

        // Compensa o pivô: o centro das páginas é que fica no centro da tela
        posicaoAlvo = new Vector3(centro.x - deslocamentoCentro.x, centro.y - deslocamentoCentro.y, zManual);
        escalaAlvo = escalaAberta;

        if (fundo != null)
        {
            fundo.transform.position = new Vector3(centro.x, centro.y, zManual + 1f);
            fundo.SetActive(true);
        }

        AplicarOrdem(true);
        MostrarPagina(0);
    }

    private void Fechar()
    {
        estaAberto = false;
        frameFechou = Time.frameCount;

        // Volta encolhendo para o ícone
        posicaoAlvo = transform.position;
        escalaAlvo = Vector3.zero;

        if (fundo != null) fundo.SetActive(false);
    }

    private void Animar()
    {
        if (!manual.gameObject.activeSelf) return;

        manual.position = Vector3.Lerp(manual.position, posicaoAlvo, velocidade * Time.deltaTime);
        manual.localScale = Vector3.Lerp(manual.localScale, escalaAlvo, velocidade * Time.deltaTime);

        // Terminou de encolher: desativa e devolve a ordem de desenho
        if (!estaAberto && manual.localScale.magnitude < 0.02f)
        {
            manual.gameObject.SetActive(false);
            AplicarOrdem(false);
        }
    }

    private void MostrarPagina(int indice)
    {
        if (paginas == null || paginas.Length == 0) return;

        paginaAtual = Mathf.Clamp(indice, 0, paginas.Length - 1);

        for (int i = 0; i < paginas.Length; i++)
        {
            paginas[i].SetActive(i == paginaAtual);
        }
    }

    private bool ClicouNoManual(Vector2 mundo)
    {
        foreach (Collider2D c in colsManual)
        {
            if (c != null && c.enabled && c.OverlapPoint(mundo)) return true;
        }
        return false;
    }

    private void AplicarOrdem(bool aberto)
    {
        for (int i = 0; i < spritesManual.Length; i++)
        {
            spritesManual[i].sortingOrder = aberto ? ordensManual[i] + ordemExtra + 10 : ordensManual[i];
        }

        if (spritesFundo != null)
        {
            for (int i = 0; i < spritesFundo.Length; i++)
            {
                spritesFundo[i].sortingOrder = aberto ? ordensFundo[i] + ordemExtra : ordensFundo[i];
            }
        }
    }
}