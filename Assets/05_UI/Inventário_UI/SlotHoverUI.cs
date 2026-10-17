using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Core.Inventory;

namespace Core.UI
{
    public class SlotHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public int slotIndex;
        public bool isBagSlot;
        public InventoryManager inventoryManager;
        public TextMeshProUGUI tooltipText;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (inventoryManager == null || tooltipText == null) return;
            GameObject item = isBagSlot ?
                (slotIndex < inventoryManager.Bag.Count ? inventoryManager.Bag[slotIndex] : null) :
                (slotIndex < inventoryManager.ToolBelt.Length ? inventoryManager.ToolBelt[slotIndex] : null);

            if (item != null)
            {
                tooltipText.text = item.name;
                tooltipText.gameObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipText != null) tooltipText.gameObject.SetActive(false);
        }
    }
}