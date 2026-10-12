using UnityEngine;
using UnityEngine.Events;

namespace Core.Triggers
{
    [RequireComponent(typeof(Collider2D))]
    public class EventTrigger2D : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [SerializeField] private string targetTag = "Player";
        [Tooltip("Se verdadeiro, o evento só será disparado uma vez na partida.")]
        [SerializeField] private bool triggerOnce = true;

        private bool _hasTriggered = false;

        [Header("Game Events")]
        public UnityEvent OnTriggerEntered;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered && triggerOnce) return;

            if (other.CompareTag(targetTag))
            {
                _hasTriggered = true;
                OnTriggerEntered?.Invoke();
            }
        }
    }
}