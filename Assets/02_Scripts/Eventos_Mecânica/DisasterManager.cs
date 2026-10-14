using UnityEngine;
using System;
using Core.Player;

namespace Core.Events
{
    public class DisasterManager : MonoBehaviour
    {
        public static DisasterManager Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private PlayerVitals playerVitals;

        [Header("Global Settings")]
        [SerializeField] private int integrityReward = 15;
        [SerializeField] private int integrityPenalty = -25;

        private string _activeDisasterID;
        private bool _isDisasterActive;

        public event Action<string> OnDisasterStarted;
        public event Action<string, bool> OnDisasterResolved; // bool indica se foi sucesso ou ignorado

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void TriggerDisaster(string disasterID)
        {
            if (_isDisasterActive) return;

            _activeDisasterID = disasterID;
            _isDisasterActive = true;

            OnDisasterStarted?.Invoke(disasterID);
            Debug.LogWarning($"DESASTRE INICIADO: {disasterID}. O jogador deve resolver ou ignorar.");
        }

        public void ResolveDisaster(bool success)
        {
            if (!_isDisasterActive) return;

            if (success)
            {
                playerVitals.ModifyIntegrity(integrityReward);
                Debug.Log($"DESASTRE RESOLVIDO: {_activeDisasterID}. Integridade: +{integrityReward}");
            }
            else
            {
                playerVitals.ModifyIntegrity(integrityPenalty);
                Debug.LogWarning($"DESASTRE IGNORADO/FALHO: {_activeDisasterID}. Integridade: {integrityPenalty}");
            }

            OnDisasterResolved?.Invoke(_activeDisasterID, success);
            _isDisasterActive = false;
            _activeDisasterID = string.Empty;
        }
    }
}