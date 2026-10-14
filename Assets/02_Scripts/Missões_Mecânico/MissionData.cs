using UnityEngine;

namespace Core.Missions
{
    public enum MissionType { Main, Secondary, Optional }
    public enum MissionState { Locked, Available, Active, Completed, Failed, Missed }
    public enum GradeMedal { None, Bronze, Silver, Gold, Diamond }

    [CreateAssetMenu(fileName = "NewMission", menuName = "Cheetah/Mission Data")]
    public class MissionData : ScriptableObject
    {
        [Header("Mission Info")]
        public string missionID;
        public string missionTitle;
        [TextArea] public string description;
        public MissionType type;

        [Header("Time & Availability")]
        public bool isMissable;
        public float timeToExpire = 0f; // 0 significa que não expira

        [Header("Grading Criteria (Minigames)")]
        [Tooltip("Define a Integridade MÍNIMA exigida no momento da conclusão para cada medalha.")]
        public int diamondIntegrityThreshold = 240;
        public int goldIntegrityThreshold = 200;
        public int silverIntegrityThreshold = 150;
        public int bronzeIntegrityThreshold = 100;
    }
}