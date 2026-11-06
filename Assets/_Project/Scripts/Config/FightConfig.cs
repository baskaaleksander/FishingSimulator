using UnityEngine;

namespace FishingSim
{
    [CreateAssetMenu(menuName = "Fishing/Fight Config", fileName = "FightConfig")]
    public class FightConfig : ScriptableObject
    {
        [Header("Line tension (0-100)")]
        [Tooltip("Tension added while the reel button is held")]
        public float reelTension = 35f;
        public float tensionRiseRate = 80f;
        public float tensionFallRate = 50f;
        [Tooltip("How much of the fish pull force turns into tension")]
        public float fishTensionFactor = 0.8f;
        public float breakThreshold = 90f;
        public float breakTime = 1f;
        [Tooltip("Tension the line starts the fight with")]
        public float startTension = 30f;
        [Tooltip("Below this tension the line counts as slack")]
        public float slackThreshold = 12f;
        [Tooltip("Seconds of slack line after which the fish shakes off the hook")]
        public float slackTime = 4f;

        [Header("Distance")]
        public float reelSpeed = 3.5f;
        [Tooltip("Pull force at which reeling makes no progress at all")]
        public float resistPull = 80f;
        [Tooltip("Meters per second the fish gains per point of pull")]
        public float pullAwayFactor = 0.015f;
        public float catchDistance = 2f;
        public float minStartDistance = 8f;
        public float maxDistance = 60f;

        [Header("Stamina")]
        public float maxStamina = 100f;
        public float moderateTensionMin = 30f;
        public float moderateTensionMax = 80f;
        public float staminaDrainRate = 8f;
        [Tooltip("Stamina regained per second while the line is slack")]
        public float staminaRegenRate = 4f;
    }
}
