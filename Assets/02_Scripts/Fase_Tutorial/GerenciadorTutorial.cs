using UnityEngine;

public class GerenciadorTutorial : MonoBehaviour
{
    // Instância global para ser acessada de qualquer lugar
    public static GerenciadorTutorial Instancia { get; private set; }

    [Header("Travas do Teclado (Começam bloqueadas)")]
    public bool podeAndar = false;
    public bool podePular = false;
    public bool podeAgachar = false;
    public bool podeInteragir = false;

    private void Awake()
    {
        // Garante que só exista um Gerenciador na fase
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
    }

    // Métodos que o Mascote vai chamar no final das falas dele para liberar o jogo
    public void LiberarMovimento() => podeAndar = true;
    public void LiberarPulo() => podePular = true;
    public void LiberarAgachamento() => podeAgachar = true;
    public void LiberarInteracao() => podeInteragir = true;
}