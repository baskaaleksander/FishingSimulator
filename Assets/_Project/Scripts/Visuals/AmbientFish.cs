using System.Collections.Generic;
using UnityEngine;

namespace FishingSim
{
    public class AmbientFish : MonoBehaviour
    {
        [SerializeField] LureController lure;
        [SerializeField] float minShoreDistance = 6f;
        [SerializeField] Vector2 speedRange = new Vector2(0.5f, 1.4f);
        [SerializeField] float turnSpeed = 100f;
        [SerializeField] float fleeRadius = 5f;
        [SerializeField] float fleeSpeedMultiplier = 2.5f;

        class Swimmer
        {
            public Transform transform;
            public Quaternion heading;
            public Vector3 target;
            public float speed;
            public float pause;
            public float phase;
            public float fleeTime;
        }

        readonly List<Swimmer> swimmers = new List<Swimmer>();

        void Start()
        {
            foreach (Transform child in transform)
            {
                var swimmer = new Swimmer
                {
                    transform = child,
                    heading = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    speed = Random.Range(speedRange.x, speedRange.y),
                    phase = Random.Range(0f, 6.28f)
                };
                child.position = RandomPoint();
                swimmer.target = RandomPoint();
                swimmers.Add(swimmer);
            }

            if (lure != null) lure.LandedInWater += OnLureLanded;
        }

        void OnDestroy()
        {
            if (lure != null) lure.LandedInWater -= OnLureLanded;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var s in swimmers) Step(s, dt);
        }

        void Step(Swimmer s, float dt)
        {
            s.phase += dt * (3f + s.speed * 3f);
            float wiggle = Mathf.Sin(s.phase) * 10f;

            if (s.pause > 0f)
            {
                s.pause -= dt;
                s.transform.rotation = s.heading * Quaternion.Euler(0f, wiggle * 0.3f, 0f);
                return;
            }

            Vector3 position = s.transform.position;
            Vector3 toTarget = s.target - position;
            if (toTarget.magnitude < 0.8f)
            {
                s.target = RandomPoint();
                s.speed = Random.Range(speedRange.x, speedRange.y);
                if (Random.value < 0.3f) s.pause = Random.Range(0.5f, 2f);
                return;
            }

            float speed = s.speed;
            if (s.fleeTime > 0f)
            {
                s.fleeTime -= dt;
                speed *= fleeSpeedMultiplier;
            }

            Quaternion wanted = Quaternion.LookRotation(toTarget.normalized);
            s.heading = Quaternion.RotateTowards(s.heading, wanted, turnSpeed * dt);
            position += s.heading * Vector3.forward * (speed * dt);
            position.y = KeepAboveBed(position);
            s.transform.SetPositionAndRotation(position, s.heading * Quaternion.Euler(0f, wiggle, 0f));
        }

        void OnLureLanded(Vector3 landing)
        {
            foreach (var s in swimmers)
            {
                Vector3 offset = s.transform.position - landing;
                offset.y = 0f;
                if (offset.magnitude > fleeRadius) continue;

                Vector3 away = offset.sqrMagnitude > 0.01f ? offset.normalized : Random.insideUnitSphere;
                s.target = ClampToLake(s.transform.position + away * 10f);
                s.fleeTime = 1.5f;
                s.pause = 0f;
            }
        }

        Vector3 RandomPoint()
        {
            for (int i = 0; i < 200; i++)
            {
                float x = Random.Range(-46f, 46f);
                float z = Random.Range(4f, 78f);
                if (LakeTerrain.SignedDistance(x, z) > -minShoreDistance) continue;

                float bottom = LakeTerrain.GroundHeight(x, z) + 0.3f;
                float top = LakeTerrain.WaterLevel - 0.4f;
                if (bottom >= top) continue;
                return new Vector3(x, Random.Range(bottom, top), z);
            }
            return new Vector3(LakeTerrain.Center.x, LakeTerrain.WaterLevel - 1f, LakeTerrain.Center.y);
        }

        Vector3 ClampToLake(Vector3 point)
        {
            for (int i = 0; i < 30 && LakeTerrain.SignedDistance(point.x, point.z) > -minShoreDistance; i++)
            {
                point.x = Mathf.Lerp(point.x, LakeTerrain.Center.x, 0.15f);
                point.z = Mathf.Lerp(point.z, LakeTerrain.Center.y, 0.15f);
            }
            point.y = KeepAboveBed(point);
            return point;
        }

        static float KeepAboveBed(Vector3 position)
        {
            float bottom = LakeTerrain.GroundHeight(position.x, position.z) + 0.2f;
            float top = LakeTerrain.WaterLevel - 0.3f;
            return bottom < top ? Mathf.Clamp(position.y, bottom, top) : top;
        }
    }
}
