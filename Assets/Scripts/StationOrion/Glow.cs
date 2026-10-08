using UnityEngine;

namespace StationOrion
{
    /// <summary>
    /// Makes an object's material glow (emission). Can be steady or pulsing.
    /// Used to show "this is important / do this next" (visual affordance + feedback).
    /// </summary>
    [DisallowMultipleComponent]
    public class Glow : MonoBehaviour
    {
        public Color color = Color.cyan;
        [Tooltip("How bright the glow is at full strength.")]
        public float intensity = 2f;
        public bool startOn = false;
        public bool pulse = false;
        public float pulseSpeed = 4f;

        Material mat;
        bool isOn;

        void Awake()
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                mat = rend.material; // per-object copy, so other objects are not affected
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            isOn = startOn;
            Apply(1f);
        }

        /// <summary>Turn the glow on with a colour, steady or pulsing.</summary>
        public void Set(Color c, bool pulsing)
        {
            color = c;
            isOn = true;
            pulse = pulsing;
            Apply(1f);
        }

        public void Off()
        {
            isOn = false;
            pulse = false;
            Apply(0f);
        }

        public void SetBaseColor(Color c)
        {
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            else mat.color = c;
        }

        void Update()
        {
            if (isOn && pulse)
            {
                float k = 0.25f + 0.75f * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
                Apply(k);
            }
        }

        void Apply(float k)
        {
            if (mat == null) return;
            mat.SetColor("_EmissionColor", isOn ? color * intensity * k : Color.black);
        }
    }
}
