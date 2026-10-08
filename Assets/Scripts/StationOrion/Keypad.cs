using System.Collections;
using TMPro;
using UnityEngine;

namespace StationOrion
{
    /// <summary>
    /// 4-digit code keypad. A new random code is made every game; the MissionManager
    /// reveals it in the Engine Room after the coolant task, so the player has to
    /// remember it (or go back and look).
    /// </summary>
    public class Keypad : MonoBehaviour
    {
        public TextMeshPro display;
        public PressButton[] keys;
        public int codeLength = 4;
        [Tooltip("Leave empty for a random code each game.")]
        public string fixedCode = "";

        public string Code { get; private set; }
        public bool Locked { get; set; } = true;
        public bool IsSolved { get; private set; }

        public System.Action Solved;
        public System.Action<string> Message;   // tells MissionManager what happened

        string entry = "";
        bool busy;

        void Awake()
        {
            if (!string.IsNullOrEmpty(fixedCode)) Code = fixedCode;
            else
            {
                Code = "";
                for (int i = 0; i < codeLength; i++) Code += Random.Range(1, 10).ToString();
            }
        }

        void Start()
        {
            foreach (var k in keys)
                if (k != null) k.Pressed += OnKey;
            Show();
        }

        void OnKey(PressButton b)
        {
            if (IsSolved || busy) return;
            if (Locked)
            {
                SOAudio.PlayAt(SOAudio.Error, transform.position, 0.6f);
                StartCoroutine(Flash("LOCKED", Color.red));
                Message?.Invoke("Keypad is locked. Finish the earlier steps first.");
                return;
            }
            if (b.id == "C") { entry = ""; Show(); return; }
            if (entry.Length < codeLength) entry += b.id;
            Show();
            if (entry.Length == codeLength) StartCoroutine(Check());
        }

        IEnumerator Check()
        {
            busy = true;
            yield return new WaitForSeconds(0.3f);
            if (entry == Code)
            {
                IsSolved = true;
                SOAudio.PlayAt(SOAudio.Success, transform.position, 0.9f);
                SetDisplay("ACCEPTED", new Color(0.2f, 1f, 0.3f));
                Solved?.Invoke();
            }
            else
            {
                SOAudio.PlayAt(SOAudio.Error, transform.position, 0.8f);
                Message?.Invoke("Wrong code. The code was shown in the Engine Room.");
                yield return Flash("DENIED", Color.red);
                entry = "";
                Show();
            }
            busy = false;
        }

        IEnumerator Flash(string text, Color c)
        {
            SetDisplay(text, c);
            yield return new WaitForSeconds(1f);
            Show();
        }

        void Show()
        {
            if (IsSolved) return;
            if (Locked) { SetDisplay("LOCKED", new Color(1f, 0.4f, 0.3f)); return; }
            string s = "";
            for (int i = 0; i < codeLength; i++) s += i < entry.Length ? entry[i] + " " : "_ ";
            SetDisplay(s.Trim(), new Color(0.4f, 0.9f, 1f));
        }

        /// <summary>Called by MissionManager when the keypad becomes usable.</summary>
        public void Unlock()
        {
            Locked = false;
            entry = "";
            Show();
        }

        void SetDisplay(string text, Color c)
        {
            if (display == null) return;
            display.text = text;
            display.color = c;
        }
    }
}
