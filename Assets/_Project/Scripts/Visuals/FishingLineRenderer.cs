using UnityEngine;

namespace FishingSim
{
    [RequireComponent(typeof(LineRenderer))]
    public class FishingLineRenderer : MonoBehaviour
    {
        [SerializeField] Transform rodTip;
        [SerializeField] Transform lure;
        [SerializeField] int segments = 20;
        [SerializeField] float maxSag = 1.5f;

        LineRenderer line;
        float tension01;

        public void SetTension(float value01)
        {
            tension01 = Mathf.Clamp01(value01);
        }

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = segments + 1;
            line.useWorldSpace = true;
        }

        void LateUpdate()
        {
            Vector3 a = rodTip.position;
            Vector3 b = lure.position;
            float distance = Vector3.Distance(a, b);
            float sag = Mathf.Min(maxSag, distance * 0.15f) * (1f - tension01);
            Vector3 control = (a + b) * 0.5f + Vector3.down * sag * 2f;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float u = 1f - t;
                line.SetPosition(i, u * u * a + 2f * u * t * control + t * t * b);
            }
        }
    }
}
