using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace StationOrion.EditorTools
{
    /// <summary>Architecture pieces: rooms, walls with door/window openings, doors, windows, props.</summary>
    public static partial class StationBuilder
    {
        enum OpKind { Door, Window, Clear }

        struct Opening
        {
            public float at, width, bottom, top;
            public OpKind kind;
        }

        static Opening DoorOp(float at, float width = 1.6f, float height = 2.2f)
        {
            return new Opening { at = at, width = width, bottom = 0f, top = height, kind = OpKind.Door };
        }

        static Opening WinOp(float at, float width, float bottom, float top)
        {
            return new Opening { at = at, width = width, bottom = bottom, top = top, kind = OpKind.Window };
        }

        /// <summary>No hole, just keeps decorative ribs away (for screens, shelves, machines on the wall).</summary>
        static Opening ClearOp(float at, float width)
        {
            return new Opening { at = at, width = width, bottom = 0f, top = 0f, kind = OpKind.Clear };
        }

        class WallInfo
        {
            public Vector3 a, along, inward;
            public float length, height;
            public List<Opening> ops = new List<Opening>();
        }

        class RoomInfo
        {
            public string name;
            public Transform root;
            public float x0, x1, z0, z1, h;
            public List<Light> lights = new List<Light>();
            public List<Glow> panels = new List<Glow>();
            public List<Behaviour> teleports = new List<Behaviour>();
            public Vector3 Center { get { return new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f); } }
        }

        // ================================================================ rooms

        static RoomInfo Room(string name, float x0, float x1, float z0, float z1, float h,
                             Opening[] north, Opening[] south, Opening[] east, Opening[] west,
                             bool lightPanels = true, Material wallMat = null)
        {
            var r = new RoomInfo { name = name, x0 = x0, x1 = x1, z0 = z0, z1 = z1, h = h };
            r.root = Node(name, world, Vector3.zero, Quaternion.identity).transform;
            if (wallMat == null) wallMat = M.wall;

            // floor (2 m tiles so lights are picked per tile) + one invisible teleport/physics floor
            var floor = Node("Floor", r.root, Vector3.zero, Quaternion.identity).transform;
            var ceil = Node("Ceiling", r.root, Vector3.zero, Quaternion.identity).transform;
            bool longX = (x1 - x0) >= (z1 - z0);
            for (float x = x0; x < x1 - 0.01f; x += 2f)
                for (float z = z0; z < z1 - 0.01f; z += 2f)
                {
                    float tw = Mathf.Min(2f, x1 - x), td = Mathf.Min(2f, z1 - z);
                    Vector3 c = new Vector3(x + tw * 0.5f, 0f, z + td * 0.5f);
                    Box("FloorTile", floor, c + Vector3.down * 0.05f, new Vector3(tw, 0.1f, td), M.floor, false, 1f);
                    Box("CeilingTile", ceil, c + Vector3.up * (h + 0.125f), new Vector3(tw, 0.25f, td), M.ceiling, false, 1f);
                    if (lightPanels && tw > 1.5f && td > 1.5f)
                    {
                        var size = longX ? new Vector3(1.1f, 0.03f, 0.45f) : new Vector3(0.45f, 0.03f, 1.1f);
                        var p = Box("LightPanel", ceil, c + Vector3.up * (h - 0.015f), size, M.lightPanel, false, 0f);
                        r.panels.Add(AddGlow(p, Color.white, 1.4f, true));
                    }
                }
            r.teleports.Add(MakeTeleportFloor(r.root, "TeleportFloor",
                new Vector3((x0 + x1) * 0.5f, -0.05f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.1f, z1 - z0)));

            // ceiling beams across the short direction
            if (longX)
                for (float x = x0 + 2f; x < x1 - 0.5f; x += 2f)
                    Box("Beam", ceil, new Vector3(x, h - 0.11f, (z0 + z1) * 0.5f), new Vector3(0.22f, 0.22f, z1 - z0), M.trim, false, 1f);
            else
                for (float z = z0 + 2f; z < z1 - 0.5f; z += 2f)
                    Box("Beam", ceil, new Vector3((x0 + x1) * 0.5f, h - 0.11f, z), new Vector3(x1 - x0, 0.22f, 0.22f), M.trim, false, 1f);

            if (north != null) Wall(r, "Wall_North", new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), Vector3.back, wallMat, north);
            if (south != null) Wall(r, "Wall_South", new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0), Vector3.forward, wallMat, south);
            if (east != null) Wall(r, "Wall_East", new Vector3(x1, 0f, z0), new Vector3(x1, 0f, z1), Vector3.left, wallMat, east);
            if (west != null) Wall(r, "Wall_West", new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1), Vector3.right, wallMat, west);

            var probeGo = Node("ReflectionProbe", r.root, new Vector3((x0 + x1) * 0.5f, h * 0.5f, (z0 + z1) * 0.5f), Quaternion.identity);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3(x1 - x0, h, z1 - z0);
            probe.boxProjection = true;
            probe.resolution = 128;

            var hum = Node("VentilationHum", r.root, new Vector3((x0 + x1) * 0.5f, h - 0.3f, (z0 + z1) * 0.5f), Quaternion.identity);
            var al = hum.AddComponent<AmbientLoop>();
            al.volume = 0.08f;
            al.pitch = 1.6f;
            al.maxDistance = 7f;
            return r;
        }

        static RoomPower FinishPower(RoomInfo r)
        {
            var p = r.root.gameObject.AddComponent<RoomPower>();
            p.lights = r.lights.ToArray();
            p.panels = r.panels.ToArray();
            p.panelColor = new Color(0.95f, 0.97f, 1f);
            return p;
        }

        static Light RoomLight(RoomInfo r, Vector3 pos, float intensity, float range, bool shadows = false)
        {
            var l = PointLight("CeilingLight", r.root, pos, new Color(0.86f, 0.92f, 1f), intensity, range, shadows);
            r.lights.Add(l);
            return l;
        }

        // ================================================================ walls

        static WallInfo Wall(RoomInfo r, string name, Vector3 a, Vector3 b, Vector3 inward, Material mat, Opening[] ops)
        {
            var w = new WallInfo { a = a, along = (b - a).normalized, inward = inward, length = Vector3.Distance(a, b), height = r.h };
            w.ops.AddRange(ops);
            var root = Node(name, r.root, Vector3.zero, Quaternion.identity).transform;

            float cursor = -WallThick;   // overlap corners
            foreach (var o in ops.Where(o => o.kind != OpKind.Clear).OrderBy(o => o.at))
            {
                float s0 = o.at - o.width * 0.5f, s1 = o.at + o.width * 0.5f;
                if (s0 > cursor + 0.001f) Piece(root, w, cursor, s0, 0f, r.h, mat);
                if (o.bottom > 0.001f) Piece(root, w, s0, s1, 0f, o.bottom, mat);
                if (o.top < r.h - 0.001f) Piece(root, w, s0, s1, o.top, r.h, mat);
                cursor = s1;
            }
            if (w.length + WallThick > cursor + 0.001f) Piece(root, w, cursor, w.length + WallThick, 0f, r.h, mat);

            WallDetails(root, w);
            foreach (var o in ops) if (o.kind == OpKind.Window) Window(root, w, o);
            return w;
        }

        static void Piece(Transform root, WallInfo w, float s0, float s1, float y0, float y1, Material mat)
        {
            Vector3 c = w.a + w.along * ((s0 + s1) * 0.5f) + Vector3.up * ((y0 + y1) * 0.5f) - w.inward * (WallThick * 0.5f);
            Vector3 size = Abs(w.along) * (s1 - s0) + Vector3.up * (y1 - y0) + Abs(w.inward) * WallThick;
            Box("WallPiece", root, c, size, mat, true, 1f);
        }

        // solid intervals of the wall at height y (openings cut out, with a margin)
        static List<Vector2> Spans(WallInfo w, float y, float margin)
        {
            var res = new List<Vector2>();
            float cur = 0f;
            foreach (var o in w.ops.Where(o => o.kind != OpKind.Clear).OrderBy(o => o.at))
            {
                if (y < o.bottom - margin || y > o.top + margin) continue;
                float s0 = o.at - o.width * 0.5f - margin, s1 = o.at + o.width * 0.5f + margin;
                if (s0 > cur) res.Add(new Vector2(cur, s0));
                cur = Mathf.Max(cur, s1);
            }
            if (cur < w.length) res.Add(new Vector2(cur, w.length));
            return res;
        }

        static bool FreeForRib(WallInfo w, float s, float margin)
        {
            foreach (var o in w.ops)
                if (s + margin > o.at - o.width * 0.5f && s - margin < o.at + o.width * 0.5f) return false;
            return true;
        }

        static void Strip(Transform root, WallInfo w, Vector2 sp, float y, float height, float depth, Material mat)
        {
            if (sp.y - sp.x < 0.05f) return;
            Box("Trim", root, w.a + w.along * ((sp.x + sp.y) * 0.5f) + Vector3.up * y + w.inward * (depth * 0.5f),
                Abs(w.along) * (sp.y - sp.x) + Vector3.up * height + Abs(w.inward) * depth, mat, false, 1f);
        }

        static void WallDetails(Transform root, WallInfo w)
        {
            foreach (var sp in Spans(w, 0.05f, 0.17f)) Strip(root, w, sp, 0.075f, 0.15f, 0.03f, M.trim);       // skirting
            foreach (var sp in Spans(w, 1.0f, 0.17f)) Strip(root, w, sp, 1.0f, 0.05f, 0.025f, M.trim);         // hand rail
            float yl = w.height - 0.32f;
            foreach (var sp in Spans(w, yl, 0.2f))
            {
                Strip(root, w, sp, yl, 0.035f, 0.03f, M.stripCyan);                                           // cove light
                Strip(root, w, sp, yl + 0.05f, 0.03f, 0.09f, M.trim);
            }
            for (float s = 1f; s < w.length - 0.4f; s += 2f)                                                   // structural ribs
                if (FreeForRib(w, s, 0.3f))
                    Box("Rib", root, w.a + w.along * s + Vector3.up * (w.height * 0.5f) + w.inward * 0.045f,
                        Abs(w.along) * 0.2f + Vector3.up * w.height + Abs(w.inward) * 0.09f, M.trim, false, 1f);
        }

        static void Window(Transform root, WallInfo w, Opening o)
        {
            Vector3 c = w.a + w.along * o.at - w.inward * (WallThick * 0.5f);
            float wd = o.width, ht = o.top - o.bottom, ym = (o.top + o.bottom) * 0.5f;
            Vector3 al = Abs(w.along), inw = Abs(w.inward), up = Vector3.up;
            float d = WallThick + 0.08f, f = 0.09f;
            Box("WindowFrame", root, c + up * (o.top + f * 0.5f), al * (wd + 2f * f) + up * f + inw * d, M.trim, false, 1f);
            Box("WindowFrame", root, c + up * (o.bottom - f * 0.5f), al * (wd + 2f * f) + up * f + inw * d, M.trim, false, 1f);
            Box("WindowFrame", root, c + w.along * (wd * 0.5f + f * 0.5f) + up * ym, al * f + up * (ht + 2f * f) + inw * d, M.trim, false, 1f);
            Box("WindowFrame", root, c - w.along * (wd * 0.5f + f * 0.5f) + up * ym, al * f + up * (ht + 2f * f) + inw * d, M.trim, false, 1f);
            int mull = Mathf.Max(1, Mathf.RoundToInt(wd / 1.3f));
            for (int i = 1; i < mull; i++)
                Box("Mullion", root, c + w.along * (-wd * 0.5f + wd * i / mull) + up * ym, al * 0.06f + up * ht + inw * (WallThick * 0.7f), M.trim, false, 1f);
            Box("Glass", root, c + up * ym, al * wd + up * ht + inw * 0.02f, M.glass, true, 1f);
            if (o.bottom > 0.5f)
                Box("Sill", root, c + w.inward * (WallThick * 0.5f + 0.1f) + up * (o.bottom - 0.02f), al * (wd + 0.1f) + up * 0.04f + inw * 0.22f, M.metal, true, 1f);
        }

        // ================================================================ doors

        /// <summary>
        /// Door frame (+ optional sliding panels) standing on the wall centre plane at 'center'.
        /// Local -Z side faces the room the wall belongs to when 'along' runs a->b of that wall in the
        /// same direction as the builder uses; signs can be put on either face.
        /// </summary>
        static DoorController DoorAt(Transform parent, string name, Vector3 center, Vector3 along, float width, float height,
                                     bool panels, bool functional, string signMinusZ, string signPlusZ, bool openLook = false)
        {
            var root = Node(name, parent, center, Quaternion.LookRotation(Vector3.Cross(along, Vector3.up), Vector3.up)).transform;
            float fw = 0.16f, depth = WallThick + 0.14f;
            Box("Jamb_L", root, new Vector3(-(width + fw) * 0.5f, (height + fw) * 0.5f, 0f), new Vector3(fw, height + fw, depth), M.trim, true, 0f);
            Box("Jamb_R", root, new Vector3((width + fw) * 0.5f, (height + fw) * 0.5f, 0f), new Vector3(fw, height + fw, depth), M.trim, true, 0f);
            Box("Header", root, new Vector3(0f, height + fw * 0.5f, 0f), new Vector3(width + 2f * fw, fw, depth), M.trim, true, 0f);

            var glows = new List<Glow>();
            Color startCol = openLook ? new Color(0.2f, 1f, 0.35f) : new Color(1f, 0.15f, 0.1f);
            for (int side = -1; side <= 1; side += 2)
            {
                float zf = side * (depth * 0.5f + 0.004f);
                Box("Hazard", root, new Vector3(-(width + fw) * 0.5f, height * 0.5f, zf), new Vector3(fw * 0.55f, height * 0.92f, 0.008f), M.hazard, false, 0f);
                Box("Hazard", root, new Vector3((width + fw) * 0.5f, height * 0.5f, zf), new Vector3(fw * 0.55f, height * 0.92f, 0.008f), M.hazard, false, 0f);
                var st = Box("StatusLight", root, new Vector3(0f, height + fw * 0.5f, side * (depth * 0.5f + 0.008f)), new Vector3(width * 0.6f, 0.05f, 0.012f), M.statusLight, false, 0f);
                glows.Add(AddGlow(st, startCol, 2f, true));
            }

            DoorController dc = null;
            if (panels)
            {
                var left = Node("Panel_L", root, new Vector3(-width * 0.25f, 0f, 0f), Quaternion.identity).transform;
                var right = Node("Panel_R", root, new Vector3(width * 0.25f, 0f, 0f), Quaternion.identity).transform;
                DoorPanel(left, width * 0.5f + 0.03f, height, 1f);
                DoorPanel(right, width * 0.5f + 0.03f, height, -1f);
                if (functional)
                {
                    dc = root.gameObject.AddComponent<DoorController>();
                    dc.leftPanel = left;
                    dc.rightPanel = right;
                    dc.slideDistance = width * 0.5f + 0.02f;
                    dc.speed = 0.9f;
                    dc.statusLights = glows.ToArray();
                }
            }

            float signY = height + fw + 0.2f;
            if (!string.IsNullOrEmpty(signMinusZ))
                Text("Sign", root, new Vector3(0f, signY, -(depth * 0.5f + 0.01f)), Quaternion.identity, new Vector2(width + 0.6f, 0.26f), signMinusZ, 1.2f, new Color(0.6f, 0.9f, 1f));
            if (!string.IsNullOrEmpty(signPlusZ))
                Text("Sign", root, new Vector3(0f, signY, depth * 0.5f + 0.01f), Quaternion.Euler(0f, 180f, 0f), new Vector2(width + 0.6f, 0.26f), signPlusZ, 1.2f, new Color(0.6f, 0.9f, 1f));
            return dc;
        }

        static void DoorPanel(Transform t, float pw, float height, float edgeDir)
        {
            Box("Slab", t, new Vector3(0f, height * 0.5f, 0f), new Vector3(pw, height, 0.08f), M.door, true, 1f);
            Box("HazardBand", t, new Vector3(0f, 1.0f, 0f), new Vector3(pw, 0.14f, 0.092f), M.hazard, false, 0f);
            Box("Viewport", t, new Vector3(edgeDir * 0.12f, 1.55f, 0f), new Vector3(0.12f, 0.42f, 0.09f), M.glassDark, false, 0f);
            Box("EdgeLight", t, new Vector3(edgeDir * (pw * 0.5f - 0.015f), height * 0.5f, 0f), new Vector3(0.025f, height * 0.85f, 0.088f), M.stripCyan, false, 0f);
            Box("Plate", t, new Vector3(0f, 0.25f, 0f), new Vector3(pw * 0.8f, 0.3f, 0.086f), M.trim, false, 0f);
        }

        // ================================================================ props

        static Light Beacon(Transform parent, Vector3 ceilingPos)
        {
            var root = Node("AlarmBeacon", parent, ceilingPos, Quaternion.identity).transform;
            Cyl("Base", root, new Vector3(0f, -0.04f, 0f), Vector3.zero, 0.09f, 0.08f, M.trim);
            Sphere("Dome", root, new Vector3(0f, -0.12f, 0f), 0.08f, M.beaconRed);
            var spinner = Node("Spinner", root, new Vector3(0f, -0.12f, 0f), Quaternion.identity);
            spinner.AddComponent<Spin>().degreesPerSecond = new Vector3(0f, 240f, 0f);
            var lg = Node("RedLight", spinner.transform, Vector3.zero, Quaternion.Euler(28f, 0f, 0f));
            var l = lg.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 80f;
            l.range = 9f;
            l.intensity = 9f;
            l.color = new Color(1f, 0.1f, 0.05f);
            l.shadows = LightShadows.None;
            return l;
        }

        static void Chair(Transform parent, Vector3 pos, Vector3 facing)
        {
            var root = Node("Chair", parent, pos, Quaternion.LookRotation(facing, Vector3.up)).transform;
            Cyl("Base", root, new Vector3(0f, 0.03f, 0f), Vector3.zero, 0.28f, 0.06f, M.trim);
            Cyl("Post", root, new Vector3(0f, 0.25f, 0f), Vector3.zero, 0.035f, 0.4f, M.metal);
            Box("Seat", root, new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.09f, 0.5f), M.chair, true, 0f);
            Box("Back", root, new Vector3(0f, 0.9f, -0.24f), new Vector3(0.5f, 0.7f, 0.08f), M.chair, true, 0f);
            Box("Headrest", root, new Vector3(0f, 1.34f, -0.24f), new Vector3(0.3f, 0.16f, 0.08f), M.chair, false, 0f);
            Box("Arm", root, new Vector3(-0.27f, 0.68f, -0.02f), new Vector3(0.05f, 0.05f, 0.42f), M.trim, false, 0f);
            Box("Arm", root, new Vector3(0.27f, 0.68f, -0.02f), new Vector3(0.05f, 0.05f, 0.42f), M.trim, false, 0f);
        }

        /// <summary>Cargo crate; the stencil text is put on the side facing 'face'.</summary>
        static GameObject Crate(Transform parent, Vector3 floorPos, float size, string stencil, Vector3 face)
        {
            var c = Box("Crate", parent, floorPos + Vector3.up * (size * 0.5f), Vector3.one * size, M.crate, true, 0f);
            if (!string.IsNullOrEmpty(stencil))
                Text("Stencil", c.transform, face * (size * 0.5f + 0.003f), Quaternion.LookRotation(-face),
                     new Vector2(size * 0.38f, size * 0.15f), stencil, 0.5f, new Color(0.08f, 0.08f, 0.08f));
            return c;
        }

        /// <summary>Open metal shelf rack. 'facing' = direction the open side faces.</summary>
        static void Rack(Transform parent, Vector3 floorCenter, Vector3 facing, float width, float depth, float height, float[] shelves)
        {
            var root = Node("Rack", parent, floorCenter, Quaternion.LookRotation(facing, Vector3.up)).transform;
            foreach (float sx in new[] { -width * 0.5f + 0.02f, width * 0.5f - 0.02f })
                foreach (float sz in new[] { -depth * 0.5f + 0.02f, depth * 0.5f - 0.02f })
                    Box("Post", root, new Vector3(sx, height * 0.5f, sz), new Vector3(0.04f, height, 0.04f), M.trim, false, 0f);
            foreach (float y in shelves)
                Box("Shelf", root, new Vector3(0f, y, 0f), new Vector3(width, 0.03f, depth), M.metal, true, 0f);
            Box("Back", root, new Vector3(0f, height * 0.5f, -depth * 0.5f), new Vector3(width, height, 0.015f), M.vent, false, 0f);
        }

        /// <summary>Ring made of segments (looks like a machined ring), optionally spinning.</summary>
        static Spin Ring(Transform parent, string name, Vector3 pos, float radius, int segments, Vector3 seg, Material mat, float spinY)
        {
            var root = Node(name, parent, pos, Quaternion.identity);
            for (int i = 0; i < segments; i++)
            {
                float a = i * 360f / segments;
                var q = Quaternion.Euler(0f, a, 0f);
                var piece = Box("Segment", root.transform, q * new Vector3(0f, 0f, radius), seg, mat, false, 0f);
                piece.transform.localRotation = q;
            }
            if (Mathf.Abs(spinY) < 0.01f) return null;
            var sp = root.AddComponent<Spin>();
            sp.degreesPerSecond = new Vector3(0f, spinY, 0f);
            return sp;
        }

        static Mesh SphereMesh(float r, int lon, int lat)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            for (int y = 0; y <= lat; y++)
            {
                float fv = y / (float)lat;
                float th = (fv - 0.5f) * Mathf.PI;
                for (int x = 0; x <= lon; x++)
                {
                    float fu = x / (float)lon;
                    float ph = fu * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Cos(th) * Mathf.Cos(ph), Mathf.Sin(th), Mathf.Cos(th) * Mathf.Sin(ph));
                    v.Add(d * r); n.Add(d); uv.Add(new Vector2(fu, fv));
                }
            }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int i = y * (lon + 1) + x;
                    tri.Add(i); tri.Add(i + lon + 1); tri.Add(i + lon + 2);
                    tri.Add(i); tri.Add(i + lon + 2); tri.Add(i + 1);
                }
            var m = new Mesh { name = "SO_Sphere" };
            m.indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        }

        static GameObject SmoothSphere(string name, Transform parent, Vector3 pos, float radius, Material mat, int lon = 96, int lat = 48)
        {
            var go = Node(name, parent, pos, Quaternion.identity);
            go.AddComponent<MeshFilter>().sharedMesh = SphereMesh(radius, lon, lat);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>A desk-mounted console screen tilted back, with optional live telemetry text.</summary>
        static void ConsoleScreen(Transform parent, Vector3 pos, Vector3 facing, Vector2 size, bool graph, TelemetryText.Mode mode)
        {
            var root = Node("ConsoleScreen", parent, pos, Quaternion.LookRotation(-facing, Vector3.up) * Quaternion.Euler(18f, 0f, 0f)).transform;
            // local -z faces the viewer
            Box("Bezel", root, new Vector3(0f, 0f, 0.015f), new Vector3(size.x + 0.05f, size.y + 0.05f, 0.03f), M.trim, false, 0f);
            Box("Display", root, new Vector3(0f, 0f, -0.002f), new Vector3(size.x, size.y, 0.006f), graph ? M.screen : M.screenDark, false, 0f);
            if (!graph)
            {
                var t = Text("Telemetry", root, new Vector3(0f, 0f, -0.008f), Quaternion.identity, size - new Vector2(0.04f, 0.03f), "", 0.5f, new Color(0.45f, 0.9f, 1f));
                t.alignment = TMPro.TextAlignmentOptions.Left;
                var tel = t.gameObject.AddComponent<TelemetryText>();
                tel.text = t;
                tel.mode = mode;
            }
        }
    }
}
