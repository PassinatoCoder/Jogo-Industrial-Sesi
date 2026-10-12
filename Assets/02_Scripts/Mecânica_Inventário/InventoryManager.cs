using UnityEngine;
using System;
using System.Collections.Generic;

namespace Core.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        [Header("Tool Belt (Cinto)")]
        [Tooltip("Slots iniciais: 3. Expansível até 6 no futuro.")]
        public int maxBeltSlots = 3;
        private List<string> _toolBelt = new List<string>();

        [Header("Bag (Bolsa Pequena)")]
        public int maxBagSlots = 6;
        public float maxBagWeight = 20f;

        [SerializeField] private float _currentWeight = 0f;
        private List<string> _bag = new List<string>();

        public bool isBagTorn { get; private set; } = false;

        // Eventos para a UI escutar posteriormente
        public event Action OnInventoryUpdated;
        public event Action OnBagTorn;

        public bool TryStoreInBelt(string itemName)
        {
            if (_toolBelt.Count >= maxBeltSlots)
            {
                Debug.LogWarning("INVENTÁRIO: Cinto de ferramentas cheio.");
                return false;
            }

            _toolBelt.Add(itemName);
            Debug.Log($"INVENTÁRIO: [{itemName}] adicionado ao CINTO. ({_toolBelt.Count}/{maxBeltSlots})");
            OnInventoryUpdated?.Invoke();
            return true;
        }

        public bool TryStoreInBag(string itemName, float weight)
        {
            if (isBagTorn)
            {
                Debug.LogError("INVENTÁRIO: A bolsa está rasgada! Conserte-a primeiro usando um item de remendo.");
                return false;
            }

            if (_bag.Count >= maxBagSlots)
            {
                Debug.LogWarning("INVENTÁRIO: Bolsa cheia (Slots).");
                return false;
            }

            _bag.Add(itemName);
            _currentWeight += weight;
            Debug.Log($"INVENTÁRIO: [{itemName}] adicionado à BOLSA. Peso: {_currentWeight}kg / {maxBagWeight}kg.");

            if (_currentWeight > maxBagWeight)
            {
                TearBag();
            }

            OnInventoryUpdated?.Invoke();
            return true;
        }

        private void TearBag()
        {
            isBagTorn = true;
            Debug.LogError("INVENTÁRIO: Limite de peso excedido. A bolsa rasgou! Penalidades aplicadas.");
            OnBagTorn?.Invoke();
        }
    }
}