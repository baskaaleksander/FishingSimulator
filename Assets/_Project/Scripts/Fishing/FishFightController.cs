using System;
using UnityEngine;

namespace FishingSim
{
    public class FishFightController : MonoBehaviour
    {
        [SerializeField] FishingInput input;
        [SerializeField] LureController lure;
        [SerializeField] FishAIController fishAI;
        [SerializeField] FishingLineRenderer line;
        [SerializeField] Transform fish;
        [SerializeField] Transform shorePoint;
        [SerializeField] FightConfig config;
        [SerializeField] float swimDepth = 0.4f;
        [SerializeField] float swimAmplitude = 3f;

        GameManager gm;
        LineTensionModel model;
        Vector3 awayDirection;
        float surfaceY;
        float swimPhase;
        float smoothedPull;
        float lateralOffset;
        float lateralVelocity;

        public FightResult LastResult { get; private set; }

        // tension (0-1), stamina (0-1), distance in meters
        public event Action<float, float, float> FightUpdated;

        void Start()
        {
            gm = GameManager.Instance;
            gm.StateChanged += OnStateChanged;
            gm.AttemptReset += OnAttemptReset;
            fish.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (gm == null) return;
            gm.StateChanged -= OnStateChanged;
            gm.AttemptReset -= OnAttemptReset;
        }

        void OnStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Fighting) BeginFight();
        }

        void OnAttemptReset()
        {
            model = null;
            fishAI.Clear();
            fish.gameObject.SetActive(false);
            line.SetTension(0f);
        }

        void BeginFight()
        {
            Vector3 hookPosition = lure.transform.position;
            surfaceY = hookPosition.y;

            Vector3 flat = hookPosition - shorePoint.position;
            flat.y = 0f;
            float distance = flat.magnitude;
            awayDirection = distance > 0.01f ? flat / distance : Vector3.forward;

            model = new LineTensionModel(config, Mathf.Max(distance, config.minStartDistance));
            fishAI.Begin();
            swimPhase = 0f;
            smoothedPull = 0f;
            lateralOffset = 0f;
            lateralVelocity = 0f;
            fish.gameObject.SetActive(true);
            PlaceFish(0f, 0f);
        }

        void Update()
        {
            if (gm.State != GameState.Fighting || model == null) return;

            float dt = Time.deltaTime;
            bool reeling = input.Reel.IsPressed();
            float pull = fishAI.Tick(dt, model.Stamina);
            FightResult result = model.Step(dt, reeling, pull);

            PlaceFish(pull, dt);
            line.SetTension(model.Tension / 100f);
            FightUpdated?.Invoke(model.Tension / 100f, model.Stamina / config.maxStamina, model.Distance);

            if (result != FightResult.None) Finish(result);
        }

        void Finish(FightResult result)
        {
            LastResult = result;
            bool caught = result == FightResult.Caught;
            fishAI.Finish(caught);
            line.SetTension(0f);
            if (!caught) fish.gameObject.SetActive(false);
            gm.TrySetState(caught ? GameState.Caught : GameState.Escaped);
        }

        void PlaceFish(float pull, float dt)
        {
            if (dt > 0f)
            {
                smoothedPull = Mathf.Lerp(smoothedPull, pull, 1f - Mathf.Exp(-3f * dt));
                swimPhase += dt * (0.6f + smoothedPull * 0.02f);
            }

            float calm = Mathf.Clamp01(model.Distance / 5f);
            float targetLateral = Mathf.Sin(swimPhase) * swimAmplitude * Mathf.Clamp01(smoothedPull / 100f) * calm;
            if (dt > 0f) lateralOffset = Mathf.SmoothDamp(lateralOffset, targetLateral, ref lateralVelocity, 0.5f, 3f, dt);

            Vector3 right = Vector3.Cross(Vector3.up, awayDirection);
            Vector3 position = shorePoint.position + awayDirection * model.Distance + right * lateralOffset;
            position.y = surfaceY;

            lure.MoveTo(position);
            fish.position = position + Vector3.down * swimDepth;

            Vector3 heading = awayDirection + right * (lateralVelocity * 0.15f);
            Quaternion targetRotation = Quaternion.LookRotation(heading);
            fish.rotation = dt > 0f ? Quaternion.Slerp(fish.rotation, targetRotation, 1f - Mathf.Exp(-6f * dt)) : targetRotation;
        }
    }
}
