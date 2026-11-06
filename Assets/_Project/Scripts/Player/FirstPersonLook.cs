using UnityEngine;

namespace FishingSim
{
    public class FirstPersonLook : MonoBehaviour
    {
        [SerializeField] FishingInput input;
        [SerializeField] Transform cameraTransform;
        [SerializeField] float sensitivity = 0.1f; // degrees per mouse pixel
        [SerializeField] float maxPitch = 80f;

        float yaw, pitch;

        public bool LookEnabled { get; private set; } = true;

        void Start()
        {
            yaw = transform.eulerAngles.y;
            SetCursorLocked(true);
        }

        void Update()
        {
            if (!LookEnabled) return;
            Vector2 d = input.Look.ReadValue<Vector2>() * sensitivity;
            yaw += d.x;
            pitch = Mathf.Clamp(pitch - d.y, -maxPitch, maxPitch);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void SetCursorLocked(bool locked)
        {
            LookEnabled = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
