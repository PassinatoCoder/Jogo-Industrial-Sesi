using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Botão clicável na cena (Collider2D, mesmo estilo dos outros botões).
/// Só funciona depois que o contador atingir a meta de placas.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BotaoSairMinigame : MonoBehaviour
{
    [Header("Referências")]
    public ContadorPlacas Contador;

    [Header("Cena")]
    [Tooltip("Nome exato da cena de destino (ela precisa estar no Build Settings).")]
    public string NomeCenaDestino;

    [Header("Feedback visual")]
    [Tooltip("Opcional: SpriteRenderer do botão, pra indicar se já pode ser clicado.")]
    public SpriteRenderer Sprite;
    public Color CorLiberado = new Color(0.2f, 1f, 0.3f);
    public Color CorBloqueado = new Color(0.4f, 0.4f, 0.4f);

    void Update()
    {
        if (Sprite == null || Contador == null)
            return;

        Sprite.color = Contador.MetaAtingida ? CorLiberado : CorBloqueado;
    }

    void OnMouseDown()
    {
        if (Contador == null || !Contador.MetaAtingida)
            return; // ainda não bateu a meta

        SceneManager.LoadScene(NomeCenaDestino);
    }
}