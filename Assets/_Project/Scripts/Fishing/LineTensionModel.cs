using UnityEngine;

namespace FishingSim
{
    public enum FightResult
    {
        None,
        Caught,
        LineBroke,
        FishEscaped
    }

    public class LineTensionModel
    {
        readonly FightConfig config;
        float overTensionTimer;
        float slackTimer;

        public float Tension { get; private set; }
        public float Stamina { get; private set; }
        public float Distance { get; private set; }

        public LineTensionModel(FightConfig config, float startDistance)
        {
            this.config = config;
            Stamina = config.maxStamina;
            Tension = config.startTension;
            Distance = Mathf.Clamp(startDistance, 0f, config.maxDistance);
        }

        public FightResult Step(float dt, bool reeling, float fishPull)
        {
            float target = (reeling ? config.reelTension : 0f) + fishPull * config.fishTensionFactor;
            float rate = target > Tension ? config.tensionRiseRate : config.tensionFallRate;
            Tension = Mathf.Clamp(Mathf.MoveTowards(Tension, target, rate * dt), 0f, 100f);

            float closing = reeling ? config.reelSpeed * (1f - Mathf.Clamp01(fishPull / config.resistPull)) : 0f;
            float pullAway = fishPull * config.pullAwayFactor;
            Distance = Mathf.Clamp(Distance - (closing - pullAway) * dt, 0f, config.maxDistance);

            if (Tension >= config.moderateTensionMin && Tension <= config.moderateTensionMax)
                Stamina -= config.staminaDrainRate * dt;
            else if (Tension < config.moderateTensionMin)
                Stamina += config.staminaRegenRate * dt;
            Stamina = Mathf.Clamp(Stamina, 0f, config.maxStamina);

            if (Tension > config.breakThreshold)
            {
                overTensionTimer += dt;
                if (overTensionTimer >= config.breakTime - 1e-4f) return FightResult.LineBroke;
            }
            else
            {
                overTensionTimer = 0f;
            }

            if (Stamina <= 0f && Distance <= config.catchDistance) return FightResult.Caught;

            if (Tension < config.slackThreshold)
            {
                slackTimer += dt;
                if (slackTimer >= config.slackTime - 1e-4f) return FightResult.FishEscaped;
            }
            else
            {
                slackTimer = 0f;
            }

            return FightResult.None;
        }
    }
}
