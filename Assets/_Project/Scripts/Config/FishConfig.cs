using UnityEngine;

namespace FishingSim
{
    [CreateAssetMenu(menuName = "Fishing/Fish Config", fileName = "FishConfig")]
    public class FishConfig : ScriptableObject
    {
        [Tooltip("Seconds between the bite and the first struggle")]
        public float hookedDelay = 1f;

        [Header("Durations (x = min, y = max seconds)")]
        public Vector2 struggleDuration = new Vector2(1.5f, 3.5f);
        public Vector2 recoverDuration = new Vector2(1.5f, 3f);
        public Vector2 jerkInterval = new Vector2(0.3f, 0.8f);

        [Header("Pull force (0-100, x = min, y = max)")]
        public Vector2 hookedPull = new Vector2(5f, 20f);
        public Vector2 strugglePull = new Vector2(50f, 90f);
        public Vector2 recoverPull = new Vector2(5f, 20f);
        public Vector2 exhaustedPull = new Vector2(0f, 6f);

        [Header("Stamina thresholds")]
        public float exhaustedThreshold = 15f;
        public float exhaustedRecoverThreshold = 35f;
    }
}
