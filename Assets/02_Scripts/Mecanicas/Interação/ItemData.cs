using UnityEngine;

[CreateAssetMenu(fileName = "NovoItem", menuName = "Industria/Item Coletavel")]
public class ItemData : ScriptableObject
{
    [Header("Identificação (Sistema de Conhecimento)")]
    public string nomeDesconhecido = "Objeto Estranho de Metal";
    public string nomeConhecido = "Paquímetro Universal";

    [TextArea(2, 4)]
    public string descricaoDesconhecida = "Tem umas garras e uns números... Parece uma régua quebrada.";
    [TextArea(2, 4)]
    public string descricaoConhecida = "Mede diâmetros internos e externos com precisão de milímetros.";

    [Header("Propriedades Físicas")]
    public float peso = 1.5f; // Quanto mais pesado, mais ferra a stamina
    public Sprite iconeUI;

    [Tooltip("Ex: Uma caixa pesada não cabe no cinto, só na mochila.")]
    public bool podeIrNoCinto = true;

    [Header("Estado Atual do Jogador")]
    public bool jaAprendeu = false; // Quando ela lê o manual, isso vira TRUE
}