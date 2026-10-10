using UnityEngine;

namespace StationOrion
{
    /// <summary>Plays a quiet looping 3D machine hum from this object (ventilation, generator...).</summary>
    public class AmbientLoop : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 0.2f;
        public float pitch = 1f;
        public float maxDistance = 10f;

        void Start()
        {
            var src = SOAudio.AddLoop(gameObject, SOAudio.Hum, volume);
            src.pitch = pitch;
            src.maxDistance = maxDistance;
            src.Play();
        }
    }
}
