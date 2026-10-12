using UnityEngine;
using Core.Interaction;
using Core.Player;

namespace Core.Items
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class ToolItem : MonoBehaviour, IWorldInteractable, IHeldItem
    {
        [Header("Item Data")]
        [SerializeField] private string itemName = "Chave Inglesa";
        [SerializeField] private float itemWeight = 1.2f;

        private Collider2D _worldCollider;
        private SpriteRenderer _spriteRenderer;
        private InteractionManager _currentOwner;

        public string InteractionPrompt => $"Pegar {itemName}";

        private void Awake()
        {
            _worldCollider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            _worldCollider.isTrigger = true;
        }

        // --- IWorldInteractable (Estado: No Chão) ---
        public void OnInteract(GameObject interactor)
        {
            _currentOwner = interactor.GetComponent<InteractionManager>();
            if (_currentOwner == null) return;

            // Transição para as mãos: Desativa a presença física no mundo
            _worldCollider.enabled = false;
            _spriteRenderer.enabled = false;

            _currentOwner.EquipItemInHands(this.gameObject);
            Debug.Log($"INTERAÇÃO: {itemName} equipado. Peso adicionado: {itemWeight}kg.");
        }

        // --- IHeldItem (Estado: Nas Mãos) ---
        public void OnUse()
        {
            // GDD: Tecla [F]
            Debug.Log($"INTERAÇÃO: Usando {itemName}. (Lógica de conserto acionada)");
        }

        public void OnInspect()
        {
            // GDD: Tecla [Y]
            Debug.Log($"INTERAÇÃO: Inspecionando {itemName}. Peso: {itemWeight}kg. Status: Funcional.");
        }

        public void OnStore()
        {
            // GDD: Tecla [G]
            Debug.Log($"INTERAÇÃO: {itemName} guardado. (O Sistema de Inventário processará o peso)");

            // PROVISÓRIO: Destrói o objeto no mundo. 
            // Quando fizermos o Inventário, isso se tornará um Struct/Data Object salvo na bolsa.
            Destroy(gameObject);
        }

        public void OnDrop()
        {
            // GDD: Tecla [E] para soltar
            _worldCollider.enabled = true;
            _spriteRenderer.enabled = true;

            // Reposiciona o item no pé do jogador
            transform.position = _currentOwner.transform.position;
            _currentOwner = null;

            Debug.Log($"INTERAÇÃO: {itemName} jogado no chão.");
        }
    }
}