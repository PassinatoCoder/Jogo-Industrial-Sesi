using UnityEngine;

namespace Core.Accessibility
{
    /// <summary>
    /// Coloque num objeto (ex.: a raiz do menu principal) pra que TODOS os textos dentro dele
    /// ignorem tamanho, fonte, espaçamento e negrito da acessibilidade.
    /// Use em textos desenhados pra caber num espaço exato (botões do menu, logotipos, títulos grandes).
    /// O menu de opções já se marca sozinho.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessibility Ignore")]
    public sealed class AccessibilityIgnore : MonoBehaviour
    {
    }
}
