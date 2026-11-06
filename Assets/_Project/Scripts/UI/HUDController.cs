using UnityEngine;
using UnityEngine.UI;

namespace FishingSim
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] CastingController caster;
        [SerializeField] FishFightController fight;
        [SerializeField] FishAIController fish;
        [SerializeField] BiteSystem bite;

        [SerializeField] GameObject powerRoot;
        [SerializeField] Image powerFill;

        [SerializeField] GameObject fightRoot;
        [SerializeField] Image tensionFill;
        [SerializeField] Image staminaFill;
        [SerializeField] Text distanceLabel;
        [SerializeField] Text fishStateLabel;
        [SerializeField] Text biteLabel;

        [SerializeField] GameObject resultRoot;
        [SerializeField] Text resultLabel;
        [SerializeField] Button tryAgainButton;

        [SerializeField] Text stateLabel;
        [SerializeField] Text hintLabel;

        static readonly Color Calm = new Color(0.3f, 0.85f, 0.35f);
        static readonly Color Warning = new Color(0.95f, 0.8f, 0.15f);
        static readonly Color Danger = new Color(0.95f, 0.2f, 0.15f);

        GameManager gm;
        float biteLabelTimer;

        void Start()
        {
            gm = GameManager.Instance;
            gm.StateChanged += OnStateChanged;
            caster.ChargeChanged += OnChargeChanged;
            fight.FightUpdated += OnFightUpdated;
            bite.Bite += OnBite;
            tryAgainButton.onClick.AddListener(OnTryAgain);
            Refresh(gm.State);
        }

        void OnDestroy()
        {
            if (gm != null) gm.StateChanged -= OnStateChanged;
            if (caster != null) caster.ChargeChanged -= OnChargeChanged;
            if (fight != null) fight.FightUpdated -= OnFightUpdated;
            if (bite != null) bite.Bite -= OnBite;
        }

        void Update()
        {
            if (gm.State == GameState.Fighting) fishStateLabel.text = "Ryba: " + FishStateName(fish.State);

            if (biteLabelTimer > 0f)
            {
                biteLabelTimer -= Time.deltaTime;
                if (biteLabelTimer <= 0f) biteLabel.gameObject.SetActive(false);
            }
        }

        void OnStateChanged(GameState previous, GameState current)
        {
            Refresh(current);
        }

        void OnChargeChanged(float power)
        {
            SetBar(powerFill, power);
        }

        void OnFightUpdated(float tension01, float stamina01, float distance)
        {
            SetBar(tensionFill, tension01);
            SetBar(staminaFill, stamina01);
            tensionFill.color = tension01 < 0.6f
                ? Color.Lerp(Calm, Warning, tension01 / 0.6f)
                : Color.Lerp(Warning, Danger, (tension01 - 0.6f) / 0.3f);
            distanceLabel.text = $"Odległość: {distance:0.0} m";
        }

        void OnBite()
        {
            biteLabel.gameObject.SetActive(true);
            biteLabelTimer = 1.5f;
        }

        void OnTryAgain()
        {
            gm.ResetAttempt();
        }

        void Refresh(GameState state)
        {
            powerRoot.SetActive(state == GameState.Charging);
            fightRoot.SetActive(state == GameState.Fighting);
            resultRoot.SetActive(state == GameState.Caught || state == GameState.Escaped);

            if (state == GameState.Caught) resultLabel.text = "Ryba złowiona!";
            else if (state == GameState.Escaped)
                resultLabel.text = fight.LastResult == FightResult.LineBroke
                    ? "Ryba uciekła\nŻyłka się zerwała"
                    : "Ryba uciekła\nŻyłka była zbyt luźna";

            if (state == GameState.Ready)
            {
                biteLabelTimer = 0f;
                biteLabel.gameObject.SetActive(false);
            }

            stateLabel.text = StateName(state);
            hintLabel.text = HintFor(state);
        }

        static void SetBar(Image fill, float value01)
        {
            fill.rectTransform.localScale = new Vector3(Mathf.Clamp01(value01), 1f, 1f);
        }

        static string HintFor(GameState state)
        {
            switch (state)
            {
                case GameState.Ready: return "Przytrzymaj LPM, aby naładować rzut";
                case GameState.Charging: return "Puść LPM, aby zarzucić";
                case GameState.WaitingForBite: return "Czekasz na branie... (PPM, aby zwinąć żyłkę)";
                case GameState.Fighting: return "Trzymaj PPM, aby zwijać. Puść, by zmniejszyć napięcie. Nie dopuść do luzu!";
                default: return "";
            }
        }

        static string StateName(GameState state)
        {
            switch (state)
            {
                case GameState.Ready: return "Gotowy";
                case GameState.Charging: return "Ładowanie rzutu";
                case GameState.Casting: return "Rzut";
                case GameState.WaitingForBite: return "Oczekiwanie na branie";
                case GameState.Fighting: return "Hol";
                case GameState.Caught: return "Złowiona";
                case GameState.Escaped: return "Uciekła";
                default: return state.ToString();
            }
        }

        static string FishStateName(FishState state)
        {
            switch (state)
            {
                case FishState.Hooked: return "zacięta";
                case FishState.Struggling: return "szarpie";
                case FishState.Recovering: return "odpoczywa";
                case FishState.Exhausted: return "wyczerpana";
                case FishState.Caught: return "złowiona";
                case FishState.Escaped: return "uciekła";
                default: return "—";
            }
        }
    }
}
