using System;
using UnityEngine;

namespace Core.Minigames.Welding
{
    public enum WeldingDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Números do minigame. A dificuldade altera uma cópia de runtime dos parâmetros-base;
    /// os valores configurados no Inspector continuam preservados entre partidas.
    /// </summary>
    [Serializable]
    public sealed class WeldingSettings
    {
        [Header("Estado inicial do personagem")]
        [Range(0, 100)] public int startingIntegrity = 100;
        [Range(0, 100)] public int startingStamina = 100;
        [Range(0, 100)] public int startingHealth = 100;

        [Header("Penalidades de integridade")]
        public int penaltyWrongEpi = 5;
        public int penaltyRushedArea = 10;
        public int penaltyArcFlash = 15;
        public int penaltyBurnNpc = 10;
        public int penaltyIgnoredHazard = 10;
        public int penaltyBreathless = 5;

        [Header("Recuperação de integridade")]
        public int integrityRewardCleanArea = 3;
        public int integrityRewardGoodWeld = 2;

        [Header("Dano ao personagem")]
        [Range(1, 100)] public int arcFlashDamage = 22;
        [Range(1, 100)] public int darkWeldingDamage = 8;
        [Range(0f, 1f)] public float darkInjuryChance = 0.22f;
        public float darkExposureInterval = 2.5f;
        [Range(1, 100)] public int breathlessDamage = 5;

        [Header("Piscada e passos (transição entre lugares)")]
        public float blinkClose = 0.14f;
        public float blinkHold = 0.28f;
        public float blinkOpen = 0.2f;
        [Tooltip("Quantos passos tocam quando ela anda até outro lugar.")]
        public int footstepsPerWalk = 4;
        public float footstepInterval = 0.27f;

        [Header("Biombo")]
        [Range(0f, 1f)] public float screenJamChance = 0.4f;
        public float mashGoal = 14f;
        public float mashDecay = 2.2f;

        [Header("Extintor")]
        [Range(0f, 1f)] public float extinguisherEmptyChance = 0.45f;
        [Range(0f, 1f)] public float extinguisherJamChance = 0.4f;

        [Header("Eventos aleatórios")]
        [Range(0f, 1.5f)] public float randomEventChance = 0.85f;

        [Header("Peças e eventos durante a solda")]
        [Tooltip("A dificuldade sobrescreve este valor: fácil = 2, médio = 4, difícil = 6.")]
        public int piecesToWeld = 4;
        public float eventIntervalMin = 7f;
        public float eventIntervalMax = 13f;
        public int jamMashGoal = 12;
        public float npcArriveSeconds = 7f;
        public float tremorMax = 16f;

        [Header("Solda")]
        public Vector2[] seamPoints =
        {
            new Vector2(0.08f, 0.50f), new Vector2(0.24f, 0.57f), new Vector2(0.44f, 0.45f),
            new Vector2(0.62f, 0.55f), new Vector2(0.80f, 0.47f), new Vector2(0.92f, 0.50f)
        };
        public Vector2 pieceSize = new Vector2(1200f, 380f);
        public Vector2 piecePosition = new Vector2(0f, -40f);
        public bool showSeamGuide = true;
        public float seamGoodTolerance = 28f;
        public float seamMaxTolerance = 70f;
        public float binLength = 24f;
        public float binTime = 0.17f;
        public float stallTime = 0.45f;
        public float heatRise = 0.8f;
        public float heatFall = 0.7f;
        public float coolSeconds = 3f;
        public Vector2 torchTipPivot = new Vector2(0.1f, 0.9f);
        public Vector2 torchSize = new Vector2(520f, 260f);

        [Header("Máscara, oxigênio e stamina")]
        public float oxygenSeconds = 16f;
        public float oxygenRefillSeconds = 5f;
        public float oxygenArcMultiplier = 1.35f;
        public float gaspGraceSeconds = 5f;
        public float arcFlashStun = 1.4f;
        [Tooltip("Quanto a stamina cai por segundo enquanto trabalha com o arco ligado.")]
        public float staminaWorkDrain = 1.35f;
        [Tooltip("Quanto a stamina se recupera por segundo fora do arco e respirando.")]
        public float staminaRecovery = 5.5f;

        [NonSerialized] private string _baseJson;

        /// <summary>Restaura os parâmetros-base e aplica o perfil escolhido sem acumular multiplicadores.</summary>
        public void ApplyDifficulty(WeldingDifficulty difficulty)
        {
            if (string.IsNullOrEmpty(_baseJson)) _baseJson = JsonUtility.ToJson(this);
            JsonUtility.FromJsonOverwrite(_baseJson, this);

            switch (difficulty)
            {
                case WeldingDifficulty.Easy:
                    piecesToWeld = 2;
                    oxygenSeconds *= 1.45f;
                    oxygenRefillSeconds *= 0.72f;
                    gaspGraceSeconds *= 1.3f;
                    randomEventChance *= 0.55f;
                    eventIntervalMin *= 1.35f;
                    eventIntervalMax *= 1.35f;
                    screenJamChance *= 0.55f;
                    extinguisherEmptyChance *= 0.65f;
                    extinguisherJamChance *= 0.6f;
                    seamGoodTolerance *= 1.3f;
                    seamMaxTolerance *= 1.2f;
                    binTime *= 0.82f;
                    darkInjuryChance *= 0.55f;
                    staminaWorkDrain *= 0.8f;
                    staminaRecovery *= 1.2f;
                    oxygenArcMultiplier *= 0.9f;
                    tremorMax *= 0.75f;
                    jamMashGoal = Mathf.Max(5, Mathf.RoundToInt(jamMashGoal * 0.8f));
                    mashGoal *= 0.8f;
                    mashDecay *= 0.85f;
                    penaltyWrongEpi = Mathf.RoundToInt(penaltyWrongEpi * 0.7f);
                    penaltyRushedArea = Mathf.RoundToInt(penaltyRushedArea * 0.7f);
                    penaltyArcFlash = Mathf.RoundToInt(penaltyArcFlash * 0.75f);
                    penaltyBurnNpc = Mathf.RoundToInt(penaltyBurnNpc * 0.75f);
                    penaltyIgnoredHazard = Mathf.RoundToInt(penaltyIgnoredHazard * 0.7f);
                    break;

                case WeldingDifficulty.Hard:
                    piecesToWeld = 6;
                    oxygenSeconds *= 0.65f;
                    oxygenRefillSeconds *= 1.3f;
                    gaspGraceSeconds *= 0.75f;
                    randomEventChance = Mathf.Min(1.5f, randomEventChance * 1.35f);
                    eventIntervalMin *= 0.72f;
                    eventIntervalMax *= 0.72f;
                    screenJamChance = Mathf.Clamp01(screenJamChance * 1.3f);
                    extinguisherEmptyChance = Mathf.Clamp01(extinguisherEmptyChance * 1.3f);
                    extinguisherJamChance = Mathf.Clamp01(extinguisherJamChance * 1.3f);
                    seamGoodTolerance *= 0.78f;
                    seamMaxTolerance *= 0.9f;
                    binTime *= 1.2f;
                    darkInjuryChance = Mathf.Clamp01(darkInjuryChance * 1.35f);
                    staminaWorkDrain *= 1.25f;
                    staminaRecovery *= 0.8f;
                    oxygenArcMultiplier *= 1.2f;
                    tremorMax *= 1.25f;
                    jamMashGoal = Mathf.RoundToInt(jamMashGoal * 1.25f);
                    mashGoal *= 1.2f;
                    mashDecay *= 1.2f;
                    penaltyWrongEpi = Mathf.RoundToInt(penaltyWrongEpi * 1.3f);
                    penaltyRushedArea = Mathf.RoundToInt(penaltyRushedArea * 1.3f);
                    penaltyArcFlash = Mathf.RoundToInt(penaltyArcFlash * 1.35f);
                    penaltyBurnNpc = Mathf.RoundToInt(penaltyBurnNpc * 1.3f);
                    penaltyIgnoredHazard = Mathf.RoundToInt(penaltyIgnoredHazard * 1.3f);
                    break;

                default:
                    piecesToWeld = 4;
                    break;
            }

            eventIntervalMin = Mathf.Max(2f, eventIntervalMin);
            eventIntervalMax = Mathf.Max(eventIntervalMin + 0.5f, eventIntervalMax);
            oxygenSeconds = Mathf.Max(4f, oxygenSeconds);
            oxygenRefillSeconds = Mathf.Max(1f, oxygenRefillSeconds);
        }
    }
}
