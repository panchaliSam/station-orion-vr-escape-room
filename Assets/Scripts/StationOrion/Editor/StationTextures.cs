using System.IO;
using UnityEditor;
using UnityEngine;

namespace StationOrion.EditorTools
{
    /// <summary>
    /// Generates all the station textures in code (no downloads, no licensing issues):
    /// sci-fi wall panels, tread-plate floor, perforated ceiling, hazard stripes, cargo crates,
    /// console screens, solar cells, a procedural Earth and a star-field sky.
    /// Each texture is saved as a PNG under Assets/StationOrion/Generated/Textures and only
    /// generated once (delete the folder to regenerate).
    /// </summary>
    public static class StationTextures
    {
        public static string Folder = "Assets/StationOrion/Generated/Textures";

        // --------------------------------------------------------------- public API

        public class Set
        {
            public Texture2D wallAlbedo, wallNormal, floorAlbedo, floorNormal, ceilAlbedo, ceilNormal;
            public Texture2D hazard, crateAlbedo, crateNormal, screen, solar, earth, stars, vent, ventNormal;
        }

        public static Set EnsureAll()
        {
            Directory.CreateDirectory(Folder);
            var s = new Set();
            const int N = 512;

            s.wallAlbedo = Load("Wall_Albedo");
            s.wallNormal = Load("Wall_Normal");
            if (s.wallAlbedo == null || s.wallNormal == null)
            {
                Progress("Wall panels", 0.1f);
                WallPanel(N, out var a, out var h);
                s.wallAlbedo = Save("Wall_Albedo", a, N, N, false);
                s.wallNormal = Save("Wall_Normal", NormalFromHeight(h, N, N, 6f), N, N, true);
            }

            s.floorAlbedo = Load("Floor_Albedo");
            s.floorNormal = Load("Floor_Normal");
            if (s.floorAlbedo == null || s.floorNormal == null)
            {
                Progress("Floor plates", 0.2f);
                FloorPlate(N, out var a, out var h);
                s.floorAlbedo = Save("Floor_Albedo", a, N, N, false);
                s.floorNormal = Save("Floor_Normal", NormalFromHeight(h, N, N, 5f), N, N, true);
            }

            s.ceilAlbedo = Load("Ceiling_Albedo");
            s.ceilNormal = Load("Ceiling_Normal");
            if (s.ceilAlbedo == null || s.ceilNormal == null)
            {
                Progress("Ceiling panels", 0.3f);
                CeilingPanel(N, out var a, out var h);
                s.ceilAlbedo = Save("Ceiling_Albedo", a, N, N, false);
                s.ceilNormal = Save("Ceiling_Normal", NormalFromHeight(h, N, N, 4f), N, N, true);
            }

            s.vent = Load("Vent_Albedo");
            s.ventNormal = Load("Vent_Normal");
            if (s.vent == null || s.ventNormal == null)
            {
                Progress("Vents", 0.35f);
                Vent(256, out var a, out var h);
                s.vent = Save("Vent_Albedo", a, 256, 256, false);
                s.ventNormal = Save("Vent_Normal", NormalFromHeight(h, 256, 256, 5f), 256, 256, true);
            }

            s.hazard = Load("Hazard_Albedo") ?? Save("Hazard_Albedo", Hazard(256), 256, 256, false);

            s.crateAlbedo = Load("Crate_Albedo");
            s.crateNormal = Load("Crate_Normal");
            if (s.crateAlbedo == null || s.crateNormal == null)
            {
                Progress("Cargo crates", 0.45f);
                Crate(N, out var a, out var h);
                s.crateAlbedo = Save("Crate_Albedo", a, N, N, false);
                s.crateNormal = Save("Crate_Normal", NormalFromHeight(h, N, N, 5f), N, N, true);
            }

            s.screen = Load("Screen_Emission") ?? Save("Screen_Emission", ScreenUI(512, 256), 512, 256, false);
            s.solar = Load("Solar_Albedo") ?? Save("Solar_Albedo", Solar(256), 256, 256, false);

            if ((s.earth = Load("Earth_Albedo")) == null)
            {
                Progress("Planet Earth (takes a few seconds)", 0.6f);
                s.earth = Save("Earth_Albedo", Earth(2048, 1024), 2048, 1024, false, TextureWrapMode.Clamp, true, TextureWrapMode.Repeat);
            }
            if ((s.stars = Load("Stars_Sky")) == null)
            {
                Progress("Star field (takes a few seconds)", 0.8f);
                s.stars = Save("Stars_Sky", Stars(4096, 2048), 4096, 2048, false, TextureWrapMode.Clamp, false, TextureWrapMode.Repeat);
            }
            return s;
        }

        static void Progress(string what, float p)
        {
            EditorUtility.DisplayProgressBar("Station Orion", "Generating textures: " + what + "...", p);
        }

        static Texture2D Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/" + name + ".png");
        }

        static Texture2D Save(string name, Color[] px, int w, int h, bool normalMap,
                              TextureWrapMode wrapV = TextureWrapMode.Repeat, bool mips = true,
                              TextureWrapMode wrapU = TextureWrapMode.Repeat)
        {
            string path = Folder + "/" + name + ".png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, normalMap);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = !normalMap;
            imp.wrapModeU = wrapU;
            imp.wrapModeV = wrapV;
            imp.mipmapEnabled = mips;
            imp.anisoLevel = 8;
            imp.filterMode = FilterMode.Trilinear;
            imp.maxTextureSize = Mathf.Max(w, h);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // --------------------------------------------------------------- noise helpers

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        static float Hash3(int x, int y, int z, int seed)
        {
            unchecked { return Hash(x + z * 1619, y - z * 31337, seed + z * 6971); }
        }

        static float Smooth(float t) { return t * t * (3f - 2f * t); }

        static int Wrap(int v, int p) { int r = v % p; return r < 0 ? r + p : r; }

        /// <summary>Tileable value noise (period in lattice cells).</summary>
        public static float Noise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = Smooth(x - x0), fy = Smooth(y - y0);
            int xa = Wrap(x0, period), xb = Wrap(x0 + 1, period), ya = Wrap(y0, period), yb = Wrap(y0 + 1, period);
            float a = Mathf.Lerp(Hash(xa, ya, seed), Hash(xb, ya, seed), fx);
            float b = Mathf.Lerp(Hash(xa, yb, seed), Hash(xb, yb, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        /// <summary>Tileable fractal noise in 0..1 for u,v in 0..1.</summary>
        public static float Fbm(float u, float v, int basePeriod, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            int p = basePeriod;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise(u * p, v * p, p, seed + i * 17) * amp;
                norm += amp;
                amp *= 0.5f;
                p *= 2;
            }
            return sum / norm;
        }

        static float Noise3(float x, float y, float z, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y), z0 = Mathf.FloorToInt(z);
            float fx = Smooth(x - x0), fy = Smooth(y - y0), fz = Smooth(z - z0);
            float c000 = Hash3(x0, y0, z0, seed), c100 = Hash3(x0 + 1, y0, z0, seed);
            float c010 = Hash3(x0, y0 + 1, z0, seed), c110 = Hash3(x0 + 1, y0 + 1, z0, seed);
            float c001 = Hash3(x0, y0, z0 + 1, seed), c101 = Hash3(x0 + 1, y0, z0 + 1, seed);
            float c011 = Hash3(x0, y0 + 1, z0 + 1, seed), c111 = Hash3(x0 + 1, y0 + 1, z0 + 1, seed);
            float a = Mathf.Lerp(Mathf.Lerp(c000, c100, fx), Mathf.Lerp(c010, c110, fx), fy);
            float b = Mathf.Lerp(Mathf.Lerp(c001, c101, fx), Mathf.Lerp(c011, c111, fx), fy);
            return Mathf.Lerp(a, b, fz);
        }

        static float Fbm3(Vector3 p, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f, f = 1f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise3(p.x * f, p.y * f, p.z * f, seed + i * 31) * amp;
                norm += amp;
                amp *= 0.5f;
                f *= 2.03f;
            }
            return sum / norm;
        }

        static float SmoothStep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        public static Color[] NormalFromHeight(float[] h, int w, int hh, float strength)
        {
            var px = new Color[w * hh];
            for (int y = 0; y < hh; y++)
                for (int x = 0; x < w; x++)
                {
                    float l = h[y * w + Wrap(x - 1, w)], r = h[y * w + Wrap(x + 1, w)];
                    float d = h[Wrap(y - 1, hh) * w + x], u = h[Wrap(y + 1, hh) * w + x];
                    var n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                    px[y * w + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            return px;
        }

        // distance (in px) to the nearest edge of a w x h rectangle starting at (x0,y0)
        static float EdgeDist(int x, int y, int x0, int y0, int w, int h)
        {
            return Mathf.Min(Mathf.Min(x - x0, x0 + w - 1 - x), Mathf.Min(y - y0, y0 + h - 1 - y));
        }

        // --------------------------------------------------------------- textures

        /// <summary>1 m x 1 m of wall: two horizontal panels with grooves, bevels, screws, a vent slot and wear.</summary>
        public static void WallPanel(int n, out Color[] albedo, out float[] height)
        {
            albedo = new Color[n * n];
            height = new float[n * n];
            Color baseCol = new Color(0.80f, 0.81f, 0.83f);
            Color accent = new Color(0.62f, 0.66f, 0.70f);
            int half = n / 2;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    float u = x / (float)n, v = y / (float)n;
                    int py = y < half ? 0 : half;
                    float e = EdgeDist(x, y, 0, py, n, half);         // distance to panel edge
                    float hgt, shade;
                    if (e < 3f) { hgt = 0f; shade = 0.28f; }                  // groove
                    else if (e < 9f) { hgt = (e - 3f) / 6f; shade = 0.9f; }    // bevel
                    else { hgt = 1f; shade = 1f; }

                    Color c = baseCol;
                    // upper panel: darker accent band
                    if (py == half && y > py + half * 0.62f && y < py + half * 0.8f && e >= 9f) { c = accent; hgt = 0.92f; }
                    // lower panel: inset service hatch with vent slots
                    if (py == 0)
                    {
                        float ix = x - n * 0.62f, iy = y - half * 0.22f;
                        if (ix > 0 && ix < n * 0.3f && iy > 0 && iy < half * 0.56f)
                        {
                            float ie = Mathf.Min(Mathf.Min(ix, n * 0.3f - ix), Mathf.Min(iy, half * 0.56f - iy));
                            hgt = ie < 2f ? 0.5f : 0.75f;
                            c = new Color(0.72f, 0.74f, 0.77f);
                            // vent slots
                            if (ie > 8f && ((int)iy % 12) < 5) { hgt = 0.35f; c = new Color(0.12f, 0.13f, 0.14f); }
                        }
                    }
                    // screws in the panel corners
                    for (int k = 0; k < 4; k++)
                    {
                        float sx = (k % 2 == 0) ? 16f : n - 17f;
                        float sy = py + ((k < 2) ? 16f : half - 17f);
                        float dd = Mathf.Sqrt((x - sx) * (x - sx) + (y - sy) * (y - sy));
                        if (dd < 5f) { hgt = 1f + (1f - dd / 5f) * 0.6f; c = new Color(0.55f, 0.57f, 0.6f); shade = 1f; }
                    }
                    float grime = Fbm(u, v, 4, 5, 11);
                    float fine = Fbm(u, v, 64, 2, 23);
                    float k2 = shade * (0.9f + 0.12f * grime + 0.04f * fine);
                    albedo[i] = new Color(c.r * k2, c.g * k2, c.b * k2, 1f);
                    height[i] = hgt + (grime - 0.5f) * 0.04f;
                }
        }

        /// <summary>1 m x 1 m dark metal tread (diamond) plate with seams and wear.</summary>
        public static void FloorPlate(int n, out Color[] albedo, out float[] height)
        {
            albedo = new Color[n * n];
            height = new float[n * n];
            int cell = 32;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    float u = x / (float)n, v = y / (float)n;
                    float e = EdgeDist(x, y, 0, 0, n, n);
                    // diamond tread: alternating diagonal bars
                    int cx = x / cell, cy = y / cell;
                    float lx = (x % cell) - cell * 0.5f, ly = (y % cell) - cell * 0.5f;
                    bool flip = ((cx + cy) & 1) == 0;
                    float a = flip ? (lx + ly) : (lx - ly);
                    float b = flip ? (lx - ly) : (lx + ly);
                    float bar = Mathf.Clamp01(1f - (Mathf.Abs(a) / 3.2f)) * Mathf.Clamp01(1f - (Mathf.Abs(b) / 15f));
                    float wear = Fbm(u, v, 4, 5, 77);
                    float hgt = 0.3f + bar * 0.7f;
                    float g = 0.20f + 0.07f * wear;
                    if (bar > 0.2f) g += 0.08f * wear;                 // worn tops are shinier/lighter
                    if (e < 4f) { hgt = 0f; g = 0.07f; }               // seam
                    else if (e < 10f) { g *= 0.9f; }
                    // bolts in corners
                    for (int k = 0; k < 4; k++)
                    {
                        float sx = (k % 2 == 0) ? 18f : n - 19f, sy = (k < 2) ? 18f : n - 19f;
                        float dd = Mathf.Sqrt((x - sx) * (x - sx) + (y - sy) * (y - sy));
                        if (dd < 6f) { hgt = 1f; g = 0.35f; }
                    }
                    albedo[i] = new Color(g, g * 1.02f, g * 1.06f, 1f);
                    height[i] = hgt;
                }
        }

        /// <summary>1 m x 1 m ceiling panel with perforations.</summary>
        public static void CeilingPanel(int n, out Color[] albedo, out float[] height)
        {
            albedo = new Color[n * n];
            height = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    float e = EdgeDist(x, y, 0, 0, n, n);
                    float g = 0.6f, hgt = 1f;
                    if (e < 4f) { g = 0.2f; hgt = 0f; }
                    else if (e < 12f) { hgt = (e - 4f) / 8f; }
                    else if (e > 40f)
                    {
                        float lx = (x % 16) - 8f, ly = (y % 16) - 8f;
                        if (lx * lx + ly * ly < 7f) { g = 0.18f; hgt = 0.6f; }
                    }
                    float grime = Fbm(x / (float)n, y / (float)n, 4, 4, 5);
                    g *= 0.92f + 0.12f * grime;
                    albedo[i] = new Color(g, g, g * 1.03f, 1f);
                    height[i] = hgt;
                }
        }

        /// <summary>Wall vent grille (used on decals and machine fronts).</summary>
        public static void Vent(int n, out Color[] albedo, out float[] height)
        {
            albedo = new Color[n * n];
            height = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    float e = EdgeDist(x, y, 0, 0, n, n);
                    float g = 0.45f, hgt = 1f;
                    if (e < 14f) { g = 0.5f; hgt = 1f; }
                    else
                    {
                        float s = (y % 16) / 16f;          // louvres
                        hgt = 1f - s;
                        g = Mathf.Lerp(0.55f, 0.06f, s);
                    }
                    albedo[i] = new Color(g, g, g, 1f);
                    height[i] = hgt;
                }
        }

        /// <summary>Yellow/black hazard stripes with wear (0.5 m tile).</summary>
        public static Color[] Hazard(int n)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = ((x + y) % (n / 2)) / (float)(n / 2);
                    bool yellow = d < 0.5f;
                    float wear = Fbm(x / (float)n, y / (float)n, 8, 4, 3);
                    Color c = yellow ? new Color(0.95f, 0.72f, 0.08f) : new Color(0.06f, 0.06f, 0.06f);
                    if (wear > 0.68f) c = Color.Lerp(c, new Color(0.35f, 0.35f, 0.36f), (wear - 0.68f) * 3f);
                    px[y * n + x] = c;
                }
            return px;
        }

        /// <summary>Cargo crate face (fits 0..1 per face): frame, cross brace, stencil block.</summary>
        public static void Crate(int n, out Color[] albedo, out float[] height)
        {
            albedo = new Color[n * n];
            height = new float[n * n];
            Color body = new Color(0.26f, 0.30f, 0.27f);
            Color frame = new Color(0.40f, 0.42f, 0.44f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    float e = EdgeDist(x, y, 0, 0, n, n);
                    Color c = body; float hgt = 0.4f;
                    if (e < 42f) { c = frame; hgt = e < 4f ? 0.6f : 1f; }
                    else
                    {
                        // horizontal ribs
                        if ((y % 64) < 6) { hgt = 0.55f; c = body * 0.85f; }
                        // stencil plate
                        if (x > n * 0.3f && x < n * 0.7f && y > n * 0.42f && y < n * 0.58f) { c = new Color(0.85f, 0.62f, 0.12f); hgt = 0.45f; }
                    }
                    float wear = Fbm(x / (float)n, y / (float)n, 4, 5, 41);
                    float k = 0.85f + 0.25f * wear;
                    albedo[i] = new Color(c.r * k, c.g * k, c.b * k, 1f);
                    height[i] = hgt;
                }
        }

        /// <summary>Console screen UI: grid, graph line and bars (used as emission map).</summary>
        public static Color[] ScreenUI(int w, int h)
        {
            var px = new Color[w * h];
            Color bg = new Color(0.01f, 0.04f, 0.08f);
            Color grid = new Color(0.03f, 0.16f, 0.25f);
            Color line = new Color(0.25f, 0.85f, 1f);
            Color amber = new Color(1f, 0.6f, 0.15f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = bg;
                    if (x % 32 == 0 || y % 32 == 0) c = grid;
                    // graph (left 60%)
                    if (x < w * 0.6f)
                    {
                        float gy = h * 0.55f + Mathf.Sin(x * 0.045f) * h * 0.18f + Mathf.Sin(x * 0.13f) * h * 0.05f;
                        if (Mathf.Abs(y - gy) < 1.6f) c = line;
                    }
                    // bars (right side)
                    else if (x > w * 0.66f && x < w * 0.95f)
                    {
                        int bar = (int)((x - w * 0.66f) / 14f);
                        float bh = (0.25f + 0.6f * Hash(bar, 3, 9)) * h * 0.8f;
                        if (((int)(x - w * 0.66f) % 14) < 9 && y > h * 0.1f && y < h * 0.1f + bh) c = bar % 4 == 0 ? amber : line * 0.8f;
                    }
                    // frame
                    if (x < 3 || y < 3 || x > w - 4 || y > h - 4) c = line * 0.6f;
                    px[y * w + x] = c;
                }
            return px;
        }

        /// <summary>Solar panel cells (dark blue with silver grid lines).</summary>
        public static Color[] Solar(int n)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Color c = new Color(0.04f, 0.07f, 0.2f);
                    if (x % 32 < 2 || y % 32 < 2) c = new Color(0.55f, 0.57f, 0.62f);
                    else if (x % 8 == 0) c = new Color(0.08f, 0.12f, 0.28f);
                    px[y * n + x] = c;
                }
            return px;
        }

        /// <summary>Procedural Earth (equirectangular): oceans, continents, deserts, ice caps, clouds.</summary>
        public static Color[] Earth(int w, int h)
        {
            var px = new Color[w * h];
            Color deep = new Color(0.01f, 0.05f, 0.16f), shallow = new Color(0.04f, 0.2f, 0.38f);
            Color forest = new Color(0.10f, 0.22f, 0.07f), plains = new Color(0.30f, 0.34f, 0.15f);
            Color desert = new Color(0.62f, 0.50f, 0.32f), ice = new Color(0.92f, 0.94f, 0.97f);
            for (int y = 0; y < h; y++)
            {
                float lat = ((y + 0.5f) / h - 0.5f) * Mathf.PI;
                float cl = Mathf.Cos(lat), sl = Mathf.Sin(lat);
                for (int x = 0; x < w; x++)
                {
                    float lon = (x + 0.5f) / w * Mathf.PI * 2f;
                    var d = new Vector3(cl * Mathf.Cos(lon), sl, cl * Mathf.Sin(lon));
                    float land = Fbm3(d * 2.2f + new Vector3(10f, 3f, 7f), 6, 101);
                    float absLat = Mathf.Abs(lat) / (Mathf.PI * 0.5f);
                    Color c;
                    if (land < 0.52f)
                    {
                        c = Color.Lerp(deep, shallow, SmoothStep(0.40f, 0.52f, land));
                    }
                    else
                    {
                        float dry = Fbm3(d * 3.1f + new Vector3(-4f, 8f, 1f), 4, 202);
                        float tropic = 1f - Mathf.Abs(absLat - 0.28f) * 3f; // desert belt
                        c = Color.Lerp(forest, plains, SmoothStep(0.45f, 0.6f, dry));
                        c = Color.Lerp(c, desert, Mathf.Clamp01(tropic) * SmoothStep(0.48f, 0.6f, dry));
                        c *= 0.85f + 0.3f * Fbm3(d * 12f, 3, 303);
                    }
                    // ice caps
                    float iceAmt = SmoothStep(0.88f, 0.93f, absLat + (land - 0.5f) * 0.15f);
                    c = Color.Lerp(c, ice, iceAmt);
                    // clouds (swirled bands)
                    var cd = new Vector3(d.x * 4.5f, d.y * 9f, d.z * 4.5f);              // stretched east-west like weather bands
                    float cn = Fbm3(cd + new Vector3(0f, Mathf.Sin(lon * 3f) * 0.6f, 0f), 7, 404);
                    float cloud = SmoothStep(0.55f, 0.72f, cn) * 0.85f;
                    c = Color.Lerp(c, new Color(0.95f, 0.96f, 0.98f), cloud);
                    c.a = 1f;
                    px[y * w + x] = c;
                }
            }
            return px;
        }

        /// <summary>Star-field panorama with a faint Milky Way band and nebula colour.</summary>
        public static Color[] Stars(int w, int h)
        {
            var px = new Color[w * h];
            // coarse nebula / milky way layer, upsampled
            int cw = w / 8, ch = h / 8;
            var neb = new Color[cw * ch];
            var bandN = new Vector3(0.3f, 0.85f, 0.42f).normalized;
            for (int y = 0; y < ch; y++)
            {
                float lat = ((y + 0.5f) / ch - 0.5f) * Mathf.PI;
                for (int x = 0; x < cw; x++)
                {
                    float lon = (x + 0.5f) / cw * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                    float band = Mathf.Exp(-Mathf.Pow(Vector3.Dot(d, bandN) / 0.22f, 2f));
                    float cloud = Fbm3(d * 3f, 5, 7);
                    float dust = Fbm3(d * 7f + Vector3.one * 5f, 4, 8);
                    float glow = band * SmoothStep(0.35f, 0.75f, cloud) * (0.6f + 0.4f * SmoothStep(0.55f, 0.35f, dust));
                    float tint = Fbm3(d * 2f + Vector3.one * 9f, 3, 9);
                    Color col = Color.Lerp(new Color(0.20f, 0.17f, 0.30f), new Color(0.30f, 0.22f, 0.18f), tint);
                    float extra = SmoothStep(0.62f, 0.8f, Fbm3(d * 2.5f + Vector3.one * 3f, 4, 10)) * 0.25f; // nebula patches
                    neb[y * cw + x] = col * (glow * 0.45f) + new Color(0.10f, 0.05f, 0.16f) * extra;
                }
            }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = (x + 0.5f) / 8f - 0.5f, fy = (y + 0.5f) / 8f - 0.5f;
                    int x0 = Mathf.FloorToInt(fx), y0 = Mathf.Clamp(Mathf.FloorToInt(fy), 0, ch - 1);
                    int y1 = Mathf.Min(y0 + 1, ch - 1);
                    float tx = fx - x0, ty = Mathf.Clamp01(fy - y0);
                    int xa = Wrap(x0, cw), xb = Wrap(x0 + 1, cw);
                    Color a = Color.Lerp(neb[y0 * cw + xa], neb[y0 * cw + xb], tx);
                    Color b = Color.Lerp(neb[y1 * cw + xa], neb[y1 * cw + xb], tx);
                    Color c = Color.Lerp(a, b, ty);
                    c.a = 1f;
                    px[y * w + x] = c;
                }
            // stars: uniformly distributed on the sphere, more of them along the band
            var rnd = new System.Random(1234);
            int count = 26000;
            for (int s = 0; s < count; s++)
            {
                float z = (float)rnd.NextDouble() * 2f - 1f;
                float t = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float r = Mathf.Sqrt(1f - z * z);
                var d = new Vector3(r * Mathf.Cos(t), z, r * Mathf.Sin(t));
                float band = Mathf.Exp(-Mathf.Pow(Vector3.Dot(d, bandN) / 0.25f, 2f));
                if (rnd.NextDouble() > 0.35 + 0.65 * band) continue;
                float lat = Mathf.Asin(d.y), lon = Mathf.Atan2(d.z, d.x);
                if (lon < 0f) lon += Mathf.PI * 2f;
                int sx = (int)(lon / (Mathf.PI * 2f) * w), sy = (int)((lat / Mathf.PI + 0.5f) * h);
                float m = (float)rnd.NextDouble();
                float bright = Mathf.Pow(m, 6f) * 1.6f + 0.15f;
                float temp = (float)rnd.NextDouble();
                Color sc = temp < 0.15f ? new Color(1f, 0.75f, 0.55f) : temp < 0.3f ? new Color(0.7f, 0.8f, 1f) : new Color(1f, 0.97f, 0.92f);
                float stretch = 1f / Mathf.Max(0.15f, Mathf.Cos(lat));
                int rad = bright > 0.9f ? 2 : bright > 0.45f ? 1 : 0;
                for (int oy = -rad; oy <= rad; oy++)
                    for (int ox = -Mathf.CeilToInt(rad * stretch); ox <= Mathf.CeilToInt(rad * stretch); ox++)
                    {
                        int px2 = Wrap(sx + ox, w), py2 = Mathf.Clamp(sy + oy, 0, h - 1);
                        float dd = Mathf.Sqrt((ox / stretch) * (ox / stretch) + oy * oy);
                        float k = rad == 0 ? 1f : Mathf.Clamp01(1f - dd / (rad + 0.6f));
                        int idx = py2 * w + px2;
                        Color cur = px[idx];
                        Color add = sc * (bright * k);
                        px[idx] = new Color(Mathf.Min(1f, cur.r + add.r), Mathf.Min(1f, cur.g + add.g), Mathf.Min(1f, cur.b + add.b), 1f);
                    }
            }
            return px;
        }
    }
}
