using UnityEngine;

namespace FishingSim
{
    public static class LakeTerrain
    {
        public const float WaterLevel = -0.5f;
        public static readonly Vector2 Center = new Vector2(0f, 34f);

        const float RadiusX = 32f;
        const float RadiusZ = 30f;
        const float MeanRadius = 31f;
        const float BeachWidth = 2.5f;
        const float MaxDepth = -3.6f;
        const float BedSlope = 0.13f;

        // Negative inside the lake, positive on land, in roughly meters from the shoreline.
        public static float SignedDistance(float x, float z)
        {
            float px = (x - Center.x) / RadiusX;
            float pz = (z - Center.y) / RadiusZ;
            float r = Mathf.Sqrt(px * px + pz * pz);
            float a = Mathf.Atan2(pz, px);
            float half = Mathf.PI * 0.5f;

            // every term is zero at a = -90 degrees, so the shore in front of the dock stays put
            float boundary = 1f
                + 0.12f * Mathf.Sin(2f * a)
                + 0.08f * Mathf.Sin(3f * a + half)
                + 0.05f * Mathf.Sin(5f * a + half)
                + 0.04f * Mathf.Sin(7f * a + half)
                + 0.025f * Mathf.Sin(9f * a + half);

            return (r - boundary) * MeanRadius;
        }

        public static float GroundHeight(float x, float z)
        {
            float d = SignedDistance(x, z);

            if (d <= 0f)
            {
                float bed = Mathf.Max(WaterLevel + d * BedSlope, MaxDepth);
                float ripple = (Mathf.PerlinNoise(x * 0.25f, z * 0.25f) - 0.5f) * 0.2f * Mathf.Clamp01(-d / 3f);
                return bed + ripple;
            }

            float beach = Mathf.SmoothStep(WaterLevel, 0f, Mathf.Clamp01(d / BeachWidth));
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 8f) / 17f));
            float hills = Mathf.Max(0f, Mathf.PerlinNoise(x * 0.05f + 100f, z * 0.05f + 100f) - 0.35f) * 4f;
            return beach + rise * hills;
        }
    }
}
