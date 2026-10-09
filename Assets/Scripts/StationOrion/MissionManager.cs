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
        public enum Stage { Intro, Power, Coolant, Access, Code, Shutdown, Escape, Boarded, Launching, Complete, Failed }

        [Header("Timing")]
        public float missionSeconds = 300f;   // 5 minutes
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

        [Header("Realistic station (optional)")]
        public RoomPower room1Power, room2Power, room3Power;
        public DoorController podDoor;
        public Behaviour[] podTeleport = new Behaviour[0];   // escape pod floor, unlocked at the end

        [Header("Ending (escape pod)")]
        public PressButton launchButton;          // inside the pod: seals the hatch and launches
        public TextMeshPro podDisplay;            // small screen inside the pod
        public Light podLight;

        [Header("Voice lines (optional)")]
        public AudioClip introVoice, podReadyVoice, lifeSignsVoice, rebootVoice, homeVoice;
        public AudioClip twoMinutesVoice, oneMinuteVoice, meltdownVoice, keypadLockedVoice;

        [Header("Failure")]
        [Tooltip("Seconds the meltdown screen stays up before the game restarts.")]
        public float restartDelay = 15f;

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
            foreach (var rp in new[] { room1Power, room2Power, room3Power }) if (rp != null) rp.SetPowered(false);
            SetTeleport(podTeleport, false);

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
            if (keypad != null)
            {
                keypad.Solved += OnKeypadSolved;
                keypad.Message += m => ShowBanner(m, 4f);
                keypad.LockedOut += () => SOAudio.PlayVoice(keypadLockedVoice, keypad.transform.position);
            }
            if (emergencyButton != null) emergencyButton.Pressed += OnEmergencyPressed;
            if (launchButton != null) launchButton.Pressed += OnLaunchPressed;
            if (podDisplay != null) podDisplay.text = "<b>LIFEBOAT 1</b>\n<size=70%><color=#AAAAAA>STANDBY</color></size>";

            if (codeNote != null) codeNote.text = "REACTOR SHUTDOWN CODE\n<color=#888888>[ ENCRYPTED ]\nStabilise the coolant to decode</color>";

            StartCoroutine(IntroThenStart());
        }

        IEnumerator IntroThenStart()
        {
            CurrentStage = Stage.Intro;
            UpdateDisplays(true);
            if (introVoice != null)
            {
                SOAudio.PlayAt(SOAudio.Chime, ScreenPos(), 0.8f);
                yield return new WaitForSeconds(0.8f);
                SOAudio.PlayVoice(introVoice, ScreenPos());
            }
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
                    if (room1Power != null) room1Power.PowerUp();
                    if (door1 != null) door1.Open();
                    SetTeleport(room2Teleport, true);
                    if (CoolantFilled() == coolantSockets.Length) { EnterStage(Stage.Access); return; }
                    break;

                case Stage.Access:
                    SOAudio.PlayAt(SOAudio.Success, Head(), 0.8f);
                    ShowBanner("COOLANT STABLE - shutdown code decoded on the wall", 6f);
                    Restore(room2Light);
                    if (room2Power != null) room2Power.PowerUp();
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
                    if (room3Power != null) room3Power.PowerUp();
                    if (podDoor != null) podDoor.Open();
                    SetTeleport(podTeleport, true);
                    if (room3Danger != null) room3Danger.gameObject.SetActive(false);
                    if (room3Success != null) room3Success.gameObject.SetActive(true);
                    if (reactorCore != null) reactorCore.Set(new Color(0.2f, 0.7f, 1f), false);
                    if (reactorRing != null) reactorRing.speedMultiplier = 0.25f;
                    if (humSource != null) humSource.pitch = 0.6f;
                    if (escapePodGlow != null) escapePodGlow.Set(new Color(0.2f, 1f, 0.4f), true);
                    var eb = emergencyButton != null ? emergencyButton.GetComponent<Glow>() : null;
                    if (eb != null) eb.Set(new Color(0.2f, 1f, 0.3f), false);
                    break;

                case Stage.Boarded:
                    if (launchButton == null) { EnterStage(Stage.Complete); return; }
                    SOAudio.PlayAt(SOAudio.Chime, launchButton.transform.position, 0.8f);
                    SOAudio.PlayVoice(podReadyVoice, launchButton.transform.position);
                    var lg = launchButton.GetComponent<Glow>();
                    if (lg != null) lg.Set(new Color(0.2f, 1f, 0.35f), true);
                    if (podDisplay != null) podDisplay.text = "<b>LIFEBOAT 1</b>\n<color=#55FF88>READY</color>\n<size=70%>Press the green LAUNCH button</size>";
                    break;

                case Stage.Launching:
                    StartCoroutine(LaunchSequence());
                    break;

                case Stage.Complete:
                    SOAudio.PlayAt(SOAudio.Success, Head(), 1f);
                    if (escapePodGlow != null) escapePodGlow.Set(new Color(0.2f, 1f, 0.4f), false);
                    restartAllowedAt = Time.time + 6f;
                    break;

                case Stage.Failed:
                    StartCoroutine(MeltdownSequence());
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
                float before = timeLeft;
                timeLeft -= Time.deltaTime;
                TimeWarnings(before, timeLeft);
                if (timeLeft <= 0f) { timeLeft = 0f; EnterStage(Stage.Failed); }
            }

            if (CurrentStage == Stage.Escape && escapePodZone != null && HeadTransform() != null)
            {
                Bounds b = escapePodZone.bounds;
                b.Expand(new Vector3(0.1f, 4f, 0.1f));
                if (b.Contains(HeadTransform().position)) EnterStage(Stage.Boarded);
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
                case Stage.Boarded: return launchButton != null ? launchButton.transform : null;
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

            Transform t = (CurrentStage >= Stage.Power && CurrentStage <= Stage.Boarded) ? PickTarget() : null;

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
                return "<size=130%><color=#55FF88><b>MISSION ACCOMPLISHED</b></color></size>\n\n" +
                       "You escaped Station Orion with\n<size=150%><b>" + timer + "</b></size> to spare.\nLifeboat 1 is on course for Earth.\n\n" +
                       "<size=80%><color=#AAAAAA>Thanks for playing. Press LAUNCH again (or F5) to play again.</color></size>";

            if (CurrentStage == Stage.Launching)
                return "<size=130%><color=#55FF88><b>LAUNCH SEQUENCE</b></color></size>\n\nHatch sealed. Rebooting lifeboat systems...";

            if (CurrentStage == Stage.Failed)
                return "<size=130%><color=#FF3333><b>REACTOR MELTDOWN</b></color></size>\n" +
                       "<color=#FF3333>MISSION FAILED</color>\n\n" +
                       "Time ran out before the reactor was shut down.\nThe station was lost.\n\n" +
                       "<size=80%>You completed: <b>" + Progress() + "</b></size>\n\n" +
                       "<color=#FFB347>Restarting in " + Mathf.CeilToInt(Mathf.Max(0f, restartAt - Time.time)) + " s</color>\n" +
                       "<size=70%><color=#AAAAAA>(press F5 or the emergency button to restart now)</color></size>";

            string s = "<color=" + timerColor + "><size=150%><b>" + timer + "</b></size></color>\n";
            if (Time.time < bannerUntil && banner != "") s += "<color=#FFFF66>" + banner + "</color>\n";
            s += "\n<b>OBJECTIVE</b>\n" + Objective(CurrentStage) + "\n\n" + Checklist();
            if (hint != "") s += "\n<size=85%><color=#88DDFF>HINT: " + hint + "</color></size>";
            s += "\n<size=65%><color=#888888>Keyboard: right-mouse look \u2022 WASD move \u2022 G grab \u2022 F5 restart</color></size>";
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
                case Stage.Boarded: return "Press the green <color=#55FF88>LAUNCH</color> button inside the pod to seal the hatch.";
            }
            return "";
        }

        string HintFor(Stage st)
        {
            switch (st)
            {
                case Stage.Power: return "The power cell is on the wall shelf. Point at it and hold GRIP, then let go above the yellow slot.";
                case Stage.Coolant: return "Red goes on red, blue on blue, green on green. Follow the diamond.";
                case Stage.Access: return "The keycard is on a crate in a corner. The scanner glows cyan on the wall beside the Reactor Room door.";
                case Stage.Code: return "Forgot the code? Go back to the Engine Room and read the wall panel.";
                case Stage.Shutdown: return "Point at the big red button and press GRIP.";
                case Stage.Boarded: return "Point at the green LAUNCH button in the middle of the pod and press GRIP.";
            }
            return "";
        }

        string Checklist()
        {
            return Line("Power", CurrentStage > Stage.Power) +
                   Line("Coolant " + CoolantFilled() + "/" + (coolantSockets != null ? coolantSockets.Length : 3), CurrentStage > Stage.Coolant) +
                   Line("Access", CurrentStage > Stage.Access) +
                   Line("Shutdown code", CurrentStage > Stage.Code) +
                   Line("Reactor", CurrentStage > Stage.Shutdown) +
                   Line("Escape pod", CurrentStage > Stage.Boarded);
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

        // ------------------------------------------------------------ ending

        float restartAllowedAt = float.MaxValue;
        AudioSource rumbleSource;

        void OnLaunchPressed(PressButton b)
        {
            if (CurrentStage == Stage.Complete)
            {
                if (Time.time >= restartAllowedAt) Restart();
                return;
            }
            if (CurrentStage != Stage.Boarded)
            {
                SOAudio.PlayAt(SOAudio.Error, b.transform.position, 0.8f);
                ShowBanner("Lifeboat locked - shut down the reactor first", 4f);
                return;
            }
            EnterStage(Stage.Launching);
        }

        IEnumerator LaunchSequence()
        {
            Vector3 at = launchButton != null ? launchButton.transform.position : Head();
            var lg = launchButton != null ? launchButton.GetComponent<Glow>() : null;
            if (lg != null) lg.Set(new Color(0.2f, 1f, 0.35f), false);

            // 1. seal the hatch
            SetTeleport(podTeleport, false);
            if (podDoor != null) podDoor.Close();
            PodText("<b>LIFEBOAT 1</b>\n<color=#FFB347>SEALING HATCH</color>");
            yield return new WaitForSeconds(1.6f);

            // 2. life signs
            SOAudio.PlayAt(SOAudio.Chime, at, 0.8f);
            yield return new WaitForSeconds(0.7f);
            PodText("<b>LIFE SIGNS DETECTED: 1</b>\n<color=#55FF88>HATCH SEALED</color>");
            yield return Wait(SOAudio.PlayVoice(lifeSignsVoice, at), 2.5f);

            // 3. reboot: lights drop, rumble starts, progress bar
            if (podLight != null) podLight.color = new Color(1f, 0.55f, 0.3f);
            rumbleSource = SOAudio.AddLoop(gameObject, SOAudio.Rumble, 0.55f);
            rumbleSource.spatialBlend = 0f;
            rumbleSource.Play();
            float voice = SOAudio.PlayVoice(rebootVoice, at);
            float dur = Mathf.Max(voice, 4f);
            for (float t = 0f; t < dur; t += 0.2f)
            {
                int filled = Mathf.Clamp(Mathf.RoundToInt(t / dur * 16f), 0, 16);
                PodText("<b>SYSTEM REBOOT</b>\n<color=#7FE8FF>" + new string('|', filled) + "</color><color=#333333>" + new string('|', 16 - filled) +
                        "</color>\n<size=70%>" + (t < dur * 0.5f ? "Navigation..." : "Course set: <b>EARTH</b>") + "</size>");
                if (podLight != null) podLight.intensity = Mathf.Lerp(0.6f, 1.6f, Mathf.PingPong(t * 3f, 1f));
                yield return new WaitForSeconds(0.2f);
            }
            if (podLight != null) { podLight.color = new Color(0.75f, 0.9f, 1f); podLight.intensity = 1.6f; }

            // 4. detach + welcome home
            PodText("<b>DETACHING</b>\n<size=80%>from Station Orion</size>\n<color=#55FF88>COURSE: EARTH</color>");
            yield return Wait(SOAudio.PlayVoice(homeVoice, at), 3f);
            if (rumbleSource != null) rumbleSource.volume = 0.25f;
            PodText("<color=#55FF88><b>MISSION\nACCOMPLISHED</b></color>\n<size=70%>Welcome home, engineer</size>");
            EnterStage(Stage.Complete);
        }

        static IEnumerator Wait(float voiceLength, float atLeast)
        {
            yield return new WaitForSeconds(Mathf.Max(voiceLength, atLeast) + 0.3f);
        }

        void PodText(string t) { if (podDisplay != null) podDisplay.text = t; }

        Vector3 ScreenPos()
        {
            if (displays != null) foreach (var d in displays) if (d != null) return d.transform.position;
            return Head();
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

        // ------------------------------------------------------------ time warnings + meltdown (the "lose" ending)

        bool warned120, warned60;
        float restartAt = float.MaxValue;

        void TimeWarnings(float before, float now)
        {
            if (!warned120 && before > 120f && now <= 120f && missionSeconds > 150f)
            {
                warned120 = true;
                SOAudio.PlayVoice(twoMinutesVoice, ScreenPos());
                ShowBanner("WARNING: 2 minutes to meltdown", 5f);
            }
            if (!warned60 && before > 60f && now <= 60f)
            {
                warned60 = true;
                SOAudio.PlayVoice(oneMinuteVoice, ScreenPos());
                ShowBanner("WARNING: 1 minute to meltdown!", 6f);
                foreach (var src in alarmSources) if (src != null) { src.pitch = 1.25f; src.volume = 0.35f; }
            }
            // last 10 seconds: a beep every second
            if (now <= 10f && now > 0f && Mathf.CeilToInt(before) != Mathf.CeilToInt(now))
                SOAudio.PlayAt(SOAudio.Beep, Head() + Vector3.up * 0.3f, 1f);
        }

        string Progress()
        {
            if (CurrentStage != Stage.Failed) return "";
            int done = 0;
            if (powerSocket != null && powerSocket.IsFilled) done++;
            done += CoolantFilled();
            if (keycardSocket != null && keycardSocket.IsFilled) done++;
            if (keypad != null && keypad.IsSolved) done++;
            int total = 1 + (coolantSockets != null ? coolantSockets.Length : 3) + 2;
            return done + " of " + total + " repairs";
        }

        IEnumerator MeltdownSequence()
        {
            restartAt = Time.time + restartDelay;
            if (emergencyButton != null) emergencyButton.Pressed += b => { if (CurrentStage == Stage.Failed) Restart(); };

            // 1. alarms go frantic, the core flares white-hot, rumble starts
            foreach (var src in alarmSources) if (src != null) { src.pitch = 1.6f; src.volume = 0.45f; }
            if (reactorCore != null) { reactorCore.intensity = 4f; reactorCore.Set(new Color(1f, 0.85f, 0.6f), true); reactorCore.pulseSpeed = 14f; }
            if (reactorRing != null) reactorRing.speedMultiplier = 3f;
            if (humSource != null) humSource.pitch = 1.5f;
            var rumble = SOAudio.AddLoop(gameObject, SOAudio.Rumble, 0.6f);
            rumble.spatialBlend = 0f;
            rumble.Play();
            SOAudio.PlayAt(SOAudio.Error, Head(), 1f);
            yield return new WaitForSeconds(0.6f);
            SOAudio.PlayVoice(meltdownVoice, ScreenPos());

            // 2. every room light turns red and flickers
            var all = Object.FindObjectsByType<Light>();
            float t0 = Time.time;
            while (Time.time < restartAt)
            {
                float k = 0.35f + 0.65f * Mathf.PerlinNoise(Time.time * 6f, 0.3f);
                foreach (var l in all)
                {
                    if (l == null || l.type == LightType.Directional) continue;
                    l.color = Color.Lerp(l.color, new Color(1f, 0.12f, 0.05f), Time.deltaTime * 2f);
                    if (!baseIntensity.ContainsKey(l)) baseIntensity[l] = l.intensity;
                    l.intensity = baseIntensity[l] * k;
                }
                if (Time.time - t0 > restartDelay * 0.6f) rumble.volume = Mathf.Lerp(0.6f, 1f, (Time.time - t0) / restartDelay);
                UpdateDisplays(false);
                yield return null;
            }
            Restart();
        }

        public void Restart()
        {
            // The setup tool adds this scene to File > Build Profiles, which is required for reloading.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
