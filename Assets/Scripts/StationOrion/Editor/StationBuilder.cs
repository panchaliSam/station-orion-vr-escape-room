using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace StationOrion.EditorTools
{
    /// <summary>
    /// Menu: Station Orion > Build Realistic Station (new scene)
    ///
    /// Builds the whole game from code into a NEW scene (Assets/Scenes/StationOrion_Realistic.unity):
    /// a detailed space station (Command Deck, Engine Room, Reactor Room, corridors, escape pod),
    /// procedural PBR textures, lighting, post-processing, a star-field sky with Earth outside the
    /// window, the XR rig and every game system (tasks, doors, timer, screens, guidance).
    /// Running it again rebuilds the scene from scratch. The old Main_StationOrion scene is untouched.
    /// This file holds the building blocks; StationBuilder.Layout.cs places everything.
    /// </summary>
    public static partial class StationBuilder
    {
        public const string ScenePath = "Assets/Scenes/StationOrion_Realistic.unity";
        const string GenRoot = "Assets/StationOrion/Generated";
        const string MatDir = GenRoot + "/Materials";
        const int ExteriorLayer = 30;     // Earth, solar arrays: lit by the sun only
        const float WallThick = 0.2f;

        static StringBuilder log;
        static StationTextures.Set T;
        static Mats M;
        static Transform world;
        static Dictionary<string, Mesh> meshCache;

        // ================================================================ entry point

        [MenuItem("Station Orion/Build Realistic Station (new scene)", priority = 0)]
        public static void BuildMenu()
        {
            if (!EditorUtility.DisplayDialog("Station Orion",
                    "This builds the complete realistic station into a NEW scene:\n\n" + ScenePath +
                    "\n\nIf that scene already exists it is rebuilt from scratch. Your other scenes are not changed.\n\nThe first build generates textures and can take up to a minute.",
                    "Build", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        public static void Build()
        {
            log = new StringBuilder();
            meshCache = new Dictionary<string, Mesh>();
            try
            {
                EnsureFolder(GenRoot);
                EnsureFolder(MatDir);
                EnsureFolder(StationTextures.Folder);
                EnsureFolder("Assets/Scenes");

                T = StationTextures.EnsureAll();
                EditorUtility.DisplayProgressBar("Station Orion", "Creating materials...", 0.9f);
                M = Mats.Create(T);

                EditorUtility.DisplayProgressBar("Station Orion", "Building the station...", 0.93f);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                world = new GameObject("Station").transform;

                BuildLayout();   // StationBuilder.Layout.cs

                EditorUtility.DisplayProgressBar("Station Orion", "Saving scene...", 0.98f);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AddSceneToBuild(ScenePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log("[Station Orion] Realistic station built: " + ScenePath + "\n" + log);
            EditorUtility.DisplayDialog("Station Orion",
                "Done! The station is in\n" + ScenePath + " (now open).\n\nPress Play to test.\n\nAny notes are in the Console.", "OK");
        }

        // ================================================================ materials

        public class Mats
        {
            public Material wall, wallPod, floor, ceiling, trim, metal, door, rubber, hazard, crate, vent, glass, glassDark;
            public Material lightPanel, stripCyan, stripWarm, screen, screenDark, holo, earth, solar, padBase;
            public Material cellBody, canRed, canBlue, canGreen, card, glowYellow, glowRed, glowBlue, glowGreen, glowCyan;
            public Material reactorCore, reactorRod, buttonRed, orange, chair, locker, scannerPad, beaconRed, marker, statusLight, cable;

            public static Mats Create(StationTextures.Set t)
            {
                var m = new Mats();
                m.wall = Lit("Wall", Color.white, 0.15f, 0.45f, t.wallAlbedo, t.wallNormal, 0.8f);
                m.wallPod = Lit("WallPod", new Color(0.95f, 0.93f, 0.88f), 0.15f, 0.5f, t.wallAlbedo, t.wallNormal, 0.8f);
                m.floor = Lit("Floor", Color.white, 0.75f, 0.5f, t.floorAlbedo, t.floorNormal, 1f);
                m.ceiling = Lit("Ceiling", Color.white, 0.1f, 0.3f, t.ceilAlbedo, t.ceilNormal, 0.6f);
                m.trim = Lit("TrimDark", new Color(0.16f, 0.17f, 0.19f), 0.8f, 0.55f);
                m.metal = Lit("Metal", new Color(0.62f, 0.64f, 0.67f), 0.9f, 0.68f);
                m.door = Lit("DoorPanel", new Color(0.46f, 0.49f, 0.53f), 0.7f, 0.55f, t.wallAlbedo, t.wallNormal, 0.6f);
                m.rubber = Lit("Rubber", new Color(0.05f, 0.05f, 0.05f), 0f, 0.2f);
                m.hazard = Lit("Hazard", Color.white, 0.3f, 0.4f, t.hazard);
                m.crate = Lit("Crate", Color.white, 0.4f, 0.35f, t.crateAlbedo, t.crateNormal, 1f);
                m.vent = Lit("Vent", Color.white, 0.6f, 0.4f, t.vent, t.ventNormal, 1f);
                m.glass = Transparent("Glass", new Color(0.55f, 0.7f, 0.8f, 0.12f), 0.97f);
                m.glassDark = Lit("GlassDark", new Color(0.03f, 0.05f, 0.07f), 0.2f, 0.95f);
                m.lightPanel = Lit("LightPanel", new Color(0.9f, 0.92f, 0.95f), 0f, 0.5f, null, null, 1f, new Color(1.3f, 1.35f, 1.4f));
                m.stripCyan = Lit("StripCyan", new Color(0.4f, 0.8f, 0.9f), 0f, 0.5f, null, null, 1f, new Color(0.35f, 1.1f, 1.5f));
                m.stripWarm = Lit("StripWarm", new Color(0.9f, 0.7f, 0.4f), 0f, 0.5f, null, null, 1f, new Color(1.5f, 0.9f, 0.4f));
                m.screen = Lit("Screen", Color.black, 0f, 0.9f, null, null, 1f, Color.white * 1.3f, t.screen);
                m.screenDark = Lit("ScreenDark", new Color(0.015f, 0.025f, 0.04f), 0.1f, 0.92f);
                m.holo = Lit("Hologram", Color.black, 0f, 0.5f, null, null, 1f, new Color(0.3f, 1.2f, 2.2f));
                m.earth = Lit("Earth", Color.white, 0f, 0.35f, t.earth);
                m.solar = Lit("SolarPanel", Color.white, 0.6f, 0.85f, t.solar);
                m.padBase = Lit("Pad", new Color(0.18f, 0.18f, 0.2f), 0.6f, 0.6f, null, null, 1f, new Color(0.05f, 0.05f, 0.05f));
                m.cellBody = Lit("PowerCell", new Color(0.95f, 0.75f, 0.12f), 0.5f, 0.65f, null, null, 1f, new Color(0.05f, 0.04f, 0f));
                m.canRed = Lit("CanisterRed", new Color(0.75f, 0.07f, 0.05f), 0.5f, 0.7f, null, null, 1f, new Color(0.05f, 0f, 0f));
                m.canBlue = Lit("CanisterBlue", new Color(0.07f, 0.25f, 0.85f), 0.5f, 0.7f, null, null, 1f, new Color(0f, 0.01f, 0.05f));
                m.canGreen = Lit("CanisterGreen", new Color(0.1f, 0.65f, 0.2f), 0.5f, 0.7f, null, null, 1f, new Color(0f, 0.05f, 0.01f));
                m.card = Lit("Keycard", new Color(0.9f, 0.92f, 0.95f), 0.1f, 0.8f, null, null, 1f, new Color(0.02f, 0.02f, 0.02f));
                m.glowYellow = Glowing("GlowYellow", new Color(1.6f, 1.2f, 0.2f));
                m.glowRed = Glowing("GlowRed", new Color(1.8f, 0.15f, 0.1f));
                m.glowBlue = Glowing("GlowBlue", new Color(0.2f, 0.6f, 2f));
                m.glowGreen = Glowing("GlowGreen", new Color(0.2f, 1.8f, 0.4f));
                m.glowCyan = Glowing("GlowCyan", new Color(0.3f, 1.4f, 1.8f));
                m.reactorCore = Lit("ReactorCore", new Color(0.35f, 0.08f, 0.03f), 0f, 0.8f, null, null, 1f, new Color(1f, 0.3f, 0.1f));
                m.reactorRod = Glowing("ReactorRod", new Color(1.6f, 0.6f, 0.15f));
                m.buttonRed = Lit("ButtonRed", new Color(0.8f, 0.05f, 0.04f), 0.2f, 0.7f, null, null, 1f, new Color(0.3f, 0f, 0f));
                m.orange = Lit("PodOrange", new Color(0.9f, 0.42f, 0.08f), 0.3f, 0.55f, null, null, 1f, new Color(0.05f, 0.02f, 0f));
                m.chair = Lit("Seat", new Color(0.12f, 0.13f, 0.15f), 0.05f, 0.25f);
                m.locker = Lit("Locker", new Color(0.3f, 0.34f, 0.38f), 0.7f, 0.45f, t.wallAlbedo, t.wallNormal, 0.5f);
                m.scannerPad = Lit("ScannerPad", new Color(0.05f, 0.1f, 0.12f), 0.2f, 0.9f, null, null, 1f, new Color(0.1f, 0.4f, 0.5f));
                m.beaconRed = Glowing("BeaconRed", new Color(2f, 0.1f, 0.05f));
                m.marker = Glowing("Marker", new Color(0.4f, 1.6f, 2.2f));
                m.statusLight = Lit("StatusLight", new Color(0.2f, 0.2f, 0.2f), 0f, 0.6f, null, null, 1f, new Color(1f, 0.1f, 0.1f));
                m.cable = Lit("Cable", new Color(0.08f, 0.08f, 0.09f), 0.1f, 0.35f);
                return m;
            }

            static Material Base(string name)
            {
                string path = MatDir + "/SO_" + name + ".mat";
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, path);
                }
                else mat.shader = shader;
                return mat;
            }

            static Material Lit(string name, Color col, float metallic, float smooth, Texture2D albedo = null,
                                Texture2D normal = null, float normalScale = 1f, Color? emission = null, Texture2D emissionMap = null)
            {
                var m = Base(name);
                m.SetColor("_BaseColor", col);
                m.SetTexture("_BaseMap", albedo);
                m.SetFloat("_Metallic", metallic);
                m.SetFloat("_Smoothness", smooth);
                if (normal != null)
                {
                    m.SetTexture("_BumpMap", normal);
                    m.SetFloat("_BumpScale", normalScale);
                    m.EnableKeyword("_NORMALMAP");
                }
                else m.DisableKeyword("_NORMALMAP");
                if (emission.HasValue)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", emission.Value);
                    m.SetTexture("_EmissionMap", emissionMap);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    m.DisableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", Color.black);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
                EditorUtility.SetDirty(m);
                return m;
            }

            static Material Glowing(string name, Color emission)
            {
                return Lit(name, Color.black, 0f, 0.6f, null, null, 1f, emission);
            }

            static Material Transparent(string name, Color col, float smooth)
            {
                var m = Lit(name, col, 0f, smooth);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
                EditorUtility.SetDirty(m);
                return m;
            }
        }

        // ================================================================ meshes

        /// <summary>Box mesh whose UVs are in metres / tile (so textures keep their real size and line up).
        /// tile &lt;= 0 maps every face 0..1 (crates, screens).</summary>
        static Mesh BoxMesh(Vector3 size, float tile, Vector3 uvOffset)
        {
            var v = new List<Vector3>(24);
            var n = new List<Vector3>(24);
            var uv = new List<Vector2>(24);
            var tri = new List<int>(36);
            Vector3 h = size * 0.5f;
            Face(v, n, uv, tri, h, Vector3.right, Vector3.up, tile, uvOffset);
            Face(v, n, uv, tri, h, Vector3.left, Vector3.up, tile, uvOffset);
            Face(v, n, uv, tri, h, Vector3.forward, Vector3.up, tile, uvOffset);
            Face(v, n, uv, tri, h, Vector3.back, Vector3.up, tile, uvOffset);
            Face(v, n, uv, tri, h, Vector3.up, Vector3.forward, tile, uvOffset);
            Face(v, n, uv, tri, h, Vector3.down, Vector3.forward, tile, uvOffset);
            var mesh = new Mesh { name = "SO_Box" };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Face(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tri, Vector3 h,
                         Vector3 normal, Vector3 up, float tile, Vector3 off)
        {
            Vector3 r = Vector3.Cross(normal, up);
            float hn = Mathf.Abs(Vector3.Dot(h, normal));
            float hu = Mathf.Abs(Vector3.Dot(h, r));
            float hv = Mathf.Abs(Vector3.Dot(h, up));
            Vector3 c = normal * hn;
            int b = v.Count;
            Vector3[] corners = { c - r * hu - up * hv, c - r * hu + up * hv, c + r * hu + up * hv, c + r * hu - up * hv };
            Vector2[] fit = { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            for (int i = 0; i < 4; i++)
            {
                v.Add(corners[i]);
                n.Add(normal);
                if (tile > 0f)
                {
                    Vector3 w = corners[i] + off;
                    uv.Add(new Vector2(Vector3.Dot(w, r) / tile, Vector3.Dot(w, up) / tile));
                }
                else uv.Add(fit[i]);
            }
            tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
            tri.Add(b); tri.Add(b + 2); tri.Add(b + 3);
        }

        /// <summary>Cylinder along local Y (real radius and height, so no scaling is needed).</summary>
        static Mesh CylMesh(float r, float height, int seg)
        {
            string key = "cyl" + r.ToString("0.###") + "_" + height.ToString("0.###") + "_" + seg;
            Mesh cached;
            if (meshCache.TryGetValue(key, out cached)) return cached;

            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            float hy = height * 0.5f;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(dir * r + Vector3.down * hy); n.Add(dir); uv.Add(new Vector2(i / (float)seg, 0f));
                v.Add(dir * r + Vector3.up * hy); n.Add(dir); uv.Add(new Vector2(i / (float)seg, 1f));
            }
            for (int i = 0; i < seg; i++)
            {
                int b = i * 2;
                tri.Add(b); tri.Add(b + 1); tri.Add(b + 3);
                tri.Add(b); tri.Add(b + 3); tri.Add(b + 2);
            }
            // caps
            for (int cap = 0; cap < 2; cap++)
            {
                float y = cap == 0 ? hy : -hy;
                var nn = cap == 0 ? Vector3.up : Vector3.down;
                int c = v.Count;
                v.Add(new Vector3(0f, y, 0f)); n.Add(nn); uv.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); n.Add(nn);
                    uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
                }
                for (int i = 0; i < seg; i++)
                {
                    if (cap == 0) { tri.Add(c); tri.Add(c + 2 + i); tri.Add(c + 1 + i); }
                    else { tri.Add(c); tri.Add(c + 1 + i); tri.Add(c + 2 + i); }
                }
            }
            var mesh = new Mesh { name = "SO_Cylinder" };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            meshCache[key] = mesh;
            return mesh;
        }

        // ================================================================ object helpers

        static GameObject Node(string name, Transform parent, Vector3 localPos, Quaternion localRot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            return go;
        }

        /// <summary>A box (real size, no scaling). Position is local to parent.</summary>
        static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat,
                              bool collider = true, float tile = 1f)
        {
            var go = Node(name, parent, localPos, Quaternion.identity);
            Vector3 off = parent.rotation == Quaternion.identity ? parent.TransformPoint(localPos) : localPos;
            go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size, tile, off);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        static GameObject RotBox(string name, Transform parent, Vector3 localPos, Vector3 localEuler, Vector3 size, Material mat, bool collider = false)
        {
            var go = Box(name, parent, localPos, size, mat, collider, 0f);
            go.transform.localRotation = Quaternion.Euler(localEuler);
            return go;
        }

        /// <summary>Cylinder along local Y after rotation localEuler.</summary>
        static GameObject Cyl(string name, Transform parent, Vector3 localPos, Vector3 localEuler, float radius, float height,
                              Material mat, bool collider = false, int seg = 24)
        {
            var go = Node(name, parent, localPos, Quaternion.Euler(localEuler));
            go.AddComponent<MeshFilter>().sharedMesh = CylMesh(radius, height, seg);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider)
            {
                var cc = go.AddComponent<CapsuleCollider>();
                cc.radius = radius;
                cc.height = Mathf.Max(height, radius * 2f);
                cc.direction = 1;
            }
            return go;
        }

        /// <summary>Pipe between two local points.</summary>
        static GameObject Pipe(string name, Transform parent, Vector3 a, Vector3 b, float radius, Material mat)
        {
            Vector3 d = b - a;
            var go = Cyl(name, parent, (a + b) * 0.5f, Vector3.zero, radius, d.magnitude, mat, false, 14);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            return go;
        }

        static GameObject Sphere(string name, Transform parent, Vector3 localPos, float radius, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * radius * 2f;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Glow AddGlow(GameObject go, Color c, float intensity = 2f, bool on = false, bool pulse = false)
        {
            var g = go.AddComponent<Glow>();
            g.color = c;
            g.intensity = intensity;
            g.startOn = on;
            g.pulse = pulse;
            return g;
        }

        static Light PointLight(string name, Transform parent, Vector3 pos, Color c, float intensity, float range, bool shadows = false)
        {
            var go = Node(name, parent, pos, Quaternion.identity);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        static TextMeshPro Text(string name, Transform parent, Vector3 pos, Quaternion rot, Vector2 size, string text,
                                float maxSize, Color color, bool billboard = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
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

        /// <summary>Floating label that always turns to the player.</summary>
        static TextMeshPro Label(Transform parent, string text, Vector3 pos, float width, Color color, float maxSize = 0.8f)
        {
            int lines = text.Split('\n').Length;
            return Text("Label_" + text.Split('\n')[0].Replace(" ", ""), parent, pos, Quaternion.identity,
                        new Vector2(width, 0.12f * lines + 0.04f), text, maxSize, color, true);
        }

        /// <summary>Wall-mounted screen: frame + dark glass + text. Returns the text.</summary>
        static TextMeshPro WallDisplay(string name, Transform parent, Vector3 center, Vector3 inward, Vector2 size, float maxFont, string text = "")
        {
            Quaternion rot = Quaternion.LookRotation(-inward, Vector3.up);
            var root = Node(name, parent, center, rot).transform;
            // local: +z goes INTO the wall, -z towards the viewer
            Box("Frame", root, new Vector3(0f, 0f, -0.02f), new Vector3(size.x + 0.12f, size.y + 0.12f, 0.04f), M.trim, false, 0f);
            Box("Glass", root, new Vector3(0f, 0f, -0.045f), new Vector3(size.x, size.y, 0.012f), M.screenDark, false, 0f);
            Box("StatusStrip", root, new Vector3(0f, -size.y * 0.5f - 0.035f, -0.045f), new Vector3(size.x * 0.4f, 0.012f, 0.01f), M.stripCyan, false, 0f);
            var t = Text(name + "_Text", root, new Vector3(0f, 0f, -0.056f), Quaternion.identity, size - new Vector2(0.08f, 0.06f), text, maxFont, new Color(0.9f, 0.96f, 1f));
            return t;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        static Vector3 Abs(Vector3 v) { return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z)); }

        // ================================================================ interaction helpers

        static XRGrabInteractable MakeGrabbable(GameObject go, string layer, float mass)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var g = go.AddComponent<XRGrabInteractable>();
            g.interactionLayers = InteractionLayerMask.GetMask("Default", layer);
            if (InteractionLayerMask.GetMask(layer) == 0)
                log.AppendLine("WARNING: interaction layer '" + layer + "' is missing in Project Settings > XR Interaction Toolkit > Interaction Layers.");
            return g;
        }

        static XRSocketInteractor MakeSocket(string name, Transform parent, Vector3 pos, Quaternion rot, string layer, float radius = 0.15f)
        {
            var go = Node(name, parent, pos, rot);
            var sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = radius;
            var s = go.AddComponent<XRSocketInteractor>();
            s.interactionLayers = InteractionLayerMask.GetMask(layer);
            s.showInteractableHoverMeshes = true;
            return s;
        }

        static SocketTask MakeTask(XRSocketInteractor socket, string label, Color c, Renderer pad, XRGrabInteractable item)
        {
            var t = socket.gameObject.AddComponent<SocketTask>();
            t.displayName = label;
            t.color = c;
            t.pad = pad;
            t.expectedItem = item;
            return t;
        }

        static Behaviour MakeTeleportFloor(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = Node(name, parent, center, Quaternion.identity);
            go.AddComponent<BoxCollider>().size = size;
            var area = go.AddComponent<TeleportationArea>();
            area.interactionLayers = InteractionLayerMask.GetMask("Default", "Teleport");
            return area;
        }

        // ================================================================ utilities

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void AddSceneToBuild(string path)
        {
            var list = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            list.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            log.AppendLine("Scene added as the first scene in the build list.");
        }

        static GameObject FindRigPrefab()
        {
            string p = AssetDatabase.GUIDToAssetPath("f6336ac4ac8b4d34bc5072418cdc62a0");
            var prefab = string.IsNullOrEmpty(p) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (prefab != null && prefab.name.Contains("XR Origin")) return prefab;
            string best = null;
            foreach (var guid in AssetDatabase.FindAssets("XR Origin t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("XR Origin (XR Rig).prefab")) continue;
                if (best == null || path.Contains("Starter Assets")) best = path;
            }
            return best != null ? AssetDatabase.LoadAssetAtPath<GameObject>(best) : null;
        }
    }
}
