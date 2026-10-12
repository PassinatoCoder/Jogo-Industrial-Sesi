using UnityEngine;
using Core.Interaction;

namespace Core.Dialogue
{
    [RequireComponent(typeof(Collider2D))]
    public class NPCInteractable : MonoBehaviour, IWorldInteractable
    {
        [Header("NPC Data")]
        [SerializeField] private string npcName = "Chefe";
        [SerializeField] private DialogueSequence dialogueData;
        [SerializeField] private float interactionCooldown = 2.0f;

        private float _nextAvailableTime = 0f;

        public string InteractionPrompt => $"Falar com {npcName}";

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void OnInteract(GameObject interactor)
        {
            if (Time.time < _nextAvailableTime) return;

            if (dialogueData == null)
            {
                Debug.LogError($"[Dialogue] {npcName}: Sequência de dados ausente.");
                return;
            }

            _nextAvailableTime = Time.time + interactionCooldown;
            DialogueManager.Instance.StartDialogue(dialogueData);
        }
    }
}