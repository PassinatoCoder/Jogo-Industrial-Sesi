using UnityEngine;

namespace Core.Interaction
{
    public interface IWorldInteractable
    {
        string InteractionPrompt { get; }
        void OnInteract(GameObject interactor);
    }

    public interface IHeldItem
    {
        void OnUse();       // GDD: Tecla [F]
        void OnInspect();   // GDD: Tecla [Y]
        void OnStore();     // GDD: Tecla [G]
        void OnDrop();      // GDD: Tecla [E] para devolver/soltar
    }
}