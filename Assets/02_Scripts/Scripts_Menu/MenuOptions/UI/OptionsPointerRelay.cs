using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.UI.Options
{
    /// <summary>
    /// Repassa cliques e arrastos do mouse pro código do menu.
    /// De propósito NÃO existe "mouse em cima" (hover): no menu de opções só o clique conta.
    /// </summary>
    public sealed class OptionsPointerRelay : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IDragHandler
    {
        public Action<PointerEventData> Down;
        public Action<PointerEventData> Click;
        public Action<PointerEventData> Drag;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Down?.Invoke(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Click?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Drag?.Invoke(eventData);
        }
    }
}
