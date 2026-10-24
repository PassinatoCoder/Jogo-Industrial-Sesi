using UnityEngine;
using System;

namespace Core.Player
{
    public class PlayerVitals : MonoBehaviour
    {
        [Header("Health System")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float healthDegradationRate = 1f;

        [Header("Stamina System")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;
        [SerializeField] private float baseRegenRate = 5f;
        [Range(0f, 1f)] [SerializeField] private float exhaustionThreshold = 0.15f;

        [Header("Integrity System")]
        [SerializeField] private int maxIntegrity = 250;
        [SerializeField] private int currentIntegrity = 150;

        public event Action<float> OnHealthChanged;
        public event Action<float> OnStaminaChanged;
        public event Action<int> OnIntegrityChanged;
        public int CurrentIntegrity => currentIntegrity;

        public float CurrentStamina => currentStamina;

        private void Awake()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
        }

        private void Update()
        {
            ProcessExhaustionPenalty();
        }

        private void ProcessExhaustionPenalty()
        {
            if (currentStamina <= 0f)
            {
                TakeDamage(healthDegradationRate * Time.deltaTime);
            }
        }

        public bool HasStamina(float amount) => currentStamina >= amount;

        public void ConsumeStamina(float amount)
        {
            currentStamina -= amount;
            currentStamina = Mathf.Max(currentStamina, 0f);
            OnStaminaChanged?.Invoke(currentStamina);
        }

        public void RegenStamina(float deltaTime, float multiplier = 1f)
        {
            if (currentStamina >= maxStamina) return;
            currentStamina += baseRegenRate * multiplier * deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina);
        }

        public float GetSpeedModifier()
        {
            return (currentStamina / maxStamina) <= exhaustionThreshold ? 0.5f : 1f;
        }

        public void ModifyIntegrity(int amount)
        {
            currentIntegrity += amount;
            currentIntegrity = Mathf.Clamp(currentIntegrity, 0, maxIntegrity);
            OnIntegrityChanged?.Invoke(currentIntegrity);
        }

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            currentHealth = Mathf.Max(currentHealth, 0f);
            OnHealthChanged?.Invoke(currentHealth);
        }
    }
}