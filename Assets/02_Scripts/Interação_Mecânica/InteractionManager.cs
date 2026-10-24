using UnityEngine;
using System;

namespace Core.Interaction
{
    [RequireComponent(typeof(Core.Inventory.InventoryManager))]
    public class InteractionManager : MonoBehaviour
    {
        [Header("Player Body")]
        [SerializeField] private Transform handPoint;

        [Header("Detection Settings")]
        [SerializeField] private Transform detectionPoint;
        [SerializeField] private float detectionRadius = 1.5f;
        [SerializeField] private LayerMask interactableLayer;

        private IWorldInteractable _nearbyInteractable;
        private IHeldItem _currentHeldItem;
        private GameObject _heldItemModel;
        private Core.Inventory.InventoryManager _inventory;

        private float _holdG_Timer = 0f;
        private const float HOLD_DURATION = 0.4f;
        private bool _isHoldingG = false;

        // Controle de Estado da Bolsa
        private bool _isInventoryOpen = false;
        private int _selectedBagSlot = -1;

        public event Action<string> OnWorldPromptChanged;
        public event Action<bool> OnHeldOptionsToggled;
        public event Action OnInventoryToggled;
        public event Action<string> OnBagSlotSelected; // Novo evento para a UI

        private void Awake() => _inventory = GetComponent<Core.Inventory.InventoryManager>();

        private void Update()
        {
            ScanEnvironment();
            ProcessInputs();
        }

        private void ScanEnvironment()
        {
            if (_currentHeldItem != null || _isInventoryOpen) return;
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
            else if (_nearbyInteractable != null) ClearWorldPrompt();
        }

        private void ProcessInputs()
        {
            if (Input.GetKeyDown(KeyCode.I) && _inventory.HasBag)
            {
                _isInventoryOpen = !_isInventoryOpen;
                _selectedBagSlot = -1; // Reseta seleção ao abrir/fechar
                OnInventoryToggled?.Invoke();
            }

            if (_isInventoryOpen)
            {
                HandleBagMenuInputs(); // Se a bolsa está aberta, o teclado controla a bolsa
            }
            else
            {
                HandleHotbarInputs();
                if (_currentHeldItem != null) HandleHeldItemInputs();
                else HandleWorldInputs();
            }
        }

        private void HandleBagMenuInputs()
        {
            if (_selectedBagSlot == -1) return;

            if (Input.GetKeyDown(KeyCode.E)) EquipSelectedFromBag();
            else if (Input.GetKeyDown(KeyCode.F)) UseSelectedFromBag();
        }

        public void SelectBagSlot(int index)
        {
            if (index >= 0 && index < _inventory.Bag.Count && _inventory.Bag[index] != null)
            {
                _selectedBagSlot = index;
                OnBagSlotSelected?.Invoke(_inventory.Bag[index].name);
            }
            else
            {
                _selectedBagSlot = -1;
                OnBagSlotSelected?.Invoke("Vazio");
            }
        }

        private void EquipSelectedFromBag()
        {
            if (_currentHeldItem != null)
            {
                Debug.LogWarning("Mãos ocupadas! Guarde o item primeiro.");
                return;
            }

            GameObject retrievedItem = _inventory.RetrieveFromBag(_selectedBagSlot);
            if (retrievedItem != null)
            {
                retrievedItem.SetActive(true);
                EquipItemInHands(retrievedItem);

                // Fecha o inventário automaticamente ao equipar
                _isInventoryOpen = false;
                _selectedBagSlot = -1;
                OnInventoryToggled?.Invoke();
            }
        }

        private void UseSelectedFromBag()
        {
            GameObject itemObj = _inventory.Bag[_selectedBagSlot];
            IHeldItem held = itemObj.GetComponent<IHeldItem>();
            held?.OnUse();
        }

        private void HandleHotbarInputs()
        {
            if (_currentHeldItem != null || !_inventory.HasToolBelt) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) EquipFromBelt(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) EquipFromBelt(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) EquipFromBelt(2);
        }

        private void EquipFromBelt(int slotIndex)
        {
            GameObject retrievedItem = _inventory.RetrieveFromBelt(slotIndex);
            if (retrievedItem != null)
            {
                retrievedItem.SetActive(true);
                EquipItemInHands(retrievedItem);
            }
        }

        private void HandleWorldInputs()
        {
            if (Input.GetKeyDown(KeyCode.E) && _nearbyInteractable != null)
            {
                _nearbyInteractable.OnInteract(gameObject);
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

            if (Input.GetKeyDown(KeyCode.G))
            {
                _isHoldingG = true;
                _holdG_Timer = 0f;
            }
            if (Input.GetKey(KeyCode.G) && _isHoldingG)
            {
                _holdG_Timer += Time.deltaTime;
                if (_holdG_Timer >= HOLD_DURATION)
                {
                    _isHoldingG = false;
                    ProcessItemStorage(true);
                }
            }
            if (Input.GetKeyUp(KeyCode.G) && _isHoldingG)
            {
                _isHoldingG = false;
                ProcessItemStorage(false);
            }
        }

        private void ProcessItemStorage(bool isLongPress)
        {
            bool success = false;
            float weight = _heldItemModel.GetComponent<Core.Items.ToolItem>().Weight;

            if (isLongPress)
            {
                if (!_inventory.HasBag) return;
                success = _inventory.TryStoreInBag(_heldItemModel, weight);
            }
            else
            {
                if (!_inventory.HasToolBelt) return;
                success = _inventory.TryStoreInBelt(_heldItemModel);
            }

            if (success)
            {
                _currentHeldItem.OnStore();
                ClearHeldItem();
            }
        }

        public void EquipItemInHands(GameObject itemObject)
        {
            IHeldItem held = itemObject.GetComponent<IHeldItem>();
            if (held == null) return;

            _currentHeldItem = held;
            _heldItemModel = itemObject;

            if (handPoint != null)
            {
                itemObject.transform.SetParent(handPoint);
                itemObject.transform.localPosition = Vector3.zero;
                itemObject.transform.localRotation = Quaternion.identity;
            }

            ClearWorldPrompt();
            OnHeldOptionsToggled?.Invoke(true);
        }

        private void ClearHeldItem()
        {
            _currentHeldItem = null;
            _heldItemModel = null;
            _isHoldingG = false;
            OnHeldOptionsToggled?.Invoke(false);
        }

        private void ClearWorldPrompt()
        {
            _nearbyInteractable = null;
            OnWorldPromptChanged?.Invoke(null);
        }
    }
}