using UnityEngine;

namespace FishingSim
{
    public enum FishState
    {
        Idle,
        Hooked,
        Struggling,
        Recovering,
        Exhausted,
        Caught,
        Escaped
    }

    public class FishStateMachine
    {
        readonly FishConfig config;
        readonly System.Random random;
        float stateTimer;
        float jerkTimer;

        public FishState State { get; private set; } = FishState.Idle;
        public float PullForce { get; private set; }

        public FishStateMachine(FishConfig config, System.Random random)
        {
            this.config = config;
            this.random = random;
        }

        public void Hook() => Enter(FishState.Hooked);
        public void MarkCaught() => Enter(FishState.Caught);
        public void MarkEscaped() => Enter(FishState.Escaped);

        public float Tick(float dt, float stamina)
        {
            if (State == FishState.Idle || State == FishState.Caught || State == FishState.Escaped)
                return 0f;

            stateTimer -= dt;
            jerkTimer -= dt;

            switch (State)
            {
                case FishState.Hooked:
                    if (stateTimer <= 0f) Enter(FishState.Struggling);
                    break;
                case FishState.Struggling:
                    if (stamina <= config.exhaustedThreshold) Enter(FishState.Exhausted);
                    else if (stateTimer <= 0f) Enter(FishState.Recovering);
                    break;
                case FishState.Recovering:
                    if (stamina <= config.exhaustedThreshold) Enter(FishState.Exhausted);
                    else if (stateTimer <= 0f) Enter(FishState.Struggling);
                    break;
                case FishState.Exhausted:
                    if (stamina >= config.exhaustedRecoverThreshold) Enter(FishState.Recovering);
                    break;
            }

            if (jerkTimer <= 0f)
            {
                PullForce = RollPull();
                jerkTimer = Range(config.jerkInterval);
            }
            return PullForce;
        }

        void Enter(FishState next)
        {
            State = next;
            switch (next)
            {
                case FishState.Hooked: stateTimer = config.hookedDelay; break;
                case FishState.Struggling: stateTimer = Range(config.struggleDuration); break;
                case FishState.Recovering: stateTimer = Range(config.recoverDuration); break;
                default: stateTimer = 0f; break;
            }
            PullForce = RollPull();
            jerkTimer = Range(config.jerkInterval);
        }

        float RollPull()
        {
            switch (State)
            {
                case FishState.Hooked: return Range(config.hookedPull);
                case FishState.Struggling: return Range(config.strugglePull);
                case FishState.Recovering: return Range(config.recoverPull);
                case FishState.Exhausted: return Range(config.exhaustedPull);
                default: return 0f;
            }
        }

        float Range(Vector2 minMax)
        {
            return minMax.x + (float)random.NextDouble() * (minMax.y - minMax.x);
        }
    }
}
