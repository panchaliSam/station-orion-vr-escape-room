using UnityEngine;

namespace StationOrion
{
    /// <summary>
    /// Generates all game sounds in code (no audio files needed), so every sound is
    /// original work and needs no licence. Every sound is played as 3D (spatial) audio.
    /// </summary>
    public static class SOAudio
    {
        const int Rate = 44100;

        static AudioClip beep, success, error, clunk, alarm, door, hum, key;

        public static AudioClip Beep    { get { if (beep == null)    beep = MakeTone("SO_Beep", 0.12f, 880f, false, 0.5f); return beep; } }
        public static AudioClip Key     { get { if (key == null)     key = MakeTone("SO_Key", 0.06f, 1200f, false, 0.4f); return key; } }
        public static AudioClip Error   { get { if (error == null)   error = MakeTone("SO_Error", 0.35f, 140f, true, 0.35f); return error; } }
        public static AudioClip Success { get { if (success == null) success = MakeSuccess(); return success; } }
        public static AudioClip Clunk   { get { if (clunk == null)   clunk = MakeClunk(); return clunk; } }
        public static AudioClip Alarm   { get { if (alarm == null)   alarm = MakeAlarm(); return alarm; } }
        public static AudioClip Door    { get { if (door == null)    door = MakeDoor(); return door; } }
        public static AudioClip Hum     { get { if (hum == null)     hum = MakeHum(); return hum; } }

        /// <summary>Plays a one-shot 3D sound at a position in the world.</summary>
        public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            var go = new GameObject("SO_OneShot_" + clip.name);
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = volume;
            src.spatialBlend = 1f;          // fully 3D
            src.minDistance = 1f;
            src.maxDistance = 20f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.Play();
            Object.Destroy(go, clip.length + 0.1f);
        }

        /// <summary>Adds a looping 3D sound to an object (alarm, reactor hum).</summary>
        public static AudioSource AddLoop(GameObject host, AudioClip clip, float volume)
        {
            var src = host.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.volume = volume;
            src.spatialBlend = 1f;
            src.minDistance = 1.5f;
            src.maxDistance = 15f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.playOnAwake = false;
            return src;
        }

        // ---------- sound generators ----------

        static AudioClip MakeTone(string name, float seconds, float freq, bool square, float volume)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = Mathf.Sin(2f * Mathf.PI * freq * t);
                if (square) s = Mathf.Sign(s);
                data[i] = s * volume * Envelope(i, n);
            }
            return Build(name, data);
        }

        static AudioClip MakeSuccess()
        {
            float[] notes = { 660f, 880f, 1320f };
            int per = Mathf.CeilToInt(0.13f * Rate);
            var data = new float[per * notes.Length];
            for (int k = 0; k < notes.Length; k++)
                for (int i = 0; i < per; i++)
                {
                    float t = i / (float)Rate;
                    data[k * per + i] = Mathf.Sin(2f * Mathf.PI * notes[k] * t) * 0.45f * Envelope(i, per);
                }
            return Build("SO_Success", data);
        }

        static AudioClip MakeClunk()
        {
            int n = Mathf.CeilToInt(0.18f * Rate);
            var data = new float[n];
            var rng = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float decay = Mathf.Exp(-t * 30f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.3f;
                data[i] = (Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.8f + noise) * decay * 0.7f;
            }
            return Build("SO_Clunk", data);
        }

        static AudioClip MakeAlarm()
        {
            // two-tone siren, 1.2 s, loops cleanly
            int n = Mathf.CeilToInt(1.2f * Rate);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float f = i < n / 2 ? 720f : 520f;
                phase += 2f * Mathf.PI * f / Rate;
                data[i] = Mathf.Sin(phase) * 0.35f;
            }
            return Build("SO_Alarm", data);
        }

        static AudioClip MakeDoor()
        {
            int n = Mathf.CeilToInt(0.9f * Rate);
            var data = new float[n];
            var rng = new System.Random(3);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * 0.05f;   // simple low-pass = "whoosh"
                data[i] = lp * 1.6f * Envelope(i, n);
            }
            return Build("SO_Door", data);
        }

        static AudioClip MakeHum()
        {
            int n = 2 * Rate; // 2 s, whole number of cycles -> seamless loop
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                data[i] = (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.25f) * 0.4f;
            }
            return Build("SO_Hum", data);
        }

        static float Envelope(int i, int n)
        {
            int fade = Mathf.Min(n / 4, Rate / 100);  // ~10 ms fade in/out, no clicks
            if (i < fade) return i / (float)fade;
            if (i > n - fade) return (n - i) / (float)fade;
            return 1f;
        }

        static AudioClip Build(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
