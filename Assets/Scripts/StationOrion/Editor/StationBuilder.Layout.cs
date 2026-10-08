using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StationOrion.EditorTools
{
    /// <summary>
    /// Floor plan (metres, Y up, player starts in the Command Deck facing north / +Z):
    ///
    ///   Command Deck  x -4..4,  z -4..4   (Earth window on the west wall)
    ///   Corridor A    x -1.2..1.2, z 4..8 (Door 1 at z = 4)
    ///   Engine Room   x -4..4,  z 8..16
    ///   Corridor B    x 4..8,   z 10.8..13.2 (Door 2 at x = 4)
    ///   Reactor Room  x 8..18,  z 7..17   (5 m high)
    ///   Escape Pod    x 18..20.8, z 10.9..13.1 (pod hatch at x = 18)
    /// </summary>
    public static partial class StationBuilder
    {
        static readonly Color Yellow = new Color(1f, 0.85f, 0.2f);
        static readonly Color Red = new Color(1f, 0.2f, 0.2f);
        static readonly Color Blue = new Color(0.25f, 0.5f, 1f);
        static readonly Color Green = new Color(0.2f, 1f, 0.35f);
        static readonly Color Cyan = new Color(0.3f, 0.9f, 1f);
        static readonly Color Orange = new Color(1f, 0.5f, 0.15f);
        static readonly Color LabelBlue = new Color(0.6f, 0.9f, 1f);

        static void BuildLayout()
        {
            // ------------------------------------------------------------ shell
            var A = Room("CommandDeck", -4f, 4f, -4f, 4f, 3.2f,
                north: new[] { DoorOp(4f), ClearOp(1.55f, 2.3f), ClearOp(6.45f, 1.9f) },
                south: new[] { ClearOp(2.0f, 3.0f), ClearOp(6.2f, 1.8f) },
                east: new[] { ClearOp(2.8f, 1.4f), ClearOp(6.2f, 1.2f) },
                west: new[] { WinOp(4f, 5.2f, 0.9f, 2.5f) });
            var corA = Room("Corridor_A", -1.2f, 1.2f, 4f, 8f, 2.6f, null, null,
                east: new[] { WinOp(2f, 0.6f, 1.2f, 1.8f) },
                west: new[] { WinOp(2f, 0.6f, 1.2f, 1.8f) }, lightPanels: false);
            var B = Room("EngineRoom", -4f, 4f, 8f, 16f, 3.6f,
                north: new[] { ClearOp(4f, 2.8f) },
                south: new[] { DoorOp(4f), ClearOp(1.4f, 2.3f) },
                east: new[] { DoorOp(4f), ClearOp(5.35f, 0.7f) },
                west: new[] { ClearOp(4f, 3.8f), ClearOp(7.2f, 1.5f) });
            var corB = Room("Corridor_B", 4f, 8f, 10.8f, 13.2f, 2.6f,
                north: new[] { WinOp(2f, 0.6f, 1.2f, 1.8f) },
                south: new[] { WinOp(2f, 0.6f, 1.2f, 1.8f) }, east: null, west: null, lightPanels: false);
            var C = Room("ReactorRoom", 8f, 18f, 7f, 17f, 5f,
                north: new[] { ClearOp(1.8f, 2.9f), ClearOp(5f, 0.6f), ClearOp(8f, 1.4f) },
                south: new[] { ClearOp(5f, 3.6f) },
                east: new[] { DoorOp(5f, 1.4f, 2.1f) },
                west: new[] { DoorOp(5f) });
            var P = Room("EscapePod", 18f, 20.8f, 10.9f, 13.1f, 2.3f,
                north: new Opening[0], south: new Opening[0],
                east: new[] { WinOp(1.1f, 0.6f, 1.1f, 1.7f) }, west: null, lightPanels: false, wallMat: M.wallPod);

            CorridorPanels(corA, new Vector3(0f, 0f, 5f), new Vector3(0f, 0f, 7f));
            CorridorPanels(corB, new Vector3(5f, 0f, 12f), new Vector3(7f, 0f, 12f));
            CorridorPanels(P, new Vector3(19.5f, 0f, 12f));

            // ------------------------------------------------------------ doors
            var door1 = DoorAt(A.root, "Door1_CommandToEngine", new Vector3(0f, 0f, 4.1f), Vector3.right, 1.6f, 2.2f,
                               true, true, "ENGINEERING  >", "COMMAND DECK");
            DoorAt(B.root, "Frame_EngineSouth", new Vector3(0f, 0f, 7.9f), Vector3.right, 1.6f, 2.2f, false, false, null, "ENGINE ROOM", true);
            var door2 = DoorAt(B.root, "Door2_EngineToReactor", new Vector3(4.1f, 0f, 12f), Vector3.forward, 1.6f, 2.2f,
                               true, true, "ENGINE ROOM", "REACTOR  >");
            DoorAt(C.root, "Frame_ReactorWest", new Vector3(7.9f, 0f, 12f), Vector3.forward, 1.6f, 2.2f, false, false, "REACTOR CONTROL", null, true);
            var podDoor = DoorAt(C.root, "Door_EscapePod", new Vector3(18.1f, 0f, 12f), Vector3.forward, 1.4f, 2.1f,
                                 true, true, null, "ESCAPE POD");
            DoorAt(A.root, "Hatch_Airlock2", new Vector3(2.2f, 0f, -3.93f), Vector3.right, 1.3f, 2.1f, true, false, null, "AIRLOCK 2  -  SEALED");

            // ------------------------------------------------------------ lights
            RoomLight(A, new Vector3(-1.8f, 2.95f, -1.5f), 2.2f, 7f);
            RoomLight(A, new Vector3(1.8f, 2.95f, 1.8f), 2.2f, 7f);
            RoomLight(corA, new Vector3(0f, 2.4f, 6f), 1.4f, 4.5f);
            RoomLight(B, new Vector3(-1.8f, 3.35f, 10.5f), 2.6f, 8f);
            RoomLight(B, new Vector3(1.8f, 3.35f, 13.5f), 2.6f, 8f);
            RoomLight(corB, new Vector3(6f, 2.4f, 12f), 1.4f, 4.5f);
            RoomLight(C, new Vector3(10.5f, 4.6f, 9.5f), 3f, 10f);
            RoomLight(C, new Vector3(15.5f, 4.6f, 9.5f), 3f, 10f);
            RoomLight(C, new Vector3(10f, 4.6f, 15.5f), 3f, 10f);
            var podLight = PointLight("PodLight", P.root, new Vector3(19.5f, 2.05f, 12f), new Color(1f, 0.88f, 0.75f), 1.6f, 4f);
            P.lights.Add(podLight);

            var alarms = new List<Light>
            {
                Beacon(A.root, new Vector3(3.4f, 3.2f, -3.4f)),
                Beacon(B.root, new Vector3(-3.4f, 3.6f, 8.6f)),
                Beacon(C.root, new Vector3(17.4f, 5f, 7.6f)),
                Beacon(C.root, new Vector3(8.6f, 5f, 16.4f)),
            };

            // ------------------------------------------------------------ rooms
            var a = CommandDeck(A);
            var b = EngineRoom(B);
            var c = ReactorRoom(C);
            EscapePod(P, C);
            Exterior();
            Environment();

            // ------------------------------------------------------------ power groups (corridors belong to the next room)
            B.lights.AddRange(corA.lights); B.panels.AddRange(corA.panels);
            C.lights.AddRange(corB.lights); C.panels.AddRange(corB.panels);
            C.lights.AddRange(P.lights); C.panels.AddRange(P.panels);
            var powerA = FinishPower(A);
            var powerB = FinishPower(B);
            var powerC = FinishPower(C);

            // ------------------------------------------------------------ player
            PlacePlayer(new Vector3(0f, 0f, -2.6f));

            // ------------------------------------------------------------ mission
            var mgrGo = new GameObject("MissionManager");
            var mgr = mgrGo.AddComponent<MissionManager>();
            mgr.displays = new[] { a.display, b.display, c.display };
            mgr.codeNote = b.codeNote;
            mgr.powerSocket = a.powerTask;
            mgr.powerCell = a.powerCell;
            mgr.coolantSockets = b.coolantTasks;
            mgr.canisters = b.canisters;
            mgr.keycardSocket = b.keycardTask;
            mgr.keycard = b.keycard;
            mgr.keypad = c.keypad;
            mgr.emergencyButton = c.button;
            mgr.door1 = door1;
            mgr.door2 = door2;
            mgr.podDoor = podDoor;
            mgr.room2Teleport = corA.teleports.Concat(B.teleports).ToArray();
            mgr.room3Teleport = corB.teleports.Concat(C.teleports).ToArray();
            mgr.podTeleport = P.teleports.ToArray();
            mgr.escapePodZone = podZoneCache;
            mgr.escapePodGlow = c.podGlow;
            mgr.reactorCore = c.core;
            mgr.reactorRing = c.ring;
            mgr.objectiveMarker = Marker();
            mgr.alarmLights = alarms.ToArray();
            mgr.room3Danger = c.danger;
            mgr.room3Success = c.success;
            mgr.room1Power = powerA;
            mgr.room2Power = powerB;
            mgr.room3Power = powerC;
            Selection.activeGameObject = mgrGo;
        }

        static void CorridorPanels(RoomInfo r, params Vector3[] at)
        {
            bool longX = (r.x1 - r.x0) >= (r.z1 - r.z0);
            foreach (var p in at)
            {
                var size = longX ? new Vector3(1.1f, 0.03f, 0.4f) : new Vector3(0.4f, 0.03f, 1.1f);
                var go = Box("LightPanel", r.root, new Vector3(p.x, r.h - 0.015f, p.z), size, M.lightPanel, false, 0f);
                r.panels.Add(AddGlow(go, Color.white, 1.4f, true));
            }
        }

        // ================================================================ COMMAND DECK

        class DeckResult { public TextMeshPro display; public SocketTask powerTask; public XRGrabInteractable powerCell; }

        static DeckResult CommandDeck(RoomInfo A)
        {
            var res = new DeckResult();
            var t = A.root;

            // Earth window + navigation console + pilot seats
            var con = Node("NavConsole", t, Vector3.zero, Quaternion.identity).transform;
            Box("ConsoleBase", con, new Vector3(-3.62f, 0.4f, 0f), new Vector3(0.62f, 0.8f, 4.8f), M.trim, true, 1f);
            Box("ConsoleTop", con, new Vector3(-3.58f, 0.82f, 0f), new Vector3(0.74f, 0.05f, 4.9f), M.metal, true, 1f);
            Box("ConsoleKickLight", con, new Vector3(-3.3f, 0.06f, 0f), new Vector3(0.03f, 0.04f, 4.7f), M.stripCyan, false, 1f);
            ConsoleScreen(con, new Vector3(-3.72f, 1.02f, -1.55f), Vector3.right, new Vector2(0.62f, 0.32f), false, TelemetryText.Mode.Navigation);
            ConsoleScreen(con, new Vector3(-3.72f, 1.02f, 0f), Vector3.right, new Vector2(0.62f, 0.32f), true, TelemetryText.Mode.Navigation);
            ConsoleScreen(con, new Vector3(-3.72f, 1.02f, 1.55f), Vector3.right, new Vector2(0.62f, 0.32f), false, TelemetryText.Mode.LifeSupport);
            Material[] btn = { M.glowCyan, M.glowYellow, M.glowGreen, M.glowRed, M.glowCyan, M.glowBlue };
            for (int i = 0; i < 18; i++)
                Box("IndicatorButton", con, new Vector3(-3.36f, 0.85f, -2.2f + i * 0.26f), new Vector3(0.05f, 0.012f, 0.05f), btn[i % btn.Length], false, 0f);
            Chair(t, new Vector3(-2.6f, 0f, -0.85f), Vector3.left);
            Chair(t, new Vector3(-2.6f, 0f, 0.85f), Vector3.left);
            Label(t, "EARTH  -  ORBIT 408 km", new Vector3(-3.2f, 2.75f, 0f), 1.6f, LabelBlue, 0.6f);

            // holotable
            var holo = Node("Holotable", t, new Vector3(0.4f, 0f, 0.7f), Quaternion.identity).transform;
            Cyl("Pedestal", holo, new Vector3(0f, 0.42f, 0f), Vector3.zero, 0.42f, 0.84f, M.trim, true, 32);
            Cyl("Rim", holo, new Vector3(0f, 0.87f, 0f), Vector3.zero, 0.62f, 0.06f, M.metal, false, 40);
            Cyl("RimLight", holo, new Vector3(0f, 0.835f, 0f), Vector3.zero, 0.625f, 0.012f, M.stripCyan, false, 40);
            Cyl("Emitter", holo, new Vector3(0f, 0.905f, 0f), Vector3.zero, 0.5f, 0.012f, M.holo, false, 40);
            var hg = Node("Hologram", holo, new Vector3(0f, 1.35f, 0f), Quaternion.identity);
            hg.AddComponent<Spin>().degreesPerSecond = new Vector3(0f, 18f, 0f);
            SmoothSphere("HoloPlanet", hg.transform, Vector3.zero, 0.15f, M.holo, 32, 16);
            Ring(hg.transform, "HoloOrbit", Vector3.zero, 0.3f, 28, new Vector3(0.05f, 0.005f, 0.01f), M.holo, 0f);
            var icon = Node("HoloStation", hg.transform, new Vector3(0.3f, 0f, 0f), Quaternion.identity).transform;
            Box("Hub", icon, Vector3.zero, new Vector3(0.05f, 0.02f, 0.02f), M.holo, false, 0f);
            Box("Panels", icon, Vector3.zero, new Vector3(0.012f, 0.012f, 0.1f), M.holo, false, 0f);
            PointLight("HoloGlow", holo, new Vector3(0f, 1.2f, 0f), new Color(0.3f, 0.8f, 1f), 0.8f, 2.5f);

            // main mission screen + how to play (north wall, visible from the start position)
            res.display = WallDisplay("Display_CommandDeck", t, new Vector3(-2.45f, 1.75f, 4f), Vector3.back, new Vector2(2.1f, 1.2f), 1.4f);
            WallDisplay("HowToPlay", t, new Vector3(2.45f, 1.75f, 4f), Vector3.back, new Vector2(1.7f, 1.2f), 1.0f,
                "<b><color=#7FE8FF>HOW TO PLAY</color></b>\n<size=75%><align=left>" +
                "<b>TELEPORT</b>  aim at the floor and release\n" +
                "<b>GRAB</b>  point at an object, hold GRIP\n" +
                "<b>DROP / PLACE</b>  let go of GRIP over the slot\n" +
                "<b>PRESS BUTTONS</b>  point + GRIP\n\n" +
                "Follow the floating <color=#7FE8FF>cyan diamond</color>.\nThe screens show your objective and the reactor timer.</align></size>");
            Box("FloorHazard", t, new Vector3(0f, 0.003f, 3.75f), new Vector3(1.9f, 0.006f, 0.3f), M.hazard, false, 0.5f);

            // east wall: spare-parts rack with the power cell
            Rack(t, new Vector3(3.78f, 0f, -1.2f), Vector3.left, 1.25f, 0.42f, 1.75f, new[] { 0.45f, 0.95f, 1.45f });
            Box("SpareBox", t, new Vector3(3.75f, 0.565f, -0.9f), new Vector3(0.25f, 0.2f, 0.3f), M.crate, true, 0f);
            Box("SpareBox", t, new Vector3(3.75f, 1.545f, -1.3f), new Vector3(0.3f, 0.16f, 0.4f), M.locker, true, 0f);
            Cyl("EmptyCell", t, new Vector3(3.75f, 0.965f + 0.15f, -0.85f), Vector3.zero, 0.055f, 0.28f, M.metal, true, 20);
            res.powerCell = PowerCell(t, new Vector3(3.75f, 0.965f + 0.175f, -1.45f));
            Label(t, "SPARE POWER CELLS", new Vector3(3.55f, 1.95f, -1.2f), 1.1f, Yellow);

            // power distribution unit with the power slot
            Box("PDU_Cabinet", t, new Vector3(3.62f, 0.5f, 2.2f), new Vector3(0.75f, 1.0f, 0.9f), M.trim, true, 1f);
            Box("PDU_Vent", t, new Vector3(3.24f, 0.45f, 2.2f), new Vector3(0.01f, 0.5f, 0.6f), M.vent, false, 0f);
            Box("PDU_Top", t, new Vector3(3.6f, 1.01f, 2.2f), new Vector3(0.8f, 0.02f, 0.95f), M.metal, true, 1f);
            Box("PDU_Light", t, new Vector3(3.24f, 0.85f, 2.2f), new Vector3(0.012f, 0.03f, 0.5f), M.glowYellow, false, 0f);
            Pipe("Cable", t, new Vector3(3.92f, 1.0f, 1.95f), new Vector3(3.92f, 3.2f, 1.95f), 0.04f, M.cable);
            Pipe("Cable", t, new Vector3(3.92f, 1.0f, 2.45f), new Vector3(3.92f, 3.2f, 2.45f), 0.04f, M.cable);
            var pad = Cyl("PowerSlotPad", t, new Vector3(3.6f, 1.035f, 2.2f), Vector3.zero, 0.1f, 0.03f, M.padBase, false, 24);
            AddGlow(pad, Yellow, 2f);
            var sock = MakeSocket("PowerSocket", t, new Vector3(3.6f, 1.05f + 0.175f, 2.2f), Quaternion.identity, "PowerCell");
            res.powerTask = MakeTask(sock, "Power Cell", Yellow, pad.GetComponent<Renderer>(), res.powerCell);
            Label(t, "POWER SLOT\n<size=70%>insert power cell</size>", new Vector3(3.45f, 1.6f, 2.2f), 0.9f, Yellow);

            // south wall: crew lockers
            for (int i = 0; i < 5; i++)
            {
                float x = -3.12f + i * 0.56f;
                Box("Locker", t, new Vector3(x, 0.975f, -3.74f), new Vector3(0.54f, 1.95f, 0.5f), M.locker, true, 0f);
                Box("LockerVent", t, new Vector3(x, 1.6f, -3.487f), new Vector3(0.3f, 0.12f, 0.01f), M.vent, false, 0f);
                Box("LockerHandle", t, new Vector3(x + 0.2f, 1.05f, -3.47f), new Vector3(0.03f, 0.16f, 0.03f), M.metal, false, 0f);
                Text("LockerTag", t, new Vector3(x, 1.82f, -3.486f), Quaternion.Euler(0f, 180f, 0f), new Vector2(0.4f, 0.07f),
                     "CREW 0" + (i + 1), 0.4f, new Color(0.85f, 0.85f, 0.85f));
            }

            // pipes along the east wall
            Pipe("Pipe", t, new Vector3(3.85f, 2.82f, -4f), new Vector3(3.85f, 2.82f, 4f), 0.05f, M.metal);
            Pipe("Pipe", t, new Vector3(3.85f, 2.95f, -4f), new Vector3(3.85f, 2.95f, 4f), 0.035f, M.trim);
            return res;
        }

        // ================================================================ ENGINE ROOM

        class EngineResult
        {
            public TextMeshPro display, codeNote;
            public SocketTask[] coolantTasks;
            public XRGrabInteractable[] canisters;
            public SocketTask keycardTask;
            public XRGrabInteractable keycard;
        }

        static EngineResult EngineRoom(RoomInfo B)
        {
            var res = new EngineResult();
            var t = B.root;

            // fusion generator (north)
            var gen = Node("FusionGenerator", t, new Vector3(0f, 1.0f, 14.9f), Quaternion.identity).transform;
            var body = Cyl("Body", gen, Vector3.zero, new Vector3(0f, 0f, 90f), 0.6f, 3.0f, M.metal, false, 40);
            body.AddComponent<BoxCollider>().size = new Vector3(1.2f, 3.0f, 1.2f);
            foreach (float x in new[] { -0.9f, 0f, 0.9f })
                Cyl("CoolingRing", gen, new Vector3(x, 0f, 0f), new Vector3(0f, 0f, 90f), 0.64f, 0.1f, M.stripWarm, false, 40);
            Cyl("EndCap", gen, new Vector3(-1.55f, 0f, 0f), new Vector3(0f, 0f, 90f), 0.5f, 0.12f, M.trim, false, 32);
            Cyl("EndCap", gen, new Vector3(1.55f, 0f, 0f), new Vector3(0f, 0f, 90f), 0.5f, 0.12f, M.trim, false, 32);
            var fan = Node("Fan", gen, new Vector3(1.64f, 0f, 0f), Quaternion.identity);
            fan.AddComponent<Spin>().degreesPerSecond = new Vector3(300f, 0f, 0f);
            for (int i = 0; i < 6; i++)
                RotBox("Blade", fan.transform, Vector3.zero, new Vector3(i * 60f, 0f, 0f), new Vector3(0.02f, 0.85f, 0.12f), M.metal);
            Box("Cradle", t, new Vector3(-1.0f, 0.25f, 14.9f), new Vector3(0.3f, 0.5f, 1.1f), M.trim, true, 0f);
            Box("Cradle", t, new Vector3(1.0f, 0.25f, 14.9f), new Vector3(0.3f, 0.5f, 1.1f), M.trim, true, 0f);
            Pipe("Pipe", t, new Vector3(-1.3f, 1.4f, 14.9f), new Vector3(-1.3f, 3.6f, 14.9f), 0.08f, M.metal);
            Pipe("Pipe", t, new Vector3(1.3f, 1.4f, 14.9f), new Vector3(1.3f, 3.6f, 14.9f), 0.08f, M.metal);
            var hum = gen.gameObject.AddComponent<AmbientLoop>();
            hum.volume = 0.3f; hum.pitch = 0.7f; hum.maxDistance = 9f;

            res.display = WallDisplay("Display_EngineRoom", t, new Vector3(0f, 2.65f, 16f), Vector3.back, new Vector2(2.3f, 1.25f), 1.4f);

            // coolant manifold (west wall)
            Box("Manifold", t, new Vector3(-3.65f, 0.45f, 12f), new Vector3(0.7f, 0.9f, 3.4f), M.trim, true, 1f);
            Box("ManifoldTop", t, new Vector3(-3.63f, 0.93f, 12f), new Vector3(0.76f, 0.02f, 3.5f), M.metal, true, 1f);
            Box("ManifoldBack", t, new Vector3(-3.97f, 1.45f, 12f), new Vector3(0.05f, 1.0f, 3.4f), M.trim, false, 1f);
            Box("ManifoldVent", t, new Vector3(-3.295f, 0.42f, 12f), new Vector3(0.01f, 0.45f, 2.9f), M.vent, false, 0f);
            string[] names = { "Red", "Blue", "Green" };
            Color[] cols = { Red, Blue, Green };
            Material[] glowMats = { M.glowRed, M.glowBlue, M.glowGreen };
            string[] layers = { "CoolantRed", "CoolantBlue", "CoolantGreen" };
            var pads = new Renderer[3];
            var sockets = new XRSocketInteractor[3];
            for (int i = 0; i < 3; i++)
            {
                float z = 11f + i;
                var p = Cyl("Pad_" + names[i], t, new Vector3(-3.6f, 0.955f, z), Vector3.zero, 0.12f, 0.03f, M.padBase, false, 24);
                AddGlow(p, cols[i], 2f);
                pads[i] = p.GetComponent<Renderer>();
                sockets[i] = MakeSocket("Socket_" + names[i], t, new Vector3(-3.6f, 0.97f + 0.195f, z), Quaternion.identity, layers[i]);
                Pipe("CoolantLine", t, new Vector3(-3.9f, 0.94f, z), new Vector3(-3.9f, 1.95f, z), 0.045f, glowMats[i]);
                Pipe("CoolantLine", t, new Vector3(-3.9f, 2.95f, z - 0.9f), new Vector3(-3.9f, 3.6f, z - 0.9f), 0.045f, glowMats[i]);
                var lab = Text("PadLabel", t, new Vector3(-3.288f, 0.78f, z), Quaternion.LookRotation(Vector3.left), new Vector2(0.4f, 0.1f),
                               names[i].ToUpper(), 0.6f, cols[i]);
                lab.fontStyle = FontStyles.Bold;
            }
            res.codeNote = WallDisplay("CodeNote_EngineRoom", t, new Vector3(-4f, 2.45f, 12f), Vector3.right, new Vector2(1.6f, 0.8f), 1.1f);
            Label(t, "COOLANT MANIFOLD\n<size=70%>match the colours</size>", new Vector3(-3.3f, 1.62f, 12f), 1.2f, LabelBlue);

            // workbench (south-west) with the blue canister
            Box("BenchTop", t, new Vector3(-2.6f, 0.9f, 8.42f), new Vector3(2.0f, 0.06f, 0.7f), M.metal, true, 1f);
            foreach (float lx in new[] { -3.55f, -1.65f })
                foreach (float lz in new[] { 8.12f, 8.72f })
                    Box("BenchLeg", t, new Vector3(lx, 0.435f, lz), new Vector3(0.05f, 0.87f, 0.05f), M.trim, true, 0f);
            Box("BenchShelf", t, new Vector3(-2.6f, 0.3f, 8.42f), new Vector3(1.9f, 0.03f, 0.6f), M.metal, true, 1f);
            Box("Toolbox", t, new Vector3(-3.2f, 1.0f, 8.35f), new Vector3(0.45f, 0.14f, 0.22f), M.buttonRed, true, 0f);
            Box("Pegboard", t, new Vector3(-2.6f, 1.55f, 8.015f), new Vector3(2.0f, 0.8f, 0.02f), M.vent, false, 0f);
            Cyl("CableSpool", t, new Vector3(-2.6f, 0.4f, 8.42f), new Vector3(90f, 0f, 0f), 0.14f, 0.12f, M.cable, true, 20);
            Box("Datapad", t, new Vector3(-2.85f, 0.94f, 8.55f), new Vector3(0.2f, 0.015f, 0.14f), M.screen, false, 0f);

            // crates
            Crate(t, new Vector3(3.35f, 0f, 15.35f), 0.8f, "CARGO 07", Vector3.left);
            Crate(t, new Vector3(3.35f, 0.8f, 15.35f), 0.6f, "COOLANT", Vector3.back);
            Crate(t, new Vector3(3.3f, 0f, 8.75f), 0.7f, "SECURITY", Vector3.left);
            Crate(t, new Vector3(2.5f, 0f, 8.6f), 0.5f, null, Vector3.back);

            // storage rack (north-west)
            Rack(t, new Vector3(-3.77f, 0f, 15.2f), Vector3.right, 1.2f, 0.45f, 1.8f, new[] { 0.45f, 0.95f, 1.45f });
            Box("RackBox", t, new Vector3(-3.75f, 0.565f, 15.0f), new Vector3(0.3f, 0.2f, 0.35f), M.crate, true, 0f);
            Box("RackBox", t, new Vector3(-3.75f, 1.545f, 15.3f), new Vector3(0.3f, 0.16f, 0.5f), M.locker, true, 0f);

            // items (spread around the room so the player explores)
            var red = Canister("Canister_Red", t, new Vector3(3.35f, 1.4f + 0.2f, 15.35f), M.canRed, M.glowRed, Red, "CoolantRed");
            var blue = Canister("Canister_Blue", t, new Vector3(-2.15f, 0.93f + 0.2f, 8.45f), M.canBlue, M.glowBlue, Blue, "CoolantBlue");
            var green = Canister("Canister_Green", t, new Vector3(-3.75f, 0.965f + 0.2f, 15.4f), M.canGreen, M.glowGreen, Green, "CoolantGreen");
            res.canisters = new[] { red, blue, green };
            res.coolantTasks = new[]
            {
                MakeTask(sockets[0], "Red Coolant", Red, pads[0], red),
                MakeTask(sockets[1], "Blue Coolant", Blue, pads[1], blue),
                MakeTask(sockets[2], "Green Coolant", Green, pads[2], green),
            };
            res.keycard = Keycard(t, new Vector3(3.3f, 0.7f + 0.01f, 8.75f), Quaternion.Euler(90f, 25f, 0f));

            // ID scanner beside door 2 (east wall)
            Box("ScannerHousing", t, new Vector3(3.965f, 1.42f, 13.35f), new Vector3(0.07f, 0.42f, 0.3f), M.trim, false, 0f);
            var spad = Box("ScannerPad", t, new Vector3(3.925f, 1.44f, 13.35f), new Vector3(0.012f, 0.2f, 0.16f), M.scannerPad, false, 0f);
            AddGlow(spad, Cyan, 2f);
            Box("ScannerLightBar", t, new Vector3(3.925f, 1.255f, 13.35f), new Vector3(0.012f, 0.025f, 0.2f), M.stripCyan, false, 0f);
            var ksock = MakeSocket("Socket_Keycard", t, new Vector3(3.9f, 1.44f, 13.35f), Quaternion.LookRotation(Vector3.right), "Keycard");
            res.keycardTask = MakeTask(ksock, "ID Keycard", Cyan, spad.GetComponent<Renderer>(), res.keycard);
            PointLight("ScannerLight", t, new Vector3(3.6f, 1.6f, 13.35f), Cyan, 1.0f, 1.6f);
            Label(t, "ID SCANNER", new Vector3(3.72f, 1.85f, 13.35f), 0.8f, Cyan);

            Box("FloorHazard", t, new Vector3(3.75f, 0.003f, 12f), new Vector3(0.3f, 0.006f, 1.9f), M.hazard, false, 0.5f);
            Pipe("Pipe", t, new Vector3(3.85f, 3.15f, 8f), new Vector3(3.85f, 3.15f, 16f), 0.05f, M.metal);
            Pipe("Pipe", t, new Vector3(3.85f, 3.28f, 8f), new Vector3(3.85f, 3.28f, 16f), 0.035f, M.trim);
            return res;
        }

        // ================================================================ REACTOR ROOM

        class ReactorResult
        {
            public TextMeshPro display;
            public Keypad keypad;
            public PressButton button;
            public Glow core, podGlow;
            public Spin ring;
            public Light danger, success;
        }

        static ReactorResult ReactorRoom(RoomInfo C)
        {
            var res = new ReactorResult();
            var t = C.root;

            // reactor core
            var R = Node("Reactor", t, new Vector3(13f, 0f, 14.3f), Quaternion.identity).transform;
            var plinth = Cyl("Plinth", R, new Vector3(0f, 0.2f, 0f), Vector3.zero, 1.5f, 0.4f, M.trim, false, 48);
            plinth.AddComponent<MeshCollider>().sharedMesh = plinth.GetComponent<MeshFilter>().sharedMesh;
            Cyl("PlinthLight", R, new Vector3(0f, 0.41f, 0f), Vector3.zero, 1.52f, 0.025f, M.stripWarm, false, 48);
            Cyl("BaseRing", R, new Vector3(0f, 0.5f, 0f), Vector3.zero, 1.0f, 0.2f, M.metal, false, 40);
            var core = Cyl("ReactorCore", R, new Vector3(0f, 2.1f, 0f), Vector3.zero, 0.42f, 3.0f, M.reactorCore, false, 32);
            res.core = AddGlow(core, new Color(1f, 0.3f, 0.1f), 2.5f);
            Cyl("Containment", R, new Vector3(0f, 2.1f, 0f), Vector3.zero, 0.9f, 3.4f, M.glass, false, 48);
            for (int i = 0; i < 6; i++)
            {
                float ang = i * Mathf.PI / 3f;
                Cyl("EnergyRod", R, new Vector3(Mathf.Cos(ang) * 0.65f, 2.1f, Mathf.Sin(ang) * 0.65f), Vector3.zero, 0.035f, 3.3f, M.reactorRod, false, 10);
            }
            Ring(R, "Ring_Low", new Vector3(0f, 1.2f, 0f), 1.25f, 20, new Vector3(0.34f, 0.12f, 0.14f), M.metal, 40f);
            res.ring = Ring(R, "Ring_Mid", new Vector3(0f, 2.2f, 0f), 1.32f, 22, new Vector3(0.34f, 0.14f, 0.16f), M.metal, -70f);
            Ring(R, "Ring_High", new Vector3(0f, 3.2f, 0f), 1.25f, 20, new Vector3(0.34f, 0.12f, 0.14f), M.metal, 55f);
            Cyl("TopCap", R, new Vector3(0f, 3.95f, 0f), Vector3.zero, 1.1f, 0.35f, M.trim, false, 40);
            Cyl("TopCapLight", R, new Vector3(0f, 3.765f, 0f), Vector3.zero, 1.12f, 0.025f, M.stripWarm, false, 40);
            for (int i = 0; i < 4; i++)
            {
                float ang = (45f + 90f * i) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(ang) * 0.7f, 0f, Mathf.Sin(ang) * 0.7f);
                Pipe("UpperConduit", R, p + Vector3.up * 4.1f, p + Vector3.up * 5f, 0.09f, M.metal);
            }
            Pipe("FloorConduit", t, new Vector3(14.4f, 0.2f, 14.3f), new Vector3(18f, 0.2f, 14.3f), 0.16f, M.metal);
            Pipe("FloorConduit", t, new Vector3(13f, 0.2f, 15.7f), new Vector3(13f, 0.2f, 17f), 0.16f, M.metal);
            // safety railing
            int posts = 18;
            for (int i = 0; i < posts; i++)
            {
                float a0 = i * Mathf.PI * 2f / posts, a1 = (i + 1) * Mathf.PI * 2f / posts;
                var p0 = new Vector3(Mathf.Cos(a0) * 2f, 0f, Mathf.Sin(a0) * 2f);
                var p1 = new Vector3(Mathf.Cos(a1) * 2f, 0f, Mathf.Sin(a1) * 2f);
                Box("RailPost", R, p0 + Vector3.up * 0.55f, new Vector3(0.05f, 1.1f, 0.05f), M.trim, true, 0f);
                var dir = p1 - p0;
                var top = Box("RailTop", R, (p0 + p1) * 0.5f + Vector3.up * 1.08f, new Vector3(0.05f, 0.05f, dir.magnitude), M.hazard, true, 0f);
                top.transform.localRotation = Quaternion.LookRotation(dir);
                var mid = Box("RailMid", R, (p0 + p1) * 0.5f + Vector3.up * 0.55f, new Vector3(0.03f, 0.03f, dir.magnitude), M.trim, false, 0f);
                mid.transform.localRotation = Quaternion.LookRotation(dir);
            }
            res.danger = PointLight("ReactorDangerLight", t, new Vector3(13f, 2.5f, 12f), new Color(1f, 0.45f, 0.15f), 4f, 10f);
            res.success = PointLight("ReactorStableLight", t, new Vector3(13f, 2.5f, 12f), new Color(0.3f, 0.85f, 1f), 3f, 10f);
            res.success.gameObject.SetActive(false);
            Label(t, "REACTOR CORE\n<size=70%>containment failing</size>", new Vector3(13f, 1.75f, 12.05f), 1.1f, Orange);
            Box("Vent", t, new Vector3(16f, 2.5f, 16.99f), new Vector3(1.2f, 1.2f, 0.02f), M.vent, false, 0f);
            Box("Vent", t, new Vector3(17.99f, 2.5f, 9.0f), new Vector3(0.02f, 1.2f, 1.2f), M.vent, false, 0f);
            Pipe("Pipe", t, new Vector3(17.85f, 3.8f, 7f), new Vector3(17.85f, 3.8f, 17f), 0.07f, M.metal);
            Pipe("Pipe", t, new Vector3(17.85f, 4.0f, 7f), new Vector3(17.85f, 4.0f, 17f), 0.05f, M.trim);
            Pipe("Pipe", t, new Vector3(8f, 4.2f, 16.85f), new Vector3(18f, 4.2f, 16.85f), 0.07f, M.metal);

            // shutdown console (south wall): keypad + emergency button + screen above
            Box("ConsoleDesk", t, new Vector3(13f, 0.45f, 7.45f), new Vector3(3.2f, 0.9f, 0.75f), M.trim, true, 1f);
            Box("ConsoleTop", t, new Vector3(13f, 0.925f, 7.5f), new Vector3(3.3f, 0.05f, 0.85f), M.metal, true, 1f);
            Box("ConsoleUpright", t, new Vector3(13f, 1.4f, 7.1f), new Vector3(3.2f, 0.9f, 0.1f), M.trim, true, 1f);
            Box("ConsoleKickLight", t, new Vector3(13f, 0.06f, 7.83f), new Vector3(3.1f, 0.04f, 0.03f), M.stripCyan, false, 1f);
            res.keypad = BuildKeypad(t, new Vector3(14.1f, 1.38f, 7.18f), Quaternion.LookRotation(Vector3.back));
            Text("KeypadTitle", t, new Vector3(14.1f, 1.77f, 7.16f), Quaternion.LookRotation(Vector3.back), new Vector2(0.6f, 0.08f),
                 "SHUTDOWN CODE", 0.5f, LabelBlue);
            Box("TelemetryGlass", t, new Vector3(12.0f, 1.45f, 7.157f), new Vector3(0.9f, 0.5f, 0.012f), M.screenDark, false, 0f);
            var tt = Text("ReactorTelemetry", t, new Vector3(12.0f, 1.45f, 7.165f), Quaternion.LookRotation(Vector3.back), new Vector2(0.84f, 0.44f), "", 0.5f, new Color(1f, 0.6f, 0.4f));
            tt.alignment = TextAlignmentOptions.Left;
            var tel = tt.gameObject.AddComponent<TelemetryText>();
            tel.text = tt;
            tel.mode = TelemetryText.Mode.Reactor;

            Box("ButtonHousing", t, new Vector3(11.9f, 0.98f, 7.6f), new Vector3(0.34f, 0.06f, 0.34f), M.hazard, true, 0f);
            Cyl("ButtonCollar", t, new Vector3(11.9f, 1.03f, 7.6f), Vector3.zero, 0.11f, 0.04f, M.trim, false, 24);
            var btn = Cyl("EmergencyButton", t, new Vector3(11.9f, 1.075f, 7.6f), Vector3.zero, 0.085f, 0.05f, M.buttonRed, false, 24);
            btn.AddComponent<BoxCollider>().size = new Vector3(0.17f, 0.05f, 0.17f);
            btn.AddComponent<XRSimpleInteractable>();
            var pb = btn.AddComponent<PressButton>();
            pb.id = "EMERGENCY";
            pb.pushDirection = Vector3.down;
            pb.pushDepth = 0.02f;
            AddGlow(btn, Red, 2f);
            res.button = pb;
            Label(t, "EMERGENCY SHUTDOWN", new Vector3(11.9f, 1.45f, 7.75f), 1.1f, new Color(1f, 0.45f, 0.4f));

            res.display = WallDisplay("Display_ReactorRoom", t, new Vector3(13f, 2.75f, 7f), Vector3.forward, new Vector2(2.8f, 1.4f), 1.5f);

            // credits board (north wall, west side)
            WallDisplay("CreditsBoard", t, new Vector3(9.8f, 2.2f, 17f), Vector3.back, new Vector2(2.6f, 1.6f), 0.6f,
                "<b><color=#7FE8FF>STATION ORION</color></b>\n<size=80%>INTE 42312 VR Group Project - University of Kelaniya</size>\n\n<size=70%>" +
                "Built with Unity 6 (URP), XR Interaction Toolkit 3.5 and OpenXR.\n" +
                "All 3D geometry, textures, the Earth and star sky, and all sound effects\nare generated procedurally in code - no third-party assets.\n" +
                "Font: Liberation Sans (TextMesh Pro, SIL Open Font License).\n" +
                "AI assistance: Claude (Anthropic) helped write C# scripts - see report.\n\nTEAM: add your names here</size>");

            // escape pod beacon above the hatch (MissionManager turns it green at the end)
            var beacon = Box("EscapePodBeacon", t, new Vector3(17.9f, 2.75f, 12f), new Vector3(0.04f, 0.1f, 1.6f), M.orange, false, 0f);
            res.podGlow = AddGlow(beacon, Orange, 2f);
            Box("FloorHazard", t, new Vector3(17.75f, 0.003f, 12f), new Vector3(0.3f, 0.006f, 1.7f), M.hazard, false, 0.5f);
            return res;
        }

        static void EscapePod(RoomInfo P, RoomInfo C)
        {
            var t = P.root;
            Chair(t, new Vector3(20.25f, 0f, 11.55f), Vector3.left);
            Chair(t, new Vector3(20.25f, 0f, 12.45f), Vector3.left);
            Box("PodStripe", t, new Vector3(19.5f, 1.2f, 13.08f), new Vector3(2.6f, 0.12f, 0.03f), M.orange, false, 1f);
            Box("PodStripe", t, new Vector3(19.5f, 1.2f, 10.92f), new Vector3(2.6f, 0.12f, 0.03f), M.orange, false, 1f);
            Box("PodPanel", t, new Vector3(20.72f, 0.75f, 12f), new Vector3(0.12f, 0.35f, 0.9f), M.trim, true, 0f);
            Box("PodPanelLights", t, new Vector3(20.655f, 0.8f, 12f), new Vector3(0.01f, 0.05f, 0.6f), M.glowGreen, false, 0f);
            Text("PodSign", t, new Vector3(20.78f, 1.95f, 12f), Quaternion.LookRotation(Vector3.right), new Vector2(1.2f, 0.2f),
                 "LIFEBOAT 1", 0.8f, Orange);
            var zone = Node("EscapePodZone", t, new Vector3(19.6f, 1.15f, 12f), Quaternion.identity);
            var bc = zone.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(2.2f, 2.3f, 2.0f);
            podZoneCache = bc;
        }

        static Collider podZoneCache;

        // ================================================================ items

        static XRGrabInteractable PowerCell(Transform parent, Vector3 pos)
        {
            var go = Cyl("PowerCell", parent, pos, Vector3.zero, 0.055f, 0.28f, M.cellBody, false, 20);
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.057f; col.height = 0.35f; col.direction = 1;
            Cyl("CapTop", go.transform, new Vector3(0f, 0.155f, 0f), Vector3.zero, 0.045f, 0.03f, M.trim, false, 16);
            Cyl("CapBottom", go.transform, new Vector3(0f, -0.155f, 0f), Vector3.zero, 0.045f, 0.03f, M.trim, false, 16);
            Cyl("Terminal", go.transform, new Vector3(0f, 0.18f, 0f), Vector3.zero, 0.015f, 0.02f, M.metal, false, 10);
            Cyl("EnergyBand", go.transform, new Vector3(0f, 0.04f, 0f), Vector3.zero, 0.057f, 0.05f, M.glowYellow, false, 20);
            Cyl("EnergyBand", go.transform, new Vector3(0f, -0.07f, 0f), Vector3.zero, 0.057f, 0.02f, M.glowYellow, false, 20);
            AddGlow(go, Yellow, 1.5f);
            return MakeGrabbable(go, "PowerCell", 1.5f);
        }

        static XRGrabInteractable Canister(string name, Transform parent, Vector3 pos, Material body, Material glow, Color c, string layer)
        {
            var go = Cyl(name, parent, pos, Vector3.zero, 0.07f, 0.3f, body, false, 24);
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.072f; col.height = 0.39f; col.direction = 1;
            Cyl("CapTop", go.transform, new Vector3(0f, 0.17f, 0f), Vector3.zero, 0.062f, 0.04f, M.metal, false, 20);
            Cyl("CapBottom", go.transform, new Vector3(0f, -0.17f, 0f), Vector3.zero, 0.062f, 0.04f, M.metal, false, 20);
            Cyl("Window", go.transform, new Vector3(0f, 0.02f, 0f), Vector3.zero, 0.072f, 0.08f, glow, false, 24);
            Box("Handle", go.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.1f, 0.02f, 0.02f), M.trim, false, 0f);
            AddGlow(go, c, 1.5f);
            return MakeGrabbable(go, layer, 2f);
        }

        static XRGrabInteractable Keycard(Transform parent, Vector3 pos, Quaternion rot)
        {
            var go = Box("Keycard", parent, pos, new Vector3(0.085f, 0.13f, 0.006f), M.card, true, 0f);
            go.transform.localRotation = rot;
            Box("Stripe", go.transform, new Vector3(0f, 0.035f, 0f), new Vector3(0.085f, 0.022f, 0.0066f), M.glowCyan, false, 0f);
            Box("Chip", go.transform, new Vector3(-0.022f, -0.012f, -0.0012f), new Vector3(0.018f, 0.014f, 0.005f), M.metal, false, 0f);
            Text("CardText", go.transform, new Vector3(0.01f, -0.04f, -0.0035f), Quaternion.identity, new Vector2(0.07f, 0.035f),
                 "<b>ORION</b>\nENGINEERING ACCESS", 0.2f, new Color(0.1f, 0.12f, 0.15f));
            AddGlow(go, Cyan, 1.5f);
            return MakeGrabbable(go, "Keycard", 0.1f);
        }

        static Keypad BuildKeypad(Transform parent, Vector3 pos, Quaternion rot)
        {
            var root = Node("ShutdownKeypad", parent, pos, rot).transform;
            Box("Backplate", root, new Vector3(0f, 0f, 0.01f), new Vector3(0.42f, 0.62f, 0.02f), M.trim, false, 0f);
            Box("DisplayGlass", root, new Vector3(0f, 0.22f, -0.002f), new Vector3(0.36f, 0.1f, 0.006f), M.screenDark, false, 0f);
            var disp = Text("KeypadDisplay", root, new Vector3(0f, 0.22f, -0.007f), Quaternion.identity, new Vector2(0.34f, 0.09f), "LOCKED", 1f, new Color(1f, 0.4f, 0.3f));
            string[] ids = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0" };
            var keys = new List<PressButton>();
            for (int i = 0; i < ids.Length; i++)
            {
                int col = i % 3, row = i / 3;
                var kp = new Vector3((col - 1) * 0.12f, 0.08f - row * 0.12f, -0.015f);
                var key = Box("Key_" + ids[i], root, kp, new Vector3(0.1f, 0.1f, 0.03f), ids[i] == "C" ? M.buttonRed : M.metal, true, 0f);
                key.AddComponent<XRSimpleInteractable>();
                var b = key.AddComponent<PressButton>();
                b.id = ids[i];
                b.pushDirection = rot * Vector3.forward;
                b.pushDepth = 0.01f;
                keys.Add(b);
                Text("KeyLabel", key.transform, new Vector3(0f, 0f, -0.0165f), Quaternion.identity, new Vector2(0.08f, 0.08f), ids[i], 0.7f, new Color(0.05f, 0.05f, 0.06f));
            }
            var kpComp = root.gameObject.AddComponent<Keypad>();
            kpComp.display = disp;
            kpComp.keys = keys.ToArray();
            return kpComp;
        }

        static Transform Marker()
        {
            var marker = new GameObject("ObjectiveMarker");
            var spinner = Node("Spinner", marker.transform, Vector3.zero, Quaternion.identity);
            var sp = spinner.AddComponent<Spin>();
            sp.degreesPerSecond = new Vector3(0f, 140f, 0f);
            sp.bobHeight = 0.05f;
            var d = Box("Diamond", spinner.transform, Vector3.zero, Vector3.one * 0.09f, M.marker, false, 0f);
            d.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            var l = Node("MarkerLight", spinner.transform, Vector3.zero, Quaternion.identity).AddComponent<Light>();
            l.type = LightType.Point; l.color = Cyan; l.intensity = 0.6f; l.range = 0.8f;
            return marker.transform;
        }

        // ================================================================ outside + environment

        static void Exterior()
        {
            var ext = new GameObject("Exterior").transform;
            var earth = SmoothSphere("Earth", ext, new Vector3(-300f, -100f, 40f), 140f, M.earth, 160, 80);
            earth.transform.rotation = Quaternion.Euler(0f, 0f, 23f);
            earth.AddComponent<Spin>().degreesPerSecond = new Vector3(0f, 0.25f, 0f);

            // main truss with solar wings and radiators (seen through the Command Deck window)
            Box("Truss", ext, new Vector3(-14f, -3f, 0f), new Vector3(0.5f, 0.5f, 60f), M.metal, false, 2f);
            Box("Truss", ext, new Vector3(-14f, -2.2f, 0f), new Vector3(0.12f, 0.12f, 60f), M.trim, false, 2f);
            for (float z = -29f; z <= 29f; z += 2f)
                Box("TrussRib", ext, new Vector3(-14f, -2.6f, z), new Vector3(0.08f, 0.8f, 0.08f), M.trim, false, 1f);
            Box("Module", ext, new Vector3(-9f, -3f, 0f), new Vector3(9.5f, 2.2f, 2.2f), M.wallPod, false, 2f);
            foreach (float z in new[] { -14f, 14f })
            {
                Box("WingArm", ext, new Vector3(-19f, -3f, z), new Vector3(10f, 0.3f, 0.3f), M.metal, false, 2f);
                for (int i = 0; i < 3; i++)
                {
                    var p = Box("SolarPanel", ext, new Vector3(-17f - i * 5.2f, -3f, z + (z > 0 ? 2.3f : -2.3f)), new Vector3(5f, 0.06f, 4f), M.solar, false, 2f);
                    p.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
                    var q = Box("SolarPanel", ext, new Vector3(-17f - i * 5.2f, -3f, z - (z > 0 ? 2.3f : -2.3f)), new Vector3(5f, 0.06f, 4f), M.solar, false, 2f);
                    q.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
                }
            }
            for (int i = 0; i < 3; i++)
                Box("Radiator", ext, new Vector3(-11f - i * 1.2f, 0.5f, -6f), new Vector3(0.05f, 3f, 2.5f), M.ceiling, false, 1f);
            SetLayerRecursive(ext.gameObject, ExteriorLayer);
        }

        static void Environment()
        {
            // sun: lights only the outside (Earth, solar wings); shadows on as a fallback
            var sunGo = new GameObject("Sun");
            sunGo.transform.rotation = Quaternion.LookRotation(new Vector3(-0.55f, -0.45f, 0.7f));
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;
            sun.cullingMask = 1 << ExteriorLayer;
            RenderSettings.sun = sun;

            // star-field sky
            string skyPath = MatDir + "/SO_SkyStars.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            var skyShader = Shader.Find("Skybox/Panoramic");
            if (sky == null) { sky = new Material(skyShader); AssetDatabase.CreateAsset(sky, skyPath); }
            sky.shader = skyShader;
            sky.SetTexture("_MainTex", T.stars);
            sky.SetFloat("_Mapping", 1f);
            sky.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
            sky.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");
            sky.SetFloat("_ImageType", 0f);
            sky.SetFloat("_Exposure", 1.3f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.26f, 0.3f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.17f, 0.19f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.08f, 0.09f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.fog = false;

            // post-processing
            string pp = GenRoot + "/SO_PostFX.asset";
            AssetDatabase.DeleteAsset(pp);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, pp);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.6f);
            var tm = profile.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.ACES);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.35f);
            ca.contrast.Override(10f);
            ca.saturation.Override(5f);
            var vg = profile.Add<Vignette>(true);
            vg.intensity.Override(0.18f);
            foreach (var comp in profile.components)
            {
                comp.name = comp.GetType().Name;
                AssetDatabase.AddObjectToAsset(comp, profile);
            }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            var vol = new GameObject("PostProcessing").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.sharedProfile = profile;
        }

        static void PlacePlayer(Vector3 pos)
        {
            var prefab = FindRigPrefab();
            if (prefab == null)
            {
                log.AppendLine("WARNING: XR Origin (XR Rig) prefab not found. Drag it from Samples/XR Interaction Toolkit/.../Starter Assets/Prefabs to (0, 0, -2.6).");
                var camGo = new GameObject("Main Camera (no XR rig found)");
                camGo.tag = "MainCamera";
                camGo.transform.position = pos + Vector3.up * 1.6f;
                camGo.AddComponent<Camera>().farClipPlane = 1500f;
                return;
            }
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.transform.position = pos;
            rig.transform.rotation = Quaternion.identity;
            foreach (var cam in rig.GetComponentsInChildren<Camera>(true))
            {
                cam.farClipPlane = 1500f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(cam);
                var acd = cam.GetComponent<UniversalAdditionalCameraData>();
                if (acd != null)
                {
                    acd.renderPostProcessing = true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(acd);
                }
            }
            log.AppendLine("XR rig placed in the Command Deck, facing the mission screen and Door 1.");
        }
    }
}
