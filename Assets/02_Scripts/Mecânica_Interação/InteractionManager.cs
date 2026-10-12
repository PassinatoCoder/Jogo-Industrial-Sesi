using UnityEngine;
using System;
using Core.Interaction;

namespace Core.Player
{
    [RequireComponent(typeof(Core.Inventory.InventoryManager))]
    public class InteractionManager : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private Transform detectionPoint;
        [SerializeField] private float detectionRadius = 1.5f;
        [SerializeField] private LayerMask interactableLayer;

        // Estado do Mundo
        private IWorldInteractable _nearbyInteractable;

        // Estado do Jogador (Mãos)
        private IHeldItem _currentHeldItem;
        private GameObject _heldItemModel;

        // Dependências de Sistema
        private Core.Inventory.InventoryManager _inventory;

        // Controle de Input (GDD: Tap vs Hold)
        private float _holdG_Timer = 0f;
        private const float HOLD_DURATION = 0.4f;
        private bool _isHoldingG = false;

        // Eventos para a UI escutar (State-Driven UI)
        public event Action<string> OnWorldPromptChanged;
        public event Action<bool> OnHeldOptionsToggled;
        public event Action OnInventoryToggled;

        private void Awake()
        {
            _inventory = GetComponent<Core.Inventory.InventoryManager>();
        }

        private void Update()
        {
            ScanEnvironment();
            ProcessInputs();
        }

        private void ScanEnvironment()
        {
            if (_currentHeldItem != null) return;

            Collider2D col = Physics2D.OverlapCircle(detectionPoint.position, detectionRadius, interactableLayer);

            if (col != null)
            {
                IWorldInteractable interactable = col.GetComponent<IWorldInteractable>();
                if (interactable != null && _nearbyInteractable != interactable)
                {
                    _nearbyInteractable = interactable;
                    OnWorldPromptChanged?.Invoke(_nearbyInteractable.InteractionPrompt);
                }
            }
            else if (_nearbyInteractable != null)
            {
                ClearWorldPrompt();
            }
        }

        private void ProcessInputs()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                OnInventoryToggled?.Invoke();
            }

            if (_currentHeldItem != null)
            {
                HandleHeldItemInputs();
            }
            else
            {
                HandleWorldInputs();
            }
        }

        private void HandleWorldInputs()
        {
            // Tecla E: Pegar objeto ou falar com NPC
            if (Input.GetKeyDown(KeyCode.E) && _nearbyInteractable != null)
            {
                _nearbyInteractable.OnInteract(this.gameObject);
            }
        }

        private void HandleHeldItemInputs()
        {
            if (Input.GetKeyDown(KeyCode.F)) _currentHeldItem.OnUse();
            else if (Input.GetKeyDown(KeyCode.Y)) _currentHeldItem.OnInspect();
            else if (Input.GetKeyDown(KeyCode.E))
            {
                _currentHeldItem.OnDrop();
                ClearHeldItem();
            }

            // Correção GDD: Clique Rápido (Cinto) vs Segurar (Bolsa)
            if (Input.GetKeyDown(KeyCode.G))
            {
                _isHoldingG = true;
                _holdG_Timer = 0f;
            }

            if (Input.GetKey(KeyCode.G) && _isHoldingG)
            {
                _holdG_Timer += Time.deltaTime;

                // Se estourou o tempo, guarda na BOLSA automaticamente
                if (_holdG_Timer >= HOLD_DURATION)
                {
                    _isHoldingG = false;
                    ProcessItemStorage(isLongPress: true);
                }
            }

            if (Input.GetKeyUp(KeyCode.G) && _isHoldingG)
            {
                // Se soltou antes de estourar o tempo, guarda no CINTO
                _isHoldingG = false;
                ProcessItemStorage(isLongPress: false);
            }
        }

        private void ProcessItemStorage(bool isLongPress)
        {
            bool storageSuccess = false;

            string itemName = _heldItemModel.name;
            float itemWeight = 2.0f;

            if (isLongPress)
            {
                storageSuccess = _inventory.TryStoreInBag(itemName, itemWeight);
            }
            else
            {
                storageSuccess = _inventory.TryStoreInBelt(itemName);
            }

            if (storageSuccess)
            {
                _currentHeldItem.OnStore();
                ClearHeldItem();
            }
        }

        private void ProcessItemStorage()
        {
            bool storageSuccess = false;

            // Variáveis provisórias até implementarmos o ScriptableObject do Item
            string itemName = _heldItemModel.name;
            float itemWeight = 2.0f;

            if (_holdG_Timer >= HOLD_DURATION)
            {
                storageSuccess = _inventory.TryStoreInBag(itemName, itemWeight);
            }
            else
            {
                storageSuccess = _inventory.TryStoreInBelt(itemName);
            }

            if (storageSuccess)
            {
                _currentHeldItem.OnStore();
                ClearHeldItem();
            }

            _holdG_Timer = 0f;
        }

        // --- API PÚBLICA PARA OS OBJETOS CHAMAREM ---

        public void EquipItemInHands(GameObject itemObject)
        {
            IHeldItem heldComponent = itemObject.GetComponent<IHeldItem>();
            if (heldComponent == null) return;

            _currentHeldItem = heldComponent;
            _heldItemModel = itemObject;

            ClearWorldPrompt();
            OnHeldOptionsToggled?.Invoke(true);
        }

        private void ClearHeldItem()
        {
            _currentHeldItem = null;
            _heldItemModel = null;
            _isHoldingG = false; // Reseta o estado do botão caso o item suma enquanto seguramos
            OnHeldOptionsToggled?.Invoke(false);
        }

        private void ClearWorldPrompt()
        {
            _nearbyInteractable = null;
            OnWorldPromptChanged?.Invoke(null);
        }

        private void OnDrawGizmosSelected()
        {
            if (detectionPoint == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(detectionPoint.position, detectionRadius);
        }
    }
}