using UnityEngine;

namespace FishingSim
{
    [CreateAssetMenu(menuName = "Fishing/Casting Config", fileName = "CastingConfig")]
    public class CastingConfig : ScriptableObject
    {
        [Tooltip("Launch speed (m/s) at 0% charge")]
        public float minForce = 5f;
        [Tooltip("Launch speed (m/s) at 100% charge")]
        public float maxForce = 20f;
        [Tooltip("Seconds of holding the button to reach 100%")]
        public float chargeTime = 2f;
        [Tooltip("Extra upward angle (degrees) added to the camera direction")]
        public float elevationAngle = 20f;
        [Tooltip("Seconds in the air before a cast counts as a miss")]
        public float flightTimeout = 6f;
    }
}
