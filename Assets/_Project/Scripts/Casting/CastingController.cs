using System;
using UnityEngine;

namespace FishingSim
{
    public class CastingController : MonoBehaviour
    {
        [SerializeField] FishingInput input;
        [SerializeField] LureController lure;
        [SerializeField] Transform cameraTransform;
        [SerializeField] Transform rodTip;
        [SerializeField] CastingConfig config;

        GameManager gm;
        float heldTime;

        public event Action<float> ChargeChanged;

        void Start()
        {
            gm = GameManager.Instance;
            gm.AttemptReset += OnAttemptReset;
            lure.LandedInWater += OnLanded;
            lure.Missed += OnMissed;
            lure.Attach(rodTip, config.flightTimeout);
        }

        void OnDestroy()
        {
            if (gm != null) gm.AttemptReset -= OnAttemptReset;
            if (lure != null)
            {
                lure.LandedInWater -= OnLanded;
                lure.Missed -= OnMissed;
            }
        }

        void Update()
        {
            switch (gm.State)
            {
                case GameState.Ready:
                    if (input.Cast.WasPressedThisFrame() && gm.TrySetState(GameState.Charging))
                    {
                        heldTime = 0f;
                        ChargeChanged?.Invoke(0f);
                    }
                    break;

                case GameState.Charging:
                    heldTime += Time.deltaTime;
                    float power = CastMath.Power01(heldTime, config.chargeTime);
                    ChargeChanged?.Invoke(power);
                    if (!input.Cast.IsPressed()) Release(power);
                    break;

                case GameState.WaitingForBite:
                    if (input.Reel.WasPressedThisFrame()) gm.ResetAttempt();
                    break;
            }
        }

        void Release(float power)
        {
            if (!gm.TrySetState(GameState.Casting)) return;
            Vector3 direction = CastMath.Direction(cameraTransform.forward, cameraTransform.right, config.elevationAngle);
            float speed = CastMath.Force(power, config.minForce, config.maxForce);
            lure.Launch(direction * speed);
        }

        void OnLanded(Vector3 position)
        {
            gm.TrySetState(GameState.WaitingForBite);
        }

        void OnMissed()
        {
            gm.ResetAttempt();
        }

        void OnAttemptReset()
        {
            heldTime = 0f;
            lure.ResetLure();
            ChargeChanged?.Invoke(0f);
        }
    }
}
