using UnityEngine;

namespace FishingSim
{
    [CreateAssetMenu(menuName = "Fishing/Bite Config", fileName = "BiteConfig")]
    public class BiteConfig : ScriptableObject
    {
        public float minWait = 3f;
        public float maxWait = 10f;
    }
}
