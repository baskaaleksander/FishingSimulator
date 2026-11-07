using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace FishingSim
{
    public class WaterEffects : MonoBehaviour
    {
        [SerializeField] LureController lure;
        [SerializeField] BiteSystem bite;
        [SerializeField] ParticleSystem splash;
        [SerializeField] Material rippleMaterial;
        [SerializeField] int ringSegments = 40;
        [SerializeField] float rippleDuration = 1.6f;
        [SerializeField] float rippleRadius = 1.8f;

        void Start()
        {
            lure.LandedInWater += OnLanded;
            bite.Bite += OnBite;
        }

        void OnDestroy()
        {
            if (lure != null) lure.LandedInWater -= OnLanded;
            if (bite != null) bite.Bite -= OnBite;
        }

        void OnLanded(Vector3 position)
        {
            splash.transform.position = position;
            splash.Play();
            StartCoroutine(Ripples(position, 3, 0.25f, rippleRadius));
        }

        void OnBite()
        {
            StartCoroutine(Ripples(lure.transform.position, 2, 0.2f, rippleRadius * 0.6f));
        }

        IEnumerator Ripples(Vector3 position, int count, float delay, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                StartCoroutine(Ring(position, radius));
                yield return new WaitForSeconds(delay);
            }
        }

        IEnumerator Ring(Vector3 center, float maxRadius)
        {
            var go = new GameObject("Ripple");
            go.transform.position = center + Vector3.up * 0.02f;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = ringSegments;
            lr.sharedMaterial = rippleMaterial;
            lr.alignment = LineAlignment.TransformZ;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;

            float t = 0f;
            while (t < rippleDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / rippleDuration);
                float radius = Mathf.Lerp(0.1f, maxRadius, 1f - (1f - k) * (1f - k));

                for (int i = 0; i < ringSegments; i++)
                {
                    float angle = i / (float)ringSegments * Mathf.PI * 2f;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
                }

                lr.widthMultiplier = Mathf.Lerp(0.05f, 0.01f, k);
                var color = new Color(1f, 1f, 1f, (1f - k) * 0.8f);
                lr.startColor = color;
                lr.endColor = color;
                yield return null;
            }

            Destroy(go);
        }
    }
}
