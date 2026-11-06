using System;
using UnityEngine;

namespace FishingSim
{
    [RequireComponent(typeof(Rigidbody))]
    public class LureController : MonoBehaviour
    {
        [SerializeField] float bobHeight = 0.03f;
        [SerializeField] float bobSpeed = 2f;

        enum Phase { Attached, Flying, Floating }

        Rigidbody rb;
        Transform tip;
        Phase phase = Phase.Attached;
        float flightTime;
        float flightTimeout = 6f;
        Vector3 floatPosition;

        public bool InWater => phase == Phase.Floating;

        public event Action<Vector3> LandedInWater;
        public event Action Missed;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void Attach(Transform rodTip, float timeout)
        {
            tip = rodTip;
            flightTimeout = timeout;
            ResetLure();
        }

        public void Launch(Vector3 velocity)
        {
            phase = Phase.Flying;
            flightTime = 0f;
            rb.isKinematic = false;
            rb.linearVelocity = velocity;
        }

        public void ResetLure()
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            phase = Phase.Attached;
            if (tip != null) transform.position = tip.position;
        }

        void LateUpdate()
        {
            switch (phase)
            {
                case Phase.Attached:
                    if (tip != null) transform.position = tip.position;
                    break;
                case Phase.Flying:
                    flightTime += Time.deltaTime;
                    if (flightTime > flightTimeout) Miss();
                    break;
                case Phase.Floating:
                    transform.position = floatPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
                    break;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (phase != Phase.Flying) return;
            if (other.gameObject.layer != LayerMask.NameToLayer("Water")) return;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            phase = Phase.Floating;
            floatPosition = new Vector3(transform.position.x, other.bounds.max.y, transform.position.z);
            transform.position = floatPosition;
            LandedInWater?.Invoke(floatPosition);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (phase == Phase.Flying) Miss();
        }

        void Miss()
        {
            ResetLure();
            Missed?.Invoke();
        }
    }
}
