using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Player;

namespace Core.Missions
{
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private PlayerVitals playerVitals;

        // Dicionários para busca em tempo real (O(1) de complexidade)
        private Dictionary<string, MissionState> _missionStates = new Dictionary<string, MissionState>();
        private Dictionary<string, MissionData> _activeMissions = new Dictionary<string, MissionData>();

        public event Action<MissionData> OnMissionStarted;
        public event Action<MissionData, GradeMedal> OnMissionCompleted;
        public event Action<MissionData> OnMissionFailedOrMissed;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartMission(MissionData mission)
        {
            if (_activeMissions.ContainsKey(mission.missionID)) return;

            _missionStates[mission.missionID] = MissionState.Active;
            _activeMissions.Add(mission.missionID, mission);

            OnMissionStarted?.Invoke(mission);
            Debug.Log($"MISSÃO INICIADA: {mission.missionTitle} | Tipo: {mission.type}");
        }

        public void CompleteMission(MissionData mission)
        {
            if (!_activeMissions.ContainsKey(mission.missionID)) return;

            _missionStates[mission.missionID] = MissionState.Completed;
            _activeMissions.Remove(mission.missionID);

            GradeMedal earnedMedal = GradeMedal.None;
            if (mission.type == MissionType.Main)
            {
                earnedMedal = CalculateMedal(mission);
            }

            OnMissionCompleted?.Invoke(mission, earnedMedal);
            Debug.Log($"MISSÃO CONCLUÍDA: {mission.missionTitle} | Medalha: {earnedMedal}");
        }

        public void FailOrMissMission(MissionData mission)
        {
            if (!_activeMissions.ContainsKey(mission.missionID)) return;

            _missionStates[mission.missionID] = mission.isMissable ? MissionState.Missed : MissionState.Failed;
            _activeMissions.Remove(mission.missionID);

            OnMissionFailedOrMissed?.Invoke(mission);
            Debug.LogWarning($"MISSÃO PERDIDA/FALHA: {mission.missionTitle}");
        }

        private GradeMedal CalculateMedal(MissionData mission)
        {
            if (playerVitals == null) return GradeMedal.None;

            int currentIntegrity = playerVitals.GetType().GetField("currentIntegrity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null
                ? (int)playerVitals.GetType().GetField("currentIntegrity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(playerVitals)
                : 150; // Fallback temporário caso currentIntegrity seja estritamente privado sem propriedade de leitura

            if (currentIntegrity >= mission.diamondIntegrityThreshold) return GradeMedal.Diamond;
            if (currentIntegrity >= mission.goldIntegrityThreshold) return GradeMedal.Gold;
            if (currentIntegrity >= mission.silverIntegrityThreshold) return GradeMedal.Silver;
            if (currentIntegrity >= mission.bronzeIntegrityThreshold) return GradeMedal.Bronze;

            return GradeMedal.None;
        }

        // Futuro "Tablet" chamará esta função para popular a UI
        public List<MissionData> GetActiveMissions()
        {
            return new List<MissionData>(_activeMissions.Values);
        }
    }
}