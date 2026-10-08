using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StationOrion.EditorTools
{
    /// <summary>
    /// Menu: Station Orion > Build Game Systems (open scene)
    /// Adds the whole game layer to Main_StationOrion in one click: mission manager, wall screens,
    /// labels, alarm lights, reactor core, keypad, emergency button, objective marker, door scripts,
    /// socket tasks and the keycard-scanner fix. Safe to run again: it deletes and rebuilds the
    /// generated objects (everything under "_StationOrion_Generated"). Ctrl+Z undoes it.
    /// </summary>
    public static class StationOrionSetup
    {
        const string GeneratedRoot = "_StationOrion_Generated";
        const string MatFolder = "Assets/Materials/Generated";

        static StringBuilder log;
        static Transform gen;
        static Scene scene;

        [MenuItem("Station Orion/Build Game Systems (open scene)")]
        public static void Build()
        {
            scene = SceneManager.GetActiveScene();
            if (!scene.name.Contains("StationOrion") &&
                !EditorUtility.DisplayDialog("Station Orion", "The open scene is \"" + scene.name +
                    "\", not Main_StationOrion. Build here anyway?", "Build", "Cancel"))
                return;

            log = new StringBuilder();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Build Station Orion Game Systems");
            int undoGroup = Undo.GetCurrentGroup();

            var room1 = Find("Room1_ControlRoom");
            var room2 = Find("Room2_EngineRoom");
            var room3 = Find("Room3_ReactorRoom");
            if (room1 == null || room2 == null || room3 == null)
            {
                EditorUtility.DisplayDialog("Station Orion", "Could not find Room1_ControlRoom, Room2_EngineRoom and Room3_ReactorRoom. Nothing was changed.", "OK");
                return;
            }

            // fresh generated root
            var old = Find(GeneratedRoot);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            var rootGo = new GameObject(GeneratedRoot);
            Undo.RegisterCreatedObjectUndo(rootGo, "create root");
            gen = rootGo.transform;

            // ---------------- manager
            var mgrGo = new GameObject("MissionManager");
            Undo.RegisterCreatedObjectUndo(mgrGo, "create");
            mgrGo.transform.SetParent(gen, false);
            var mgr = mgrGo.AddComponent<MissionManager>();

            // ---------------- player start
            var rig = Find("XR Origin (XR Rig)");
            var start = Find("PlayerStart");
            if (rig != null && start != null)
            {
                Undo.RecordObject(rig, "move rig");
                rig.position = start.position;
                rig.rotation = start.rotation;
                log.AppendLine("Player placed at PlayerStart, facing the screen and the first door.");
            }

            // ---------------- scanner fix (Room 2): move scanner beside the door it unlocks
            var scanner = room2.Find("Scanner_Area");
            var keySock = room2.Find("Socket_Keycard");
            if (scanner != null && keySock != null)
            {
                Undo.RecordObject(scanner, "move scanner");
                scanner.localPosition = new Vector3(2.87f, 1.4f, -1.15f);
                scanner.localRotation = Quaternion.Euler(0f, 90f, 0f);
                Undo.RecordObject(keySock, "fix socket");
                keySock.localPosition = new Vector3(2.7f, 1.4f, -1.15f);   // in FRONT of the panel
                keySock.localRotation = Quaternion.Euler(0f, 0f, 90f);       // card stands flat against it
                keySock.localScale = Vector3.one;                             // no shrinking of the drop zone
                log.AppendLine("Keycard scanner moved next to the Reactor Room door; drop zone fixed (was behind the panel and scaled to 15%).");
            }
            else log.AppendLine("WARNING: Scanner_Area or Socket_Keycard not found in Room2.");

            // bigger, easier drop zones
            foreach (var n in new[] { "Socket_Red", "Socket_Blue", "Socket_Green", "Socket_Keycard" })
                SetSocketRadius(room2.Find(n), 0.15f);
            SetSocketRadius(room1.Find("PowerSocket"), 0.15f);

            // ---------------- spread items around Room 2 (exploration)
            var crateMat = LoadMat("Assets/Materials/Prop_Metal.mat");
            MakeBox("Crate_Keycard", room2, new Vector3(2.2f, 0.35f, 2.2f), new Vector3(0.7f, 0.7f, 0.7f), crateMat, true);
            MakeBox("Crate_Red", room2, new Vector3(-2.4f, 0.3f, 2.4f), new Vector3(0.6f, 0.6f, 0.6f), crateMat, true);
            Place(room2.Find("Keycard"), new Vector3(2.2f, 0.73f, 2.2f), Quaternion.identity);
            Place(room2.Find("Canister_Red"), new Vector3(-2.4f, 0.72f, 2.4f), Quaternion.identity);
            Place(room2.Find("Canister_Blue"), new Vector3(1.6f, 0.12f, -2.4f), Quaternion.identity);
            Place(room2.Find("Canister_Green"), new Vector3(-1.5f, 1.12f, -1.35f), Quaternion.identity);
            log.AppendLine("Coolant canisters and keycard spread around the Engine Room (player has to search).");

            // ---------------- socket tasks
            var yellow = new Color(1f, 0.85f, 0.2f);
            var red = new Color(1f, 0.2f, 0.2f);
            var blue = new Color(0.2f, 0.5f, 1f);
            var green = new Color(0.2f, 1f, 0.35f);
            var cyan = new Color(0.3f, 0.9f, 1f);

            mgr.powerSocket = SetupSocket(room1, "PowerSocket", "PowerCell_SocketArea", "PowerCell", yellow, "Power Cell");
            mgr.powerCell = Item(room1, "PowerCell", yellow);
            mgr.coolantSockets = new[]
            {
                SetupSocket(room2, "Socket_Red", "CanisterSocket_Red", "Canister_Red", red, "Red Coolant"),
                SetupSocket(room2, "Socket_Blue", "CanisterSocket_Blue", "Canister_Blue", blue, "Blue Coolant"),
                SetupSocket(room2, "Socket_Green", "CanisterSocket_Green", "Canister_Green", green, "Green Coolant"),
            }.Where(s => s != null).ToArray();
            mgr.canisters = new[] { Item(room2, "Canister_Red", red), Item(room2, "Canister_Blue", blue), Item(room2, "Canister_Green", green) }
                .Where(c => c != null).ToArray();
            mgr.keycardSocket = SetupSocket(room2, "Socket_Keycard", "Scanner_Area", "Keycard", cyan, "ID Keycard");
            mgr.keycard = Item(room2, "Keycard", cyan);

            // ---------------- doors
            mgr.door1 = SetupDoor("Door_1to2");
            mgr.door2 = SetupDoor("Door_2to3");

            // ---------------- teleport locking
            mgr.room2Teleport = Teleports(room2.Find("Floor"));
            mgr.room3Teleport = Teleports(room3.Find("Floor"));
            log.AppendLine("Engine Room floor teleports: " + mgr.room2Teleport.Length + ", Reactor Room floor teleports: " + mgr.room3Teleport.Length + " (locked until doors open).");

            // ---------------- escape pod
            var pod = room3.Find("EscapePod_Zone");
            if (pod != null)
            {
                foreach (var tp in Teleports(pod)) { Undo.RecordObject(tp, "pod"); tp.enabled = false; }
                var col = pod.GetComponent<Collider>();
                if (col != null) { Undo.RecordObject(col, "pod"); col.isTrigger = true; mgr.escapePodZone = col; }
                mgr.escapePodGlow = GetOrAdd<Glow>(pod.gameObject);
                mgr.escapePodGlow.color = new Color(1f, 0.5f, 0.1f);
                log.AppendLine("Escape pod is now a walk-in zone (teleport onto the floor inside it to win).");
            }

            // ---------------- lights
            mgr.room1Light = LightNamed("Light_Room1_Normal");
            mgr.room2Light = LightNamed("Light_Room2_Normal");
            mgr.room3Light = LightNamed("Light_Room3_Normal");
            mgr.room3Danger = LightNamed("Light_Room3_Danger");
            mgr.room3Success = LightNamed("Light_Room3_Success");
            mgr.alarmLights = new[]
            {
                MakeLight("Alarm_Room1", room1, new Vector3(2.4f, 2.6f, 2.4f), Color.red, 3f, 8f),
                MakeLight("Alarm_Room2", room2, new Vector3(-2.4f, 2.6f, -2.4f), Color.red, 3f, 8f),
                MakeLight("Alarm_Room3", room3, new Vector3(2.4f, 2.6f, -2.4f), Color.red, 3f, 8f),
            };
            MakeLight("ScannerGlowLight", room2, new Vector3(2.45f, 1.5f, -1.15f), cyan, 2f, 1.5f);

            // ---------------- screens
            var panelMat = LoadMat("Assets/Materials/Screen_Black.mat");
            var white = new Color(0.92f, 0.96f, 1f);
            mgr.displays = new[]
            {
                // Room 1: on the existing Screen object
                MakeText("Display_ControlRoom", room1.TransformPoint(new Vector3(-2f, 1.6f, 2.81f)), room1.rotation,
                         new Vector2(1.85f, 1.1f), "STATION ORION", 1.4f, white, false),
                MakePanel("Display_EngineRoom", room2, new Vector3(0f, 1.9f, 2.86f), Vector3.zero, new Vector2(2.2f, 1.25f), panelMat, 1.4f, white),
                MakePanel("Display_ReactorRoom", room3, new Vector3(-2.86f, 1.95f, 1.0f), new Vector3(0f, -90f, 0f), new Vector2(1.9f, 1.15f), panelMat, 1.4f, white),
            };
            mgr.codeNote = MakePanel("CodeNote_EngineRoom", room2, new Vector3(-2.86f, 1.7f, -2.2f), new Vector3(0f, -90f, 0f),
                                     new Vector2(0.9f, 0.5f), panelMat, 1.2f, white);

            // ---------------- labels (always face the player)
            var labelCol = new Color(0.55f, 0.9f, 1f);
            Label("HOW TO PLAY\n<size=70%>Teleport: aim at the floor\nGrab: point at an object + GRIP\nPress buttons: point + GRIP</size>",
                  room1.TransformPoint(new Vector3(-1.1f, 1.45f, -0.9f)), new Vector2(0.8f, 0.4f), 0.9f, Color.white);
            Label("SPARE POWER CELL", room1.TransformPoint(new Vector3(-2.6f, 1.6f, -1f)), new Vector2(0.8f, 0.15f), 0.8f, yellow);
            Label("POWER SLOT", room1.TransformPoint(new Vector3(1.5f, 1.25f, -1.5f)), new Vector2(0.6f, 0.15f), 0.8f, yellow);
            Label("COOLANT PADS\n<size=70%>match the colours</size>", room2.TransformPoint(new Vector3(-1.5f, 1.65f, 0f)), new Vector2(0.8f, 0.25f), 0.8f, labelCol);
            Label("ID SCANNER", room2.TransformPoint(new Vector3(2.75f, 1.8f, -1.15f)), new Vector2(0.6f, 0.15f), 0.8f, cyan);
            Label("SHUTDOWN KEYPAD", room3.TransformPoint(new Vector3(-2.75f, 1.8f, -0.9f)), new Vector2(0.7f, 0.15f), 0.8f, labelCol);
            Label("EMERGENCY SHUTDOWN", room3.TransformPoint(new Vector3(-2f, 1.45f, 0f)), new Vector2(0.8f, 0.15f), 0.8f, new Color(1f, 0.4f, 0.4f));
            Label("ESCAPE POD", room3.TransformPoint(new Vector3(0f, 2.35f, 2.2f)), new Vector2(0.8f, 0.2f), 1f, new Color(1f, 0.6f, 0.2f));
            // door signs (fixed, readable from the room before the door)
            MakeText("Sign_EngineRoom", room1.TransformPoint(new Vector3(0f, 2.75f, 2.88f)), room1.rotation, new Vector2(1.4f, 0.3f), "ENGINE ROOM", 1.2f, labelCol, false);
            MakeText("Sign_ReactorRoom", room2.TransformPoint(new Vector3(2.88f, 2.75f, 0f)), room2.rotation * Quaternion.Euler(0f, 90f, 0f), new Vector2(1.4f, 0.3f), "REACTOR ROOM", 1.2f, labelCol, false);

            // ---------------- reactor core (Room 3 centrepiece)
            var coreMat = MakeMat("SO_ReactorCore", new Color(0.3f, 0.05f, 0.03f), new Color(1f, 0.25f, 0.1f));
            var ringMat = MakeMat("SO_ReactorRing", new Color(0.35f, 0.38f, 0.42f), Color.black);
            var core = Prim(PrimitiveType.Cylinder, "ReactorCore", room3, new Vector3(1.6f, 0.9f, -0.8f), new Vector3(0.5f, 0.9f, 0.5f), coreMat);
            mgr.reactorCore = GetOrAdd<Glow>(core);
            mgr.reactorCore.color = new Color(1f, 0.25f, 0.1f);
            var ring = Prim(PrimitiveType.Cylinder, "ReactorRing", room3, new Vector3(1.6f, 1.0f, -0.8f), new Vector3(1.1f, 0.02f, 1.1f), ringMat);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            mgr.reactorRing = ring.AddComponent<Spin>();
            mgr.reactorRing.degreesPerSecond = new Vector3(0f, 120f, 0f);
            Label("REACTOR CORE", room3.TransformPoint(new Vector3(1.6f, 2.1f, -0.8f)), new Vector2(0.7f, 0.15f), 0.8f, new Color(1f, 0.5f, 0.3f));

            // ---------------- emergency button
            var btnMat = LoadMat("Assets/Materials/Button_Red.mat");
            var btn = Prim(PrimitiveType.Cylinder, "EmergencyButton", room3, new Vector3(-2f, 1.13f, 0f), new Vector3(0.2f, 0.03f, 0.2f), btnMat);
            Object.DestroyImmediate(btn.GetComponent<Collider>());
            btn.AddComponent<BoxCollider>();
            btn.AddComponent<XRSimpleInteractable>();
            var pb = btn.AddComponent<PressButton>();
            pb.id = "EMERGENCY";
            pb.pushDirection = Vector3.down;
            var bg = btn.AddComponent<Glow>();
            bg.color = Color.red;
            mgr.emergencyButton = pb;

            // ---------------- keypad
            mgr.keypad = BuildKeypad(room3, new Vector3(-2.86f, 1.35f, -0.9f), new Vector3(0f, -90f, 0f), panelMat, crateMat);

            // ---------------- objective marker
            var markerMat = MakeMat("SO_Marker", new Color(0.2f, 0.8f, 1f), new Color(0.3f, 0.9f, 1f) * 2f);
            var marker = new GameObject("ObjectiveMarker");
            Undo.RegisterCreatedObjectUndo(marker, "create");
            marker.transform.SetParent(gen, false);
            var spinner = new GameObject("Spinner");
            spinner.transform.SetParent(marker.transform, false);
            var sp = spinner.AddComponent<Spin>();
            sp.degreesPerSecond = new Vector3(0f, 140f, 0f);
            sp.bobHeight = 0.05f;
            var diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diamond.name = "Diamond";
            Object.DestroyImmediate(diamond.GetComponent<Collider>());
            diamond.transform.SetParent(spinner.transform, false);
            diamond.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            diamond.transform.localScale = Vector3.one * 0.09f;
            diamond.GetComponent<Renderer>().sharedMaterial = markerMat;
            mgr.objectiveMarker = marker.transform;

            // ---------------- build settings (needed for restart + Windows build)
            AddSceneToBuild(scene.path);

            EditorUtility.SetDirty(mgr);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = mgrGo;

            Debug.Log("[Station Orion] Game systems built.\n" + log);
            EditorUtility.DisplayDialog("Station Orion",
                "Game systems built.\n\nSave the scene (Ctrl+S), then press Play.\n\nDetails are in the Console.\nCtrl+Z undoes everything.", "OK");
        }

        // ============================================================ helpers

        static Transform Find(string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
            return null;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(go);
            else Undo.RecordObject(c, "edit");
            return c;
        }

        static void Place(Transform t, Vector3 localPos, Quaternion localRot)
        {
            if (t == null) return;
            Undo.RecordObject(t, "place");
            t.localPosition = localPos;
            t.localRotation = localRot;
        }

        static void SetSocketRadius(Transform t, float r)
        {
            if (t == null) return;
            var s = t.GetComponent<SphereCollider>();
            if (s == null) return;
            Undo.RecordObject(s, "radius");
            s.radius = r;
            s.isTrigger = true;
        }

        static SocketTask SetupSocket(Transform room, string socketName, string padName, string itemName, Color color, string label)
        {
            var s = room.Find(socketName);
            if (s == null || s.GetComponent<XRSocketInteractor>() == null)
            {
                log.AppendLine("WARNING: socket " + socketName + " not found in " + room.name);
                return null;
            }
            var task = GetOrAdd<SocketTask>(s.gameObject);
            task.displayName = label;
            task.color = color;
            var pad = room.Find(padName);
            task.pad = pad != null ? pad.GetComponent<Renderer>() : null;
            var item = room.Find(itemName);
            task.expectedItem = item != null ? item.GetComponent<XRGrabInteractable>() : null;
            EditorUtility.SetDirty(task);
            return task;
        }

        static XRGrabInteractable Item(Transform room, string name, Color color)
        {
            var t = room.Find(name);
            if (t == null) { log.AppendLine("WARNING: item " + name + " not found"); return null; }
            var g = GetOrAdd<Glow>(t.gameObject);
            g.color = color;
            return t.GetComponent<XRGrabInteractable>();
        }

        static DoorController SetupDoor(string name)
        {
            var d = Find(name);
            if (d == null) { log.AppendLine("WARNING: door " + name + " not found"); return null; }
            var dc = GetOrAdd<DoorController>(d.gameObject);
            dc.slideDistance = 1.6f;
            dc.slideRight = true;
            return dc;
        }

        static Behaviour[] Teleports(Transform t)
        {
            if (t == null) return new Behaviour[0];
            return t.GetComponents<MonoBehaviour>().Where(m => m != null && m.GetType().Name == "TeleportationArea")
                    .Cast<Behaviour>().ToArray();
        }

        static Light LightNamed(string name)
        {
            var t = Find(name);
            if (t == null) { log.AppendLine("WARNING: light " + name + " not found"); return null; }
            return t.GetComponent<Light>();
        }

        static Light MakeLight(string name, Transform room, Vector3 local, Color c, float intensity, float range)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "create");
            go.transform.SetParent(gen, false);
            go.transform.position = room.TransformPoint(local);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            return l;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform room, Vector3 local, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Undo.RegisterCreatedObjectUndo(go, "create");
            go.name = name;
            go.transform.SetParent(gen, false);
            go.transform.position = room.TransformPoint(local);
            go.transform.rotation = room.rotation;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void MakeBox(string name, Transform room, Vector3 local, Vector3 scale, Material mat, bool keepCollider)
        {
            var go = Prim(PrimitiveType.Cube, name, room, local, scale, mat);
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        static TextMeshPro MakeText(string name, Vector3 worldPos, Quaternion worldRot, Vector2 size, string text, float maxSize, Color color, bool billboard)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "create");
            go.transform.SetParent(gen, false);
            go.transform.position = worldPos;
            go.transform.rotation = worldRot;
            var t = go.AddComponent<TextMeshPro>();
            t.rectTransform.sizeDelta = size;
            t.text = text;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true;
            t.fontSizeMin = 0.05f;
            t.fontSizeMax = maxSize;
            if (billboard) go.AddComponent<Billboard>();
            return t;
        }

        static void Label(string text, Vector3 worldPos, Vector2 size, float maxSize, Color color)
        {
            MakeText("Label_" + text.Split('\n')[0].Replace(" ", ""), worldPos, Quaternion.identity, size, text, maxSize, color, true);
        }

        // A dark backing panel on a wall with text in front of it.
        static TextMeshPro MakePanel(string name, Transform room, Vector3 local, Vector3 localEuler, Vector2 size, Material mat, float maxSize, Color color)
        {
            Quaternion rot = room.rotation * Quaternion.Euler(localEuler);
            Vector3 pos = room.TransformPoint(local);
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(back, "create");
            back.name = name + "_Panel";
            Object.DestroyImmediate(back.GetComponent<Collider>());
            back.transform.SetParent(gen, false);
            back.transform.position = pos + rot * Vector3.forward * 0.02f;   // just behind the text
            back.transform.rotation = rot;
            back.transform.localScale = new Vector3(size.x + 0.1f, size.y + 0.1f, 0.02f);
            if (mat != null) back.GetComponent<Renderer>().sharedMaterial = mat;
            return MakeText(name, pos, rot, size, "", maxSize, color, false);
        }

        static Keypad BuildKeypad(Transform room, Vector3 local, Vector3 localEuler, Material panelMat, Material keyMat)
        {
            Quaternion rot = room.rotation * Quaternion.Euler(localEuler);
            var root = new GameObject("ShutdownKeypad");
            Undo.RegisterCreatedObjectUndo(root, "create");
            root.transform.SetParent(gen, false);
            root.transform.position = room.TransformPoint(local);
            root.transform.rotation = rot;

            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Backplate";
            Object.DestroyImmediate(back.GetComponent<Collider>());
            back.transform.SetParent(root.transform, false);
            back.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            back.transform.localScale = new Vector3(0.42f, 0.62f, 0.02f);
            if (panelMat != null) back.GetComponent<Renderer>().sharedMaterial = panelMat;

            var dispGo = new GameObject("KeypadDisplay", typeof(RectTransform));
            dispGo.transform.SetParent(root.transform, false);
            dispGo.transform.localPosition = new Vector3(0f, 0.22f, -0.01f);
            var disp = dispGo.AddComponent<TextMeshPro>();
            disp.rectTransform.sizeDelta = new Vector2(0.36f, 0.1f);
            disp.alignment = TextAlignmentOptions.Center;
            disp.enableAutoSizing = true;
            disp.fontSizeMin = 0.05f;
            disp.fontSizeMax = 1f;
            disp.text = "LOCKED";
            disp.color = new Color(1f, 0.4f, 0.3f);

            string[] ids = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0" };
            var keys = new List<PressButton>();
            for (int i = 0; i < ids.Length; i++)
            {
                int col = i % 3, row = i / 3;
                Vector3 kp = new Vector3((col - 1) * 0.12f, 0.08f - row * 0.12f, -0.015f);
                var key = GameObject.CreatePrimitive(PrimitiveType.Cube);
                key.name = "Key_" + ids[i];
                key.transform.SetParent(root.transform, false);
                key.transform.localPosition = kp;
                key.transform.localScale = new Vector3(0.1f, 0.1f, 0.03f);
                if (keyMat != null) key.GetComponent<Renderer>().sharedMaterial = keyMat;
                key.AddComponent<XRSimpleInteractable>();
                var b = key.AddComponent<PressButton>();
                b.id = ids[i];
                b.pushDirection = rot * Vector3.forward;   // into the wall
                b.pushDepth = 0.01f;
                keys.Add(b);

                var lab = new GameObject("Label_" + ids[i], typeof(RectTransform));
                lab.transform.SetParent(root.transform, false);
                lab.transform.localPosition = kp + new Vector3(0f, 0f, -0.02f);
                var t = lab.AddComponent<TextMeshPro>();
                t.rectTransform.sizeDelta = new Vector2(0.08f, 0.08f);
                t.alignment = TextAlignmentOptions.Center;
                t.enableAutoSizing = true;
                t.fontSizeMin = 0.05f;
                t.fontSizeMax = 0.8f;
                t.text = ids[i];
                t.color = Color.white;
            }

            var kpComp = root.AddComponent<Keypad>();
            kpComp.display = disp;
            kpComp.keys = keys.ToArray();
            return kpComp;
        }

        static Material LoadMat(string path)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) log.AppendLine("Note: material " + path + " not found, using default.");
            return m;
        }

        static Material MakeMat(string name, Color baseColor, Color emission)
        {
            if (!AssetDatabase.IsValidFolder(MatFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Generated");
            string path = MatFolder + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor); else m.color = baseColor;
            if (emission != Color.black)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void AddSceneToBuild(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var list = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            list.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            log.AppendLine("Main scene set as the first scene in the build list.");
        }
    }
}
