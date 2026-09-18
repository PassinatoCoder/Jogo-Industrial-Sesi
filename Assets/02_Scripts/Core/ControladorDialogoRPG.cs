using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[System.Serializable]
public class LinhaDialogo
{
    public enum AtorFalante { EsquerdaProtagonista, DireitaNPC }

    [Header("Configuração do Ator")]
    public AtorFalante quemFala;
    public string nomePersonagem;

    [Header("Sprites (Expressões)")]
    public Sprite spriteEsquerda; // Sprite da protagonista nesta fala
    public Sprite spriteDireita;  // Sprite do NPC nesta fala

    [Header("Fala")]
    [TextArea(2, 4)] public string texto;
}

public class ControladorDialogoRPG : MonoBehaviour
{
    public static ControladorDialogoRPG Instancia { get; private set; }

    [Header("UI - Atores")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private RectTransform atorEsquerda;
    [SerializeField] private RectTransform atorDireita;
    [SerializeField] private Image imagemEsquerda;
    [SerializeField] private Image imagemDireita;

    [Header("UI - Textos")]
    [SerializeField] private TextMeshProUGUI textoNome;
    [SerializeField] private TextMeshProUGUI textoDialogo;

    [Header("Configurações")]
    [SerializeField] private float velocidadeDigitacao = 0.03f;
    [SerializeField] private Color corFoco = Color.white; // 100% visível
    [SerializeField] private Color corSombra = new Color(0.4f, 0.4f, 0.4f, 0.8f); // Escurecido e transparente

    private LinhaDialogo[] sequenciaAtual;
    private int indiceAtual;
    private UnityAction aoTerminar;
    private Coroutine rotinaDigitacao;
    private bool dialogoRolando = false;

    private void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);

        painelPrincipal.SetActive(false);
    }

    private void Update()
    {
        if (!dialogoRolando) return;

        bool interagiu = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                         (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) ||
                         (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

        if (interagiu) AvancarDialogo();
    }

    public void IniciarSequencia(LinhaDialogo[] linhas, UnityAction callback)
    {
        sequenciaAtual = linhas;
        aoTerminar = callback;
        indiceAtual = 0;
        dialogoRolando = true;

        Time.timeScale = 0f; // Congela o jogo
        painelPrincipal.SetActive(true);
        MostrarLinhaAtual();
    }

    private void MostrarLinhaAtual()
    {
        LinhaDialogo linha = sequenciaAtual[indiceAtual];
        textoNome.text = linha.nomePersonagem;

        // Atualiza os Sprites
        if (linha.spriteEsquerda != null) { imagemEsquerda.sprite = linha.spriteEsquerda; imagemEsquerda.gameObject.SetActive(true); }
        else imagemEsquerda.gameObject.SetActive(false);

        if (linha.spriteDireita != null) { imagemDireita.sprite = linha.spriteDireita; imagemDireita.gameObject.SetActive(true); }
        else imagemDireita.gameObject.SetActive(false);

        // Aplica o Game Feel (Foco, Cor e Pulinho)
        if (linha.quemFala == LinhaDialogo.AtorFalante.EsquerdaProtagonista)
        {
            AplicarFoco(imagemEsquerda, atorEsquerda, true);
            AplicarFoco(imagemDireita, atorDireita, false);
        }
        else
        {
            AplicarFoco(imagemDireita, atorDireita, true);
            AplicarFoco(imagemEsquerda, atorEsquerda, false);
        }

        if (rotinaDigitacao != null) StopCoroutine(rotinaDigitacao);
        rotinaDigitacao = StartCoroutine(DigitarTexto(linha.texto));
    }

    private void AplicarFoco(Image imagem, RectTransform transformAtor, bool temFoco)
    {
        if (!imagem.gameObject.activeSelf) return;

        imagem.color = temFoco ? corFoco : corSombra;
        transformAtor.localScale = temFoco ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;

        if (temFoco) StartCoroutine(EfeitoPulinho(transformAtor));
    }

    private IEnumerator EfeitoPulinho(RectTransform ator)
    {
        // Animação matemática simples rodando em tempo real (pois Time.timeScale é 0)
        float tempo = 0;
        Vector2 posOriginal = ator.anchoredPosition;
        while (tempo < 0.15f)
        {
            tempo += Time.unscaledDeltaTime;
            ator.anchoredPosition = posOriginal + new Vector2(0, Mathf.Sin(tempo * Mathf.PI / 0.15f) * 15f);
            yield return null;
        }
        ator.anchoredPosition = posOriginal;
    }

    private IEnumerator DigitarTexto(string texto)
    {
        textoDialogo.text = "";
        foreach (char c in texto)
        {
            textoDialogo.text += c;
            yield return new WaitForSecondsRealtime(velocidadeDigitacao);
        }
        rotinaDigitacao = null;
    }

    private void AvancarDialogo()
    {
        if (rotinaDigitacao != null)
        {
            StopCoroutine(rotinaDigitacao);
            rotinaDigitacao = null;
            textoDialogo.text = sequenciaAtual[indiceAtual].texto;
            return;
        }

        indiceAtual++;
        if (indiceAtual < sequenciaAtual.Length) MostrarLinhaAtual();
        else EncerrarDialogo();
    }

    private void EncerrarDialogo()
    {
        dialogoRolando = false;
        painelPrincipal.SetActive(false);
        Time.timeScale = 1f; // Descongela

        aoTerminar?.Invoke();
        aoTerminar = null;
    }
}