using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[System.Serializable]
public class LinhaDialogo
{
    public enum AtorFalante { EsquerdaProtagonista, DireitaNPC }

    [Header("Configuração do Ator")]
    public AtorFalante quemFala;
    public string nomePersonagem;

    [Header("Sprites (Expressões)")]
    public Sprite spriteEsquerda;
    public Sprite spriteDireita;

    [Header("Câmera do Cutscene (opcional)")]
    public CinemachineCamera cameraDestaFala;

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
    [SerializeField] private Color corFoco = Color.white;
    [SerializeField] private Color corSombra = new Color(0.4f, 0.4f, 0.4f, 0.8f);

    [Header("Câmera (Cutscene)")]
    [SerializeField, Tooltip("A câmera que fica visível antes do diálogo começar e depois que ele termina.")]
    private CinemachineCamera cameraPadrao;

    private LinhaDialogo[] sequenciaAtual;
    private int indiceAtual;
    private UnityAction aoTerminar;
    private Coroutine rotinaDigitacao;
    private bool dialogoRolando = false;
    private CinemachineCamera cameraAtivaAtual;

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
        if (dialogoRolando) return;

        sequenciaAtual = linhas;
        aoTerminar = callback;
        indiceAtual = 0;
        dialogoRolando = true;

        if (cameraPadrao != null) cameraPadrao.gameObject.SetActive(false);

        Time.timeScale = 0f;
        painelPrincipal.SetActive(true);
        MostrarLinhaAtual();
    }

    private void MostrarLinhaAtual()
    {
        LinhaDialogo linha = sequenciaAtual[indiceAtual];
        textoNome.text = linha.nomePersonagem;

        if (linha.spriteEsquerda != null) { imagemEsquerda.sprite = linha.spriteEsquerda; imagemEsquerda.gameObject.SetActive(true); }
        else imagemEsquerda.gameObject.SetActive(false);

        if (linha.spriteDireita != null) { imagemDireita.sprite = linha.spriteDireita; imagemDireita.gameObject.SetActive(true); }
        else imagemDireita.gameObject.SetActive(false);

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

        AtualizarCameraDaFala(linha);

        if (rotinaDigitacao != null) StopCoroutine(rotinaDigitacao);
        rotinaDigitacao = StartCoroutine(DigitarTexto(linha.texto));
    }

    private void AtualizarCameraDaFala(LinhaDialogo linha)
    {
        if (linha.cameraDestaFala == null) return;
        if (linha.cameraDestaFala == cameraAtivaAtual) return;

        if (cameraAtivaAtual != null) cameraAtivaAtual.gameObject.SetActive(false);
        linha.cameraDestaFala.gameObject.SetActive(true);
        cameraAtivaAtual = linha.cameraDestaFala;
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
        Time.timeScale = 1f;

        if (cameraAtivaAtual != null)
        {
            cameraAtivaAtual.gameObject.SetActive(false);
            cameraAtivaAtual = null;
            if (cameraPadrao != null) cameraPadrao.gameObject.SetActive(true);
        }

        aoTerminar?.Invoke();
        aoTerminar = null;
    }
}