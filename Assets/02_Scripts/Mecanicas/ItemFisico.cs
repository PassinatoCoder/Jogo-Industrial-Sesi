using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ItemFisico : MonoBehaviour
{
    [Tooltip("Arraste o arquivo ScriptableObject do item aqui")]
    public ItemData dadosDoItem;

    private void Awake()
    {
        // Garante que o colisor seja um sensor invisível pra ela poder parar em cima e pegar
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    // O PlayerInteracao vai chamar isso quando apertar 'Y'
    public void Inspecionar()
    {
        if (dadosDoItem == null) return;

        if (dadosDoItem.jaAprendeu)
        {
            Debug.Log($"Raquel: Isso é um {dadosDoItem.nomeConhecido}. {dadosDoItem.descricaoConhecida}");
            // Depois o Marcos liga isso num Pop-up de UI bonito!
        }
        else
        {
            Debug.Log($"Raquel: O que é isso? {dadosDoItem.nomeDesconhecido}. {dadosDoItem.descricaoDesconhecida}");
        }
    }

    // O PlayerInteracao vai chamar isso quando apertar 'E' ou 'G'
    public ItemData PegarItem()
    {
        gameObject.SetActive(false); // Some do chão
        return dadosDoItem; // Manda os dados pro inventário da Raquel
    }
}