using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StationOrion.EditorTools
{
    /// <summary>
    /// Pieces added after the first build (scanner pedestal, escape-pod launch console, controls board,
    /// voice lines) + the menu "Station Orion > Apply Latest Fixes (open scene)".
    /// The menu works on ANY scene that has a MissionManager - the realistic station AND the original
    /// Main_StationOrion scene - because it finds things through the MissionManager's references
    /// (door 2, keycard, escape pod zone) instead of by room names. Your hand edits are kept.
    /// </summary>
    public static partial class StationBuilder
    {
        const string VoiceDir = "Assets/Audio/Voice";
        const string VoiceCredit = "Computer voice: generated with the Kokoro-82M text-to-speech model (hexgrad, Apache 2.0 licence).";
        const string FixRootName = "_StationOrion_Fixes";

        public const string ControlsText =
            "<b><color=#7FE8FF>HOW TO PLAY</color></b>\n<size=62%><align=left>" +
            "<b><color=#FFB347>VR CONTROLLERS</color></b>\n" +
            "Teleport: push thumbstick forward, aim at the floor, release\n" +
            "Turn: thumbstick left / right\n" +
            "Grab and place: point the laser + hold GRIP, let go over the slot\n" +
            "Press buttons: point + GRIP\n\n" +
            "<b><color=#FFB347>KEYBOARD + MOUSE (no headset)</color></b>\n" +
            "Look around: hold RIGHT MOUSE button + move the mouse (or arrow keys)\n" +
            "Move: W A S D      Up / down: E / Q\n" +
            "Control a hand: [ = left,  ] = right,  H = head,  Tab = cycle\n" +
            "Teleport: hold I to aim, release to jump      Turn: J / L\n" +
            "Grab / drop: G      Trigger: T      Reset view: R      Restart game: F5\n\n" +
            "Follow the floating <color=#7FE8FF>cyan diamond</color>. The screens show your objective and the timer.</align></size>";

        // ---------------------------------------------------------------- shared pieces

        /// <summary>Free-standing ID scanner; the keycard lies flat on the glowing pad on top.</summary>
        static XRSocketInteractor ScannerPedestal(Transform parent, Vector3 pos, out Renderer pad)
        {
            var root = Node("ScannerPedestal", parent, pos, Quaternion.identity).transform;
            Box("Base", root, new Vector3(0f, 0.03f, 0f), new Vector3(0.4f, 0.06f, 0.4f), M.trim, true, 0f);
            Box("Post", root, new Vector3(0f, 0.5f, 0f), new Vector3(0.16f, 1.0f, 0.16f), M.trim, true, 0f);
            Box("Head", root, new Vector3(0f, 1.03f, 0f), new Vector3(0.44f, 0.08f, 0.36f), M.metal, true, 0f);
            Box("HeadRim", root, new Vector3(0f, 1.074f, 0f), new Vector3(0.46f, 0.01f, 0.38f), M.stripCyan, false, 0f);
            var p = Box("ScannerPad", root, new Vector3(0f, 1.077f, 0f), new Vector3(0.3f, 0.008f, 0.22f), M.scannerPad, false, 0f);
            AddGlow(p, Cyan, 2f);
            pad = p.GetComponent<Renderer>();
            var socket = MakeSocket("Socket_Keycard", root, new Vector3(0f, 1.095f, 0f), Quaternion.Euler(-90f, 90f, 0f), "Keycard", 0.22f);
            PointLight("ScannerLight", root, new Vector3(0f, 1.55f, 0f), Cyan, 1.0f, 1.8f);
            Label(root, "ID SCANNER\n<size=70%>place the keycard here</size>", new Vector3(0f, 1.42f, 0f), 1.0f, Cyan);
            return socket;
        }

        /// <summary>Launch console + a pod screen (on the pod wall, or floating above the console).</summary>
        static PressButton PodLaunch(Transform parent, Vector3 pos, bool wallDisplay, out TextMeshPro display)
        {
            var root = Node("LaunchConsole", parent, pos, Quaternion.identity).transform;
            Box("Post", root, new Vector3(0f, 0.45f, 0f), new Vector3(0.22f, 0.9f, 0.22f), M.trim, true, 0f);
            Box("Top", root, new Vector3(0f, 0.93f, 0f), new Vector3(0.36f, 0.06f, 0.36f), M.hazard, true, 0f);
            Cyl("Collar", root, new Vector3(0f, 0.975f, 0f), Vector3.zero, 0.1f, 0.03f, M.trim, false, 24);
            var btn = Cyl("LaunchButton", root, new Vector3(0f, 1.005f, 0f), Vector3.zero, 0.08f, 0.04f, M.canGreen, false, 24);
            btn.AddComponent<BoxCollider>().size = new Vector3(0.16f, 0.04f, 0.16f);
            btn.AddComponent<XRSimpleInteractable>();
            var pb = btn.AddComponent<PressButton>();
            pb.id = "LAUNCH";
            pb.pushDirection = Vector3.down;
            pb.pushDepth = 0.015f;
            AddGlow(btn, Green, 2f);
            Label(root, "LAUNCH", new Vector3(0f, 1.3f, 0f), 0.6f, Green);
            if (wallDisplay)
                display = WallDisplay("PodDisplay", parent, new Vector3(20.8f, 2.02f, 12f), Vector3.left, new Vector2(0.9f, 0.36f), 0.8f);
            else
                display = Text("PodDisplay", root, new Vector3(0f, 1.75f, 0f), Quaternion.identity, new Vector2(0.9f, 0.4f), "", 0.8f,
                               new Color(0.9f, 0.96f, 1f), true);
            return pb;
        }

        static void AssignVoices(MissionManager mgr)
        {
            mgr.introVoice = Voice("SO_Voice_Intro");
            mgr.podReadyVoice = Voice("SO_Voice_PodReady");
            mgr.lifeSignsVoice = Voice("SO_Voice_LifeSigns");
            mgr.rebootVoice = Voice("SO_Voice_Reboot");
            mgr.homeVoice = Voice("SO_Voice_Home");
            mgr.twoMinutesVoice = Voice("SO_Voice_TwoMinutes");
            mgr.oneMinuteVoice = Voice("SO_Voice_OneMinute");
            mgr.meltdownVoice = Voice("SO_Voice_Meltdown");
            mgr.keypadLockedVoice = Voice("SO_Voice_KeypadLocked");
        }

        static AudioClip Voice(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(VoiceDir + "/" + name + ".wav");
            if (clip == null) log.AppendLine("Note: voice clip " + name + " not found in " + VoiceDir + " (the game still works without it).");
            return clip;
        }

        // ---------------------------------------------------------------- patch an existing scene

        [MenuItem("Station Orion/Apply Latest Fixes (open scene)", priority = 1)]
        public static void ApplyFixesMenu()
        {
            var mgr = Object.FindAnyObjectByType<MissionManager>();
            if (mgr == null)
            {
                EditorUtility.DisplayDialog("Station Orion", "No MissionManager in the open scene.\nOpen the scene you play (Main_StationOrion or StationOrion_Realistic) first.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Station Orion",
                    "This updates the OPEN scene \"" + SceneManager.GetActiveScene().name + "\" (your own edits are kept):\n\n" +
                    "- ID scanner on a free-standing pedestal beside Door 2\n- Escape pod LAUNCH button, pod screen and the new ending\n" +
                    "- Voice announcements\n- HOW TO PLAY board with keyboard controls\n\nCtrl+Z undoes it.", "Apply", "Cancel"))
                return;

            log = new StringBuilder();
            meshCache = new Dictionary<string, Mesh>();
            EnsureFolder(GenRoot);
            EnsureFolder(MatDir);
            EnsureFolder(StationTextures.Folder);
            try
            {
                T = StationTextures.EnsureAll();
                M = Mats.Create(T);
            }
            finally { EditorUtility.ClearProgressBar(); }

            Physics.SyncTransforms();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Station Orion fixes");
            int group = Undo.GetCurrentGroup();
            Undo.RecordObject(mgr, "fixes");

            var fixRoot = FindNamed(FixRootName);
            if (fixRoot == null)
            {
                fixRoot = new GameObject(FixRootName).transform;
                Undo.RegisterCreatedObjectUndo(fixRoot.gameObject, "fix root");
            }

            FixScanner(mgr, fixRoot);
            FixPod(mgr, fixRoot);
            AssignVoices(mgr);
            mgr.missionSeconds = 300f;
            mgr.restartDelay = 15f;
            log.AppendLine("- Timer set to 5 minutes; meltdown ending + time warnings on.");
            if (mgr.keypad != null)
            {
                Undo.RecordObject(mgr.keypad, "keypad");
                mgr.keypad.maxAttempts = 5;
                mgr.keypad.lockoutSeconds = 15f;
                EditorUtility.SetDirty(mgr.keypad);
                log.AppendLine("- Keypad: 5 wrong codes lock it for 15 seconds.");
            }
            FixControlsBoard(fixRoot);
            FixCredits();

            EditorUtility.SetDirty(mgr);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(group);
            Debug.Log("[Station Orion] Fixes applied.\n" + log);
            EditorUtility.DisplayDialog("Station Orion", "Fixes applied:\n\n" + log + "\nSave the scene (Ctrl+S), then press Play.", "OK");
        }

        // ---- 1. keycard scanner -> pedestal beside door 2
        static void FixScanner(MissionManager mgr, Transform fixRoot)
        {
            if (mgr.door2 == null || mgr.keycard == null)
            {
                log.AppendLine("- Scanner: door 2 or keycard not set on the MissionManager, skipped.");
                return;
            }

            // the keycard must not be a child of another item (it was under Canister_Green in one scene)
            var kt = mgr.keycard.transform;
            if (kt.parent != null && kt.parent.GetComponentInParent<Rigidbody>() != null)
            {
                var holder = kt.parent.GetComponentInParent<Rigidbody>().transform;
                Undo.SetTransformParent(kt, holder.parent, "free keycard");
                log.AppendLine("- Keycard was a child of " + holder.name + "; it is now a separate object.");
            }

            // remove the old scanner + socket
            var old = mgr.keycardSocket;
            if (old != null)
            {
                if (old.pad != null && old.pad.gameObject != old.gameObject && old.pad.GetComponent<XRGrabInteractable>() == null)
                {
                    Undo.RecordObject(old.pad.gameObject, "hide old scanner");
                    old.pad.gameObject.SetActive(false);
                }
                Undo.DestroyObjectImmediate(old.gameObject);
            }
            foreach (var n in new[] { "ScannerPedestal", "ScannerHousing", "ScannerPad", "ScannerLightBar", "ScannerLight", "ScannerGlowLight", "Label_IDSCANNER", "Socket_Keycard", "Scanner_Area" })
                DestroyAllNamed(null, n);

            // where: 0.9 m into the room from door 2, 1.4 m to its freer side, on the floor
            Transform door = mgr.door2.transform;
            Vector3 f = door.forward; f.y = 0f; f.Normalize();
            Vector3 toKey = kt.position - door.position; toKey.y = 0f;
            if (Vector3.Dot(toKey, f) < 0f) f = -f;
            Vector3 r = Vector3.Cross(Vector3.up, f);
            Vector3 mid = new Vector3(door.position.x, 1.0f, door.position.z) + f * 0.9f;
            float right = FreeDistance(mid, r), left = FreeDistance(mid, -r);
            Vector3 side = right >= left ? r : -r;
            Vector3 pos = mid + side * Mathf.Min(1.4f, Mathf.Max(right, left) - 0.35f);
            pos.y = FloorY(pos);

            Renderer pad;
            var sock = ScannerPedestal(fixRoot, pos, out pad);
            Undo.RegisterCreatedObjectUndo(sock.transform.parent.gameObject, "scanner");
            mgr.keycardSocket = MakeTask(sock, "ID Keycard", Cyan, pad, mgr.keycard);
            log.AppendLine("- ID scanner: new pedestal beside Door 2 (old wall scanner removed).");
        }

        // ---- 2. escape pod: launch console, pod screen, light
        static void FixPod(MissionManager mgr, Transform fixRoot)
        {
            if (mgr.escapePodZone == null)
            {
                log.AppendLine("- Ending: no escape pod zone set on the MissionManager, skipped.");
                return;
            }
            if (mgr.launchButton != null) Undo.DestroyObjectImmediate(mgr.launchButton.transform.parent.gameObject);
            foreach (var n in new[] { "LaunchConsole", "PodDisplay", "PodSign" }) DestroyAllNamed(null, n);

            var podRoom = FindNamed("EscapePod");
            bool realistic = podRoom != null;
            Transform parent = realistic ? podRoom : fixRoot;
            Vector3 c = mgr.escapePodZone.bounds.center;
            Vector3 pos = realistic ? new Vector3(19.75f, 0f, 12f) : new Vector3(c.x, FloorY(c + Vector3.up), c.z);

            TextMeshPro disp;
            mgr.launchButton = PodLaunch(parent, pos, realistic, out disp);
            Undo.RegisterCreatedObjectUndo(mgr.launchButton.transform.parent.gameObject, "launch");
            if (realistic) Undo.RegisterCreatedObjectUndo(disp.transform.parent.gameObject, "pod display");
            mgr.podDisplay = disp;

            var pl = FindNamed("PodLight");
            if (pl == null)
            {
                var l = PointLight("PodLight", fixRoot, pos + Vector3.up * 2.1f, new Color(1f, 0.88f, 0.75f), 1.4f, 4f);
                Undo.RegisterCreatedObjectUndo(l.gameObject, "pod light");
                pl = l.transform;
            }
            mgr.podLight = pl.GetComponent<Light>();
            log.AppendLine("- Ending: LAUNCH console + pod screen added in the escape pod" +
                           (mgr.podDoor != null ? " (the hatch closes on launch)." : " (this scene's pod has no hatch door, so nothing closes)."));
        }

        // ---- 3. controls board
        static void FixControlsBoard(Transform fixRoot)
        {
            var how = FindNamed("HowToPlay");
            if (how != null)
            {
                Vector3 hp = how.position, inward = -how.forward;
                Transform parent = how.parent;
                Undo.DestroyObjectImmediate(how.gameObject);
                var t = WallDisplay("HowToPlay", parent, hp, inward, new Vector2(2.3f, 1.35f), 1.0f, ControlsText);
                Undo.RegisterCreatedObjectUndo(t.transform.parent.gameObject, "how to play");
                log.AppendLine("- HOW TO PLAY board replaced with the controls version.");
                return;
            }

            // no wall board: a free-standing board in front of the start position, on a clear spot
            DestroyAllNamed(null, "Label_HOWTOPLAY");
            DestroyAllNamed(null, "ControlsBoard");
            var rig = Object.FindAnyObjectByType<XROrigin>();
            if (rig == null) { log.AppendLine("- HOW TO PLAY: no XR Origin found, skipped."); return; }
            Transform rt = rig.transform;
            Vector3 fwd = rt.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 rgt = Vector3.Cross(Vector3.up, fwd);
            Vector3[] tries = { fwd * 1.5f - rgt * 1.3f, fwd * 1.5f + rgt * 1.3f, fwd * 2.2f - rgt * 1.6f, fwd * 2.2f + rgt * 1.6f, fwd * 1.8f };
            Vector3 spot = rt.position + tries[0];
            foreach (var tr in tries)
            {
                Vector3 p = rt.position + tr;
                if (!Physics.CheckBox(p + Vector3.up * 1.1f, new Vector3(0.75f, 0.9f, 0.3f), Quaternion.LookRotation(fwd), ~0, QueryTriggerInteraction.Ignore))
                { spot = p; break; }
            }
            spot.y = FloorY(spot + Vector3.up);
            Vector3 toPlayer = rt.position - spot; toPlayer.y = 0f;
            var board = Node("ControlsBoard", fixRoot, spot, Quaternion.LookRotation(-toPlayer.normalized, Vector3.up)).transform;
            Undo.RegisterCreatedObjectUndo(board.gameObject, "controls board");
            Box("Stand", board, new Vector3(0f, 0.4f, 0.06f), new Vector3(0.12f, 0.8f, 0.08f), M.trim, true, 0f);
            Box("Foot", board, new Vector3(0f, 0.02f, 0.06f), new Vector3(0.6f, 0.04f, 0.4f), M.trim, true, 0f);
            WallDisplay("HowToPlay", board, new Vector3(0f, 1.45f, 0.06f), Vector3.back, new Vector2(1.5f, 1.0f), 0.9f, ControlsText);
            log.AppendLine("- HOW TO PLAY: free-standing controls board placed near the start.");
        }

        // ---- 4. credits: voice tool
        static void FixCredits()
        {
            foreach (var t in Object.FindObjectsByType<TextMeshPro>())
            {
                if (t.text == null || !t.text.ToUpper().Contains("CREDIT") && !t.text.Contains("STATION ORION</color></b>\n<size=80%>INTE")) continue;
                if (t.text.Contains("Kokoro")) return;
                Undo.RecordObject(t, "credits");
                int old = t.text.IndexOf("\n<size=60%>Computer voice:");
                if (old < 0) old = t.text.IndexOf("Computer voice: eSpeak");
                if (old >= 0) t.text = t.text.Substring(0, old);
                t.text += "\n<size=60%>" + VoiceCredit + "</size>";
                log.AppendLine("- Credits: voice tool added.");
                return;
            }
            log.AppendLine("- Credits board not found: add \"" + VoiceCredit + "\" to your credits by hand.");
        }

        // ---------------------------------------------------------------- helpers

        static float FreeDistance(Vector3 from, Vector3 dir)
        {
            RaycastHit hit;
            return Physics.Raycast(from, dir, out hit, 2.5f, ~0, QueryTriggerInteraction.Ignore) ? hit.distance : 2.5f;
        }

        static float FloorY(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out hit, 5f, ~0, QueryTriggerInteraction.Ignore)) return hit.point.y;
            return 0f;
        }

        static Transform FindNamed(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
            return null;
        }

        /// <summary>Destroys every object with this name (under 'under', or in the whole scene if null).</summary>
        static void DestroyAllNamed(Transform under, string name)
        {
            var list = new List<GameObject>();
            if (under != null)
            {
                foreach (var t in under.GetComponentsInChildren<Transform>(true))
                    if (t != under && t.name == name) list.Add(t.gameObject);
            }
            else
            {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        if (t.name == name) list.Add(t.gameObject);
            }
            foreach (var go in list)
                if (go != null && go.GetComponentInParent<XROrigin>() == null && go.GetComponent<XRGrabInteractable>() == null)
                    Undo.DestroyObjectImmediate(go);
        }
    }
}
