using UnityEngine;

namespace FishingSim
{
    public static class CastMath
    {
        public static float Power01(float heldTime, float chargeTime)
        {
            if (chargeTime <= 0f) return 1f;
            return Mathf.Clamp01(heldTime / chargeTime);
        }

        public static float Force(float power01, float minForce, float maxForce)
        {
            return Mathf.Lerp(minForce, maxForce, Mathf.Clamp01(power01));
        }

        public static Vector3 Direction(Vector3 forward, Vector3 right, float elevationDegrees)
        {
            return (Quaternion.AngleAxis(-elevationDegrees, right) * forward).normalized;
        }
    }
}
