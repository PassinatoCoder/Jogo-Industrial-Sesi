using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Core.Minigames.Welding
{
    /// <summary>Relatório final do minigame e estado que pode ser integrado ao sistema principal.</summary>
    [Serializable]
    public sealed class WeldingResult
    {
        public bool completed;
        public bool victory;
        public string difficulty = "Médio";
        [UnityEngine.Range(0f, 100f)] public float integrity = 100f;
        [UnityEngine.Range(0f, 100f)] public float stamina = 100f;
        [UnityEngine.Range(0f, 100f)] public float health = 100f;
        [UnityEngine.Range(0f, 100f)] public float oxygen = 100f;
        public int mistakes;
        public List<string> mistakeLog = new List<string>();
        public float weldQuality;
        public int reworkCount;
        public float totalTime;
        public string grade = "-";
        public int integrityDelta;

        // Mantido apenas para compatibilidade com integrações antigas; não é mais usado pelo HUD nem pela lógica.
        [Obsolete("Use integrity. O sistema de segurança antigo foi substituído por integridade.")]
        public int safetyScore;
    }

    [Serializable]
    public sealed class WeldingResultEvent : UnityEvent<WeldingResult>
    {
    }
}
