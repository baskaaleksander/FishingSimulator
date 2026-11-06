using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FishingSim
{
    public class FpsCounter : MonoBehaviour
    {
        [SerializeField] Text label;
        [SerializeField] float refreshInterval = 0.5f;

        float intervalTime;
        int intervalFrames;
        float totalTime;
        int totalFrames;
        float minFps = float.MaxValue;

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                label.gameObject.SetActive(!label.gameObject.activeSelf);

            float dt = Time.unscaledDeltaTime;
            intervalTime += dt;
            intervalFrames++;
            totalTime += dt;
            totalFrames++;

            if (intervalTime < refreshInterval) return;

            float fps = intervalFrames / intervalTime;
            minFps = Mathf.Min(minFps, fps);
            label.text = $"{fps:0} FPS (avg {AverageFps:0}, min {minFps:0})";
            intervalTime = 0f;
            intervalFrames = 0;
        }

        float AverageFps => totalTime > 0f ? totalFrames / totalTime : 0f;

        void OnApplicationQuit()
        {
            Debug.Log($"[FPS] average {AverageFps:0.0}, lowest interval {minFps:0.0}, frames {totalFrames}, time {totalTime:0.0}s");
        }
    }
}
