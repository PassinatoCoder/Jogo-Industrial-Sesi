using UnityEngine;
using System;
using System.Collections.Generic;

namespace Core.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        [Header("Tool Belt (Cinto)")]
        public int maxBeltSlots = 3;
        private GameObject[] _toolBelt;

        [Header("Bag (Bolsa Pequena)")]
        public int maxBagSlots = 6;
        public float maxBagWeight = 20f;

        [SerializeField] private float _currentWeight = 0f;
        private List<GameObject> _bag = new List<GameObject>();

        public bool HasToolBelt { get; private set; } = false;
        public bool HasBag { get; private set; } = false;
        public bool isBagTorn { get; private set; } = false;

        public event Action OnInventoryUpdated;
        public event Action OnBagTorn;

        public GameObject[] ToolBelt => _toolBelt;
        public IReadOnlyList<GameObject> Bag => _bag;
        public float CurrentWeight => _currentWeight;

        private void Awake() => _toolBelt = new GameObject[maxBeltSlots];

        public void EnableToolBelt() => HasToolBelt = true;
        public void EnableBag() => HasBag = true;

        public bool TryStoreInBelt(GameObject itemObj)
        {
            if (!HasToolBelt) return false;
            for (int i = 0; i < _toolBelt.Length; i++)
            {
                if (_toolBelt[i] == null)
                {
                    _toolBelt[i] = itemObj;
                    itemObj.SetActive(false);
                    OnInventoryUpdated?.Invoke();
                    return true;
                }
            }
            return false;
        }

        public bool TryStoreInBag(GameObject itemObj, float weight)
        {
            if (!HasBag || isBagTorn || _bag.Count >= maxBagSlots) return false;
            _bag.Add(itemObj);
            _currentWeight += weight;
            itemObj.SetActive(false);
            if (_currentWeight > maxBagWeight) { isBagTorn = true; OnBagTorn?.Invoke(); }
            OnInventoryUpdated?.Invoke();
            return true;
        }

        public GameObject RetrieveFromBelt(int slotIndex)
        {
            if (!HasToolBelt || slotIndex < 0 || slotIndex >= _toolBelt.Length || _toolBelt[slotIndex] == null) return null;
            GameObject item = _toolBelt[slotIndex];
            _toolBelt[slotIndex] = null;
            OnInventoryUpdated?.Invoke();
            return item;
        }

        public GameObject RetrieveFromBag(int slotIndex)
        {
            if (!HasBag || slotIndex < 0 || slotIndex >= _bag.Count || _bag[slotIndex] == null) return null;
            GameObject item = _bag[slotIndex];

            Core.Items.ToolItem tool = item.GetComponent<Core.Items.ToolItem>();
            if (tool != null) _currentWeight -= tool.Weight;

            _bag.RemoveAt(slotIndex);
            OnInventoryUpdated?.Invoke();
            return item;
        }
    }
}