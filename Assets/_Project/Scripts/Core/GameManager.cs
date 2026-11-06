using System;
using UnityEngine;

namespace FishingSim
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Ready;

        public event Action<GameState, GameState> StateChanged; // (previous, current)
        public event Action AttemptReset;

        public static bool IsTransitionAllowed(GameState from, GameState to)
        {
            switch (from)
            {
                case GameState.Ready:          return to == GameState.Charging;
                case GameState.Charging:       return to == GameState.Casting || to == GameState.Ready;
                case GameState.Casting:        return to == GameState.WaitingForBite || to == GameState.Ready;
                case GameState.WaitingForBite: return to == GameState.Fighting || to == GameState.Ready;
                case GameState.Fighting:       return to == GameState.Caught || to == GameState.Escaped;
                case GameState.Caught:
                case GameState.Escaped:        return to == GameState.Ready;
                default:                       return false;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool TrySetState(GameState next)
        {
            if (next == State || !IsTransitionAllowed(State, next)) return false;
            var prev = State;
            State = next;
            StateChanged?.Invoke(prev, next);
            return true;
        }

        public void ResetAttempt()
        {
            var prev = State;
            State = GameState.Ready;
            AttemptReset?.Invoke();
            if (prev != GameState.Ready) StateChanged?.Invoke(prev, GameState.Ready);
        }
    }
}
