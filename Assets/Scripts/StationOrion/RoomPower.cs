using System.Collections;
using UnityEngine;

namespace StationOrion
{
    /// <summary>
    /// Controls all the lights of one room (real lights + glowing ceiling panels).
    /// Emergency mode = dimmed. PowerUp() flickers the lights back on like a real
    /// power restore (strong visual feedback that the task worked).
    /// </summary>
    public class RoomPower : MonoBehaviour
    {
        public Light[] lights = new Light[0];
        public Glow[] panels = new Glow[0];
        public Color panelColor = new Color(0.9f, 0.95f, 1f);
        [Range(0f, 1f)] public float dimLevel = 0.2f;

        float[] baseIntensity;
        public bool Powered { get; private set; } = true;

        void Init()
        {
            if (baseIntensity != null && baseIntensity.Length == lights.Length) return;
            baseIntensity = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
                baseIntensity[i] = lights[i] != null ? lights[i].intensity : 0f;
        }

        void Awake() { Init(); }

        public void SetPowered(bool on) { Init(); Powered = on; Apply(on ? 1f : dimLevel); }

        void Apply(float k)
        {
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].intensity = baseIntensity[i] * k;
            foreach (var p in panels)
                if (p != null) p.Set(panelColor * Mathf.Max(k, 0.05f), false);
        }

        /// <summary>Flicker on, then stay on.</summary>
        public void PowerUp()
        {
            Init();
            if (isActiveAndEnabled) StartCoroutine(Flicker());
            else SetPowered(true);
        }

        IEnumerator Flicker()
        {
            float[] pattern = { 1f, 0.1f, 0.8f, 0.05f, 1f, 0.3f, 1f };
            foreach (var k in pattern)
            {
                Apply(k);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.14f));
            }
            SetPowered(true);
        }
    }
}
