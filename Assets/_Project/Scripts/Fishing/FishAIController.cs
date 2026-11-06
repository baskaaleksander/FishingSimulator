using UnityEngine;

namespace FishingSim
{
    public class FishAIController : MonoBehaviour
    {
        [SerializeField] FishConfig config;

        FishStateMachine machine;

        public FishState State => machine != null ? machine.State : FishState.Idle;
        public float PullForce => machine != null ? machine.PullForce : 0f;

        public void Begin()
        {
            machine = new FishStateMachine(config, new System.Random());
            machine.Hook();
        }

        public float Tick(float dt, float stamina)
        {
            return machine != null ? machine.Tick(dt, stamina) : 0f;
        }

        public void Finish(bool caught)
        {
            if (machine == null) return;
            if (caught) machine.MarkCaught();
            else machine.MarkEscaped();
        }

        public void Clear()
        {
            machine = null;
        }
    }
}
