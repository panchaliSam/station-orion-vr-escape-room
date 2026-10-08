using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StationOrion
{
    /// <summary>
    /// The "brain" of Station Orion. It runs the story from start to finish:
    ///   Intro -> Power -> Coolant -> Access (keycard) -> Code (keypad) -> Shutdown (button) -> Escape -> Complete
    /// It owns the 10-minute reactor timer, the wall screens, the alarm, room lights,
    /// doors, which floors can be teleported to, the objective marker and the endings.
    /// All references are filled automatically by  Station Orion > Build Game Systems.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public enum Stage { Intro, Power, Coolant, Access, Code, Shutdown, Escape, Complete, Failed }

        [Header("Timing")]
        public float missionSeconds = 600f;
        public float introSeconds = 7f;
        [Tooltip("Show a hint if the player makes no progress for this long.")]
        public float hintAfterIdle = 25f;

        [Header("Screens (world-space text)")]
        public TextMeshPro[] displays = new TextMeshPro[0];
        public TextMeshPro codeNote;

        [Header("Tasks")]
        public SocketTask powerSocket;
        public XRGrabInteractable powerCell;
        public SocketTask[] coolantSockets = new SocketTask[0];
        public XRGrabInteractable[] canisters = new XRGrabInteractable[0];   // same order as coolantSockets
        public SocketTask keycardSocket;
        public XRGrabInteractable keycard;
        public Keypad keypad;
        public PressButton emergencyButton;

        [Header("World")]
        public DoorController door1;
        public DoorController door2;
        public Behaviour[] room2Teleport = new Behaviour[0];   // TeleportationArea components, locked at start
        public Behaviour[] room3Teleport = new Behaviour[0];
        public Collider escapePodZone;
        public Glow escapePodGlow;
        public Glow reactorCore;
        public Spin reactorRing;
        public Transform objectiveMarker;

        [Header("Lights")]
        public Light[] alarmLights = new Light[0];
        public Light room1Light, room2Light, room3Light, room3Danger, room3Success;

        public Stage CurrentStage { get; private set; } = Stage.Intro;

        float timeLeft;
        float lastProgress;
        string banner = "";
        float bannerUntil;
        string hint = "";
        string lastText = "";
        readonly Dictionary<Light, float> baseIntensity = new Dictionary<Light, float>();
        readonly List<AudioSource> alarmSources = new List<AudioSource>();
        AudioSource humSource;
        Glow glowingItem;
        Transform cam;

        // ------------------------------------------------------------ setup

        void Start()
        {
            timeLeft = missionSeconds;
            lastProgress = Time.time;

            foreach (var l in new[] { room1Light, room2Light, room3Light, room3Danger, room3Success })
                if (l != null) baseIntensity[l] = l.intensity;
            foreach (var l in alarmLights)
                if (l != null) baseIntensity[l] = l.intensity;

            // Emergency lighting: rooms dim, red alarm lights on.
            Dim(room1Light, 0.3f); Dim(room2Light, 0.3f); Dim(room3Light, 0.3f);
            if (room3Success != null) room3Success.gameObject.SetActive(false);

            // Lock the next rooms: you cannot teleport there until their door opens.
            SetTeleport(room2Teleport, false);
            SetTeleport(room3Teleport, false);

            // Alarm siren from every alarm light (3D audio guides the player's attention).
            foreach (var l in alarmLights)
            {
                if (l == null) continue;
                var src = SOAudio.AddLoop(l.gameObject, SOAudio.Alarm, 0.25f);
                src.Play();
                alarmSources.Add(src);
            }
            if (reactorCore != null)
            {
                humSource = SOAudio.AddLoop(reactorCore.gameObject, SOAudio.Hum, 0.5f);
                humSource.Play();
                reactorCore.Set(new Color(1f, 0.25f, 0.1f), true);
            }
            if (escapePodGlow != null) escapePodGlow.Set(new Color(1f, 0.5f, 0.1f) * 0.4f, false);

            // Listen to every task.
            if (powerSocket != null) powerSocket.Filled += OnSocketFilled;
            foreach (var s in coolantSockets) if (s != null) { s.Filled += OnSocketFilled; s.Emptied += OnSocketEmptied; }
            if (keycardSocket != null) keycardSocket.Filled += OnSocketFilled;
            foreach (var s in AllSockets()) if (s != null) s.WrongItem += OnWrongItem;
            if (keypad != null) { keypad.Solved += OnKeypadSolved; keypad.Message += m => ShowBanner(m, 4f); }
            if (emergencyButton != null) emergencyButton.Pressed += OnEmergencyPressed;

            if (codeNote != null) codeNote.text = "REACTOR SHUTDOWN CODE\n<color=#888888>[ ENCRYPTED ]\nStabilise the coolant to decode</color>";

            StartCoroutine(IntroThenStart());
        }

        IEnumerator IntroThenStart()
        {
            CurrentStage = Stage.Intro;
            UpdateDisplays(true);
            yield return new WaitForSeconds(introSeconds);
            EnterStage(Stage.Power);
        }

        IEnumerable<SocketTask> AllSockets()
        {
            if (powerSocket != null) yield return powerSocket;
            foreach (var s in coolantSockets) if (s != null) yield return s;
            if (keycardSocket != null) yield return keycardSocket;
        }

        // ------------------------------------------------------------ story

        void EnterStage(Stage next)
        {
            CurrentStage = next;
            lastProgress = Time.time;
            hint = "";

            switch (next)
            {
                case Stage.Power:
                    ShowBanner("Mission started. Follow the glowing diamond.", 5f);
                    if (powerSocket != null && powerSocket.IsFilled) { EnterStage(Stage.Coolant); return; }
                    break;

                case Stage.Coolant:
                    SOAudio.PlayAt(SOAudio.Success, Head(), 0.8f);
                    ShowBanner("POWER RESTORED - Engine Room unlocked", 5f);
                    Restore(room1Light);
                    if (door1 != null) door1.Open();
                    SetTeleport(room2Teleport, true);
                    if (CoolantFilled() == coolantSockets.Length) { EnterStage(Stage.Access); return; }
                    break;

                case Stage.Access:
                    SOAudio.PlayAt(SOAudio.Success, Head(), 0.8f);
                    ShowBanner("COOLANT STABLE - shutdown code decoded on the wall", 6f);
                    Restore(room2Light);
                    if (codeNote != null && keypad != null)
                        codeNote.text = "REACTOR SHUTDOWN CODE\n<size=170%><color=#66FFAA>" + Spaced(keypad.Code) + "</color></size>\nRemember it!";
                    if (keycardSocket != null && keycardSocket.IsFilled) { EnterStage(Stage.Code); return; }
                    break;

                case Stage.Code:
                    SOAudio.PlayAt(SOAudio.Success, Head(), 0.8f);
                    ShowBanner("ACCESS GRANTED - Reactor Room unlocked", 5f);
                    if (door2 != null) door2.Open();
                    SetTeleport(room3Teleport, true);
                    if (keypad != null) keypad.Unlock();
                    break;

                case Stage.Shutdown:
                    ShowBanner("CODE ACCEPTED - press the EMERGENCY SHUTDOWN button", 6f);
                    var bg = emergencyButton != null ? emergencyButton.GetComponent<Glow>() : null;
                    if (bg != null) bg.Set(Color.red, true);
                    break;

                case Stage.Escape:
                    SOAudio.PlayAt(SOAudio.Success, Head(), 1f);
                    ShowBanner("REACTOR STABLE - get into the ESCAPE POD!", 8f);
                    StopAlarm();
                    Restore(room3Light);
                    if (room3Danger != null) room3Danger.gameObject.SetActive(false);
                    if (room3Success != null) room3Success.gameObject.SetActive(true);
                    if (reactorCore != null) reactorCore.Set(new Color(0.2f, 0.7f, 1f), false);
                    if (reactorRing != null) reactorRing.speedMultiplier = 0.25f;
                    if (humSource != null) humSource.pitch = 0.6f;
                    if (escapePodGlow != null) escapePodGlow.Set(new Color(0.2f, 1f, 0.4f), true);
                    var eb = emergencyButton != null ? emergencyButton.GetComponent<Glow>() : null;
                    if (eb != null) eb.Set(new Color(0.2f, 1f, 0.3f), false);
                    break;

                case Stage.Complete:
                    SOAudio.PlayAt(SOAudio.Success, Head(), 1f);
                    if (escapePodGlow != null) escapePodGlow.Set(new Color(0.2f, 1f, 0.4f), false);
                    break;

                case Stage.Failed:
                    StartCoroutine(RestartAfter(10f));
                    break;
            }
            UpdateTargets();
            UpdateDisplays(true);
        }

        void OnSocketFilled(SocketTask s)
        {
            lastProgress = Time.time;
            if (CurrentStage == Stage.Power && s == powerSocket) EnterStage(Stage.Coolant);
            else if (CurrentStage == Stage.Coolant && System.Array.IndexOf(coolantSockets, s) >= 0)
            {
                int n = CoolantFilled();
                if (n == coolantSockets.Length) EnterStage(Stage.Access);
                else { ShowBanner("Coolant " + n + "/" + coolantSockets.Length + " connected", 3f); UpdateTargets(); }
            }
            else if (CurrentStage == Stage.Access && s == keycardSocket) EnterStage(Stage.Code);
            else if (CurrentStage == Stage.Coolant && s == keycardSocket)
                ShowBanner("Scanner: card read, but coolant must be stable first", 4f);
            UpdateDisplays(true);
        }

        void OnSocketEmptied(SocketTask s)
        {
            if (CurrentStage == Stage.Coolant) UpdateTargets();
            UpdateDisplays(true);
        }

        void OnWrongItem(SocketTask s)
        {
            ShowBanner("That does not fit here. Match the item to the glowing slot of the same colour.", 4f);
        }

        void OnKeypadSolved()
        {
            lastProgress = Time.time;
            if (CurrentStage == Stage.Code) EnterStage(Stage.Shutdown);
        }

        void OnEmergencyPressed(PressButton b)
        {
            if (CurrentStage == Stage.Shutdown) EnterStage(Stage.Escape);
            else if (CurrentStage < Stage.Shutdown)
            {
                SOAudio.PlayAt(SOAudio.Error, b.transform.position, 0.8f);
                ShowBanner("SHUTDOWN LOCKED - enter the code on the keypad first", 4f);
            }
        }

        int CoolantFilled()
        {
            int n = 0;
            foreach (var s in coolantSockets) if (s != null && s.IsFilled) n++;
            return n;
        }

        // ------------------------------------------------------------ every frame

        void Update()
        {
            bool running = CurrentStage >= Stage.Power && CurrentStage <= Stage.Escape;
            if (running)
            {
                timeLeft -= Time.deltaTime;
                if (timeLeft <= 0f) { timeLeft = 0f; EnterStage(Stage.Failed); }
            }

            if (CurrentStage == Stage.Escape && escapePodZone != null && HeadTransform() != null)
            {
                Bounds b = escapePodZone.bounds;
                b.Expand(new Vector3(0.1f, 4f, 0.1f));
                if (b.Contains(HeadTransform().position)) EnterStage(Stage.Complete);
            }

            // Idle hint: if nothing happened for a while, help the player.
            if (running && CurrentStage != Stage.Escape && Time.time - lastProgress > hintAfterIdle)
            {
                lastProgress = Time.time;
                hint = HintFor(CurrentStage);
                if (objectiveMarker != null) SOAudio.PlayAt(SOAudio.Beep, objectiveMarker.position, 1f);
            }

            PulseAlarm(running);
            MoveMarker();
            UpdateDisplays(false);

            var kb = Keyboard.current;
            if (kb != null && kb.f5Key.wasPressedThisFrame) Restart();
        }

        void PulseAlarm(bool running)
        {
            if (CurrentStage >= Stage.Escape && CurrentStage != Stage.Failed) return;
            float speed = (timeLeft < 60f || CurrentStage == Stage.Failed) ? 9f : 4f;
            float k = 0.15f + 0.85f * Mathf.Abs(Mathf.Sin(Time.time * speed * 0.5f));
            foreach (var l in alarmLights)
                if (l != null && baseIntensity.TryGetValue(l, out float b)) l.intensity = b * k;
        }

        // ------------------------------------------------------------ objective marker + glow

        Transform currentTarget;

        void UpdateTargets()
        {
            foreach (var s in AllSockets()) s.SetTargeted(false);
            switch (CurrentStage)
            {
                case Stage.Power: if (powerSocket != null) powerSocket.SetTargeted(true); break;
                case Stage.Coolant: foreach (var s in coolantSockets) if (s != null && !s.IsFilled) s.SetTargeted(true); break;
                case Stage.Access: if (keycardSocket != null) keycardSocket.SetTargeted(true); break;
            }
        }

        Transform PickTarget()
        {
            switch (CurrentStage)
            {
                case Stage.Power: return ItemOrSocket(powerCell, powerSocket);
                case Stage.Coolant:
                    for (int i = 0; i < coolantSockets.Length; i++)
                        if (coolantSockets[i] != null && !coolantSockets[i].IsFilled)
                            return ItemOrSocket(i < canisters.Length ? canisters[i] : null, coolantSockets[i]);
                    return null;
                case Stage.Access:
                    if (codeNote != null && !seenCode) return codeNote.transform;
                    return ItemOrSocket(keycard, keycardSocket);
                case Stage.Code: return keypad != null ? keypad.transform : null;
                case Stage.Shutdown: return emergencyButton != null ? emergencyButton.transform : null;
                case Stage.Escape: return escapePodZone != null ? escapePodZone.transform : null;
            }
            return null;
        }

        bool seenCode;

        // Point at the item until the player picks it up, then point at where it goes.
        Transform ItemOrSocket(XRGrabInteractable item, SocketTask socket)
        {
            if (item == null) return socket != null ? socket.transform : null;
            if (item.isSelected && !InSocket(item)) return socket != null ? socket.transform : null;
            return item.transform;
        }

        static bool InSocket(XRGrabInteractable item)
        {
            foreach (var i in item.interactorsSelecting)
                if (i is XRSocketInteractor) return true;
            return false;
        }

        void MoveMarker()
        {
            // the code note counts as "seen" once the player gets close to it
            if (CurrentStage == Stage.Access && !seenCode && codeNote != null && HeadTransform() != null &&
                Vector3.Distance(HeadTransform().position, codeNote.transform.position) < 2.2f)
                seenCode = true;

            Transform t = (CurrentStage >= Stage.Power && CurrentStage <= Stage.Escape) ? PickTarget() : null;

            // make the current item glow
            Glow g = null;
            if (t != null && (CurrentStage == Stage.Power || CurrentStage == Stage.Coolant || CurrentStage == Stage.Access))
                g = t.GetComponent<Glow>();
            if (g != glowingItem)
            {
                if (glowingItem != null) glowingItem.Off();
                glowingItem = g;
                if (glowingItem != null) glowingItem.Set(glowingItem.color, true);
            }

            if (objectiveMarker == null) return;
            if (t == null) { objectiveMarker.gameObject.SetActive(false); return; }
            objectiveMarker.gameObject.SetActive(true);
            float lift = t == (escapePodZone != null ? escapePodZone.transform : null) ? 1.4f : 0.35f;
            Vector3 goal = t.position + Vector3.up * lift;
            if (t != currentTarget && currentTarget == null) objectiveMarker.position = goal;
            currentTarget = t;
            objectiveMarker.position = Vector3.Lerp(objectiveMarker.position, goal, Time.deltaTime * 6f);
        }

        // ------------------------------------------------------------ screens

        void ShowBanner(string text, float seconds)
        {
            banner = text;
            bannerUntil = Time.time + seconds;
            UpdateDisplays(true);
        }

        void UpdateDisplays(bool force)
        {
            if (displays == null) return;
            string text = Compose();
            if (!force && text == lastText) return;
            lastText = text;
            foreach (var d in displays) if (d != null) d.text = text;
        }

        string Compose()
        {
            string timer = FormatTime(timeLeft);
            string timerColor = timeLeft < 60f ? "#FF3333" : "#FFB347";

            if (CurrentStage == Stage.Intro)
                return "<size=130%><b>STATION ORION</b></size>\n" +
                       "<color=#FF4444><b>WARNING: REACTOR OVERLOAD</b></color>\n\n" +
                       "You are the last engineer on board.\n" +
                       "Repair the station and reach the escape pod\nbefore the reactor melts down.\n\n" +
                       "<color=#88CCFF>Starting in a moment...</color>";

            if (CurrentStage == Stage.Complete)
                return "<size=130%><color=#55FF88><b>MISSION COMPLETE</b></color></size>\n\n" +
                       "You escaped Station Orion with\n<size=150%><b>" + timer + "</b></size> to spare.\n\n" +
                       "<size=80%><color=#AAAAAA>Thanks for playing. Press F5 to play again.</color></size>";

            if (CurrentStage == Stage.Failed)
                return "<size=130%><color=#FF3333><b>REACTOR MELTDOWN</b></color></size>\n\n" +
                       "The station was lost.\nRestarting in a few seconds...";

            string s = "<color=" + timerColor + "><size=150%><b>" + timer + "</b></size></color>\n";
            if (Time.time < bannerUntil && banner != "") s += "<color=#FFFF66>" + banner + "</color>\n";
            s += "\n<b>OBJECTIVE</b>\n" + Objective(CurrentStage) + "\n\n" + Checklist();
            if (hint != "") s += "\n<size=85%><color=#88DDFF>HINT: " + hint + "</color></size>";
            return s;
        }

        string Objective(Stage st)
        {
            switch (st)
            {
                case Stage.Power: return "Restore power: put the glowing <color=#FFE14D>YELLOW power cell</color> on the yellow slot.";
                case Stage.Coolant: return "Cool the reactor: put each canister on the pad of the <b>same colour</b> (" + CoolantFilled() + "/" + coolantSockets.Length + ").";
                case Stage.Access: return "Read the shutdown code on the wall, then place the <color=#7FE8FF>ID keycard</color> on the scanner by the next door.";
                case Stage.Code: return "Enter the 4-digit shutdown code on the keypad in the Reactor Room.";
                case Stage.Shutdown: return "Press the red <color=#FF5555>EMERGENCY SHUTDOWN</color> button.";
                case Stage.Escape: return "Reactor stable! Get into the <color=#55FF88>ESCAPE POD</color>.";
            }
            return "";
        }

        string HintFor(Stage st)
        {
            switch (st)
            {
                case Stage.Power: return "The power cell is on the shelf. Point at it and hold GRIP, then let go above the yellow slot.";
                case Stage.Coolant: return "Red goes on red, blue on blue, green on green. Follow the diamond.";
                case Stage.Access: return "The keycard is on a crate in a corner. The scanner glows blue next to the next door.";
                case Stage.Code: return "Forgot the code? Go back to the Engine Room and read the wall panel.";
                case Stage.Shutdown: return "Point at the big red button and press GRIP.";
            }
            return "";
        }

        string Checklist()
        {
            return Line("Power", CurrentStage > Stage.Power) +
                   Line("Coolant " + CoolantFilled() + "/" + (coolantSockets != null ? coolantSockets.Length : 3), CurrentStage > Stage.Coolant) +
                   Line("Access", CurrentStage > Stage.Access) +
                   Line("Shutdown code", CurrentStage > Stage.Code) +
                   Line("Reactor", CurrentStage > Stage.Shutdown);
        }

        static string Line(string label, bool done)
        {
            return done ? "<color=#55FF88>[DONE]  " + label + "</color>\n" : "<color=#AAAAAA>[ -- ]  " + label + "</color>\n";
        }

        static string FormatTime(float t)
        {
            int m = Mathf.FloorToInt(t / 60f);
            int s = Mathf.FloorToInt(t % 60f);
            return m.ToString("00") + ":" + s.ToString("00");
        }

        static string Spaced(string code)
        {
            string r = "";
            foreach (char c in code) r += c + " ";
            return r.Trim();
        }

        // ------------------------------------------------------------ helpers

        Transform HeadTransform()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            return cam;
        }

        Vector3 Head() { var h = HeadTransform(); return h != null ? h.position : transform.position; }

        void Dim(Light l, float k) { if (l != null && baseIntensity.TryGetValue(l, out float b)) l.intensity = b * k; }
        void Restore(Light l) { if (l != null && baseIntensity.TryGetValue(l, out float b)) l.intensity = b; }

        static void SetTeleport(Behaviour[] areas, bool on)
        {
            if (areas == null) return;
            foreach (var a in areas) if (a != null) a.enabled = on;
        }

        void StopAlarm()
        {
            foreach (var src in alarmSources) if (src != null) src.Stop();
            foreach (var l in alarmLights) if (l != null) l.enabled = false;
        }

        IEnumerator RestartAfter(float seconds)
        {
            foreach (var src in alarmSources) if (src != null) src.pitch = 1.4f;
            yield return new WaitForSeconds(seconds);
            Restart();
        }

        public void Restart()
        {
            // The setup tool adds this scene to File > Build Profiles, which is required for reloading.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
