using UnityEngine;
using Core.Interaction;

namespace Core.Items
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class ToolItem : MonoBehaviour, IWorldInteractable, IHeldItem
    {
        [Header("Item Data")]
        [SerializeField] private string itemName = "Chave Inglesa";
        [SerializeField] private float itemWeight = 1.2f;
        public Sprite itemIcon;

        private Collider2D _worldCollider;
        private InteractionManager _currentOwner;

        public string InteractionPrompt => $"Pegar {itemName}";
        public float Weight => itemWeight;

        private void Awake()
        {
            _worldCollider = GetComponent<Collider2D>();
            _worldCollider.isTrigger = true;
        }

        public void OnInteract(GameObject interactor)
        {
            _currentOwner = interactor.GetComponent<InteractionManager>();
            if (_currentOwner == null) return;

            _worldCollider.enabled = false;
            _currentOwner.EquipItemInHands(gameObject);
        }

        public void OnUse() => Debug.Log($"INTERAÇÃO: Usando {itemName}.");
        public void OnInspect() => Debug.Log($"INTERAÇÃO: Inspecionando {itemName}.");
        public void OnStore() => Debug.Log($"INTERAÇÃO: {itemName} arquivado.");

        public void OnDrop()
        {
            _worldCollider.enabled = true;
            transform.SetParent(null);
            transform.position = _currentOwner.transform.position;
            _currentOwner = null;
        }
    }
}