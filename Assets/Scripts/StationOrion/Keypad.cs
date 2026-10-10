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
        [Tooltip("Wrong codes allowed before the keypad locks itself.")]
        public int maxAttempts = 5;
        [Tooltip("How long the keypad stays locked after too many wrong codes (seconds).")]
        public float lockoutSeconds = 15f;

        public string Code { get; private set; }
        public bool Locked { get; set; } = true;
        public bool IsSolved { get; private set; }

        public System.Action Solved;
        public System.Action<string> Message;   // tells MissionManager what happened
        public System.Action LockedOut;         // too many wrong codes
        public bool IsLockedOut => Time.time < lockedUntil;

        string entry = "";
        bool busy;
        int wrongAttempts;
        float lockedUntil = -1f;
        int lastShownSecond = -1;

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
            if (IsLockedOut)
            {
                SOAudio.PlayAt(SOAudio.Error, transform.position, 0.5f);
                Message?.Invoke("Keypad locked - wait " + Mathf.CeilToInt(lockedUntil - Time.time) + " s");
                return;
            }
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
                wrongAttempts++;
                int left = maxAttempts - wrongAttempts;
                SOAudio.PlayAt(SOAudio.Error, transform.position, 0.8f);
                entry = "";
                if (left <= 0)
                {
                    wrongAttempts = 0;
                    lockedUntil = Time.time + lockoutSeconds;
                    Message?.Invoke("Too many wrong codes - keypad locked for " + Mathf.RoundToInt(lockoutSeconds) + " seconds");
                    LockedOut?.Invoke();
                }
                else
                {
                    Message?.Invoke("Wrong code (" + left + (left == 1 ? " try" : " tries") + " left before lockout). The code is on the Engine Room panel.");
                    yield return Flash("DENIED  " + left + " LEFT", Color.red);
                    Show();
                }
            }
            busy = false;
        }

        IEnumerator Flash(string text, Color c)
        {
            SetDisplay(text, c);
            yield return new WaitForSeconds(1f);
            Show();
        }

        void Update()
        {
            if (IsSolved || lockedUntil < 0f) return;
            if (IsLockedOut)
            {
                int sec = Mathf.CeilToInt(lockedUntil - Time.time);
                if (sec != lastShownSecond)
                {
                    lastShownSecond = sec;
                    SetDisplay("LOCKED  " + sec + "s", Color.Lerp(Color.red, new Color(1f, 0.6f, 0.2f), (sec % 2)));
                }
            }
            else
            {
                lockedUntil = -1f;
                lastShownSecond = -1;
                SOAudio.PlayAt(SOAudio.Beep, transform.position, 0.8f);
                Message?.Invoke("Keypad unlocked - try again");
                Show();
            }
        }

        void Show()
        {
            if (IsSolved) return;
            if (IsLockedOut) return;
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
