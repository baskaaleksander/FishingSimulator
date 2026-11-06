using System;
using UnityEngine;

namespace FishingSim
{
    public class BiteSystem : MonoBehaviour
    {
        [SerializeField] LureController lure;
        [SerializeField] BiteConfig config;

        GameManager gm;
        float timer;
        bool armed;

        public event Action Bite;

        void Start()
        {
            gm = GameManager.Instance;
            lure.LandedInWater += OnLanded;
        }

        void OnDestroy()
        {
            if (lure != null) lure.LandedInWater -= OnLanded;
        }

        void OnLanded(Vector3 position)
        {
            timer = UnityEngine.Random.Range(config.minWait, config.maxWait);
            armed = true;
        }

        void Update()
        {
            if (!armed) return;
            if (gm.State != GameState.WaitingForBite || !lure.InWater)
            {
                armed = false;
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            armed = false;
            Bite?.Invoke();
            gm.TrySetState(GameState.Fighting);
        }
    }
}
