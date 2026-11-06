using UnityEngine;
using UnityEngine.UI;

namespace FishingSim
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] CastingController caster;
        [SerializeField] GameObject powerRoot;
        [SerializeField] Image powerFill;
        [SerializeField] Text stateLabel;
        [SerializeField] Text hintLabel;

        GameManager gm;

        void Start()
        {
            gm = GameManager.Instance;
            gm.StateChanged += OnStateChanged;
            caster.ChargeChanged += OnChargeChanged;
            Refresh(gm.State);
        }

        void OnDestroy()
        {
            if (gm != null) gm.StateChanged -= OnStateChanged;
            if (caster != null) caster.ChargeChanged -= OnChargeChanged;
        }

        void OnStateChanged(GameState previous, GameState current)
        {
            Refresh(current);
        }

        void OnChargeChanged(float power)
        {
            powerFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(power), 1f, 1f);
        }

        void Refresh(GameState state)
        {
            powerRoot.SetActive(state == GameState.Charging);
            stateLabel.text = state.ToString();
            hintLabel.text = HintFor(state);
        }

        static string HintFor(GameState state)
        {
            switch (state)
            {
                case GameState.Ready: return "Hold LMB to charge the cast";
                case GameState.Charging: return "Release LMB to throw";
                case GameState.WaitingForBite: return "Waiting for a bite... (RMB to reel in)";
                default: return "";
            }
        }
    }
}
