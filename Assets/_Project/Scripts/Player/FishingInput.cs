using UnityEngine;
using UnityEngine.InputSystem;

namespace FishingSim
{
    public class FishingInput : MonoBehaviour
    {
        public InputAction Look { get; private set; }
        public InputAction Cast { get; private set; }
        public InputAction Reel { get; private set; }
        public InputAction Pause { get; private set; }

        void Awake()
        {
            Look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            Cast = new InputAction("Cast", InputActionType.Button, "<Mouse>/leftButton");
            Reel = new InputAction("Reel", InputActionType.Button, "<Mouse>/rightButton");
            Pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        }

        void OnEnable()
        {
            Look.Enable(); Cast.Enable(); Reel.Enable(); Pause.Enable();
        }

        void OnDisable()
        {
            Look.Disable(); Cast.Disable(); Reel.Disable(); Pause.Disable();
        }

        void OnDestroy()
        {
            Look?.Dispose(); Cast?.Dispose(); Reel?.Dispose(); Pause?.Dispose();
        }
    }
}
