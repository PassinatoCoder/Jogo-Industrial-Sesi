using UnityEngine;
using System;

namespace Core.Systems
{
    public class PlayerIntegrity : MonoBehaviour
    {
        public static PlayerIntegrity Instance { get; private set; }

        [Header("Configurações de Integridade")]
        [Tooltip("O máximo é 250, o mínimo é 0 (demissão). 150 é a média neutra.")]
        [SerializeField] private int maxIntegrity = 250;
        [SerializeField] private int startingIntegrity = 150;

        [Header("Limiares de Reação (Red Dead Honor System)")]
        [SerializeField] private int thresholdIgnored = 100;
        [SerializeField] private int thresholdPenalized = 75;

        private int _currentIntegrity;

        public event Action<int, int> OnIntegrityChanged; // Passa o valor atual e a mudança (+ ou -)

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _currentIntegrity = startingIntegrity;
        }

        /// <summary>
        /// Adiciona pontos por fazer o procedimento NR corretamente.
        /// </summary>
        public void AddIntegrity(int amount)
        {
            if (amount <= 0) return;
            ChangeIntegrity(amount);
            Debug.Log($"[Integrity] +{amount} (Boa atitude profissional). Total: {_currentIntegrity}");
        }

        /// <summary>
        /// Remove pontos por falhas de segurança (ex: não checar EPI).
        /// </summary>
        public void LoseIntegrity(int amount)
        {
            if (amount <= 0) return;
            ChangeIntegrity(-amount);
            Debug.Log($"[Integrity] -{amount} (Violação de Segurança). Total: {_currentIntegrity}");
        }

        private void ChangeIntegrity(int delta)
        {
            _currentIntegrity = Mathf.Clamp(_currentIntegrity + delta, 0, maxIntegrity);
            OnIntegrityChanged?.Invoke(_currentIntegrity, delta);

            CheckThresholds();
        }

        private void CheckThresholds()
        {
            if (_currentIntegrity == 0)
            {
                Debug.LogWarning("[Integrity] INTEGRIDADE ZERO. Demissão. Game Over.");
                // Chamar evento de Game Over aqui no futuro
            }
            else if (_currentIntegrity < thresholdPenalized)
            {
                Debug.Log("[Integrity] Zona de Penalidade. A supervisão está de olho.");
            }
            else if (_currentIntegrity < thresholdIgnored)
            {
                Debug.Log("[Integrity] Moral baixa. NPCs não respondem bem a você.");
            }
        }

        public int GetCurrentIntegrity() => _currentIntegrity;
    }
}