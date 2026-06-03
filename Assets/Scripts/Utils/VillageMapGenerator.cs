#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class VillageMapGenerator : EditorWindow
{
    private float terrainWidth  = 400f;
    private float terrainLength = 300f;
    private float terrainHeight = 600f;
    private int   heightmapRes  = 513;

    [MenuItem("Tools/Terrain/Generate Village Map")]
    static void ShowWindow() => GetWindow<VillageMapGenerator>("Village Map Generator");

    void OnGUI()
    {
        GUILayout.Label("Village Map Generator", EditorStyles.boldLabel);
        terrainWidth  = EditorGUILayout.FloatField("Width",  terrainWidth);
        terrainLength = EditorGUILayout.FloatField("Length", terrainLength);
        terrainHeight = EditorGUILayout.FloatField("Height", terrainHeight);
        GUILayout.Space(10);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("GENERATE", GUILayout.Height(35))) Generate();
        GUI.backgroundColor = Color.white;
    }

    void Generate()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/GeneratedMap"))
            AssetDatabase.CreateFolder("Assets/Scenes", "GeneratedMap");

        TerrainData td = new TerrainData();
        td.heightmapResolution = heightmapRes;
        td.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
        td.SetDetailResolution(504, 24);

        string path = "Assets/Scenes/GeneratedMap/VillageTerrainData.asset";
        if (AssetDatabase.LoadAssetAtPath<TerrainData>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(td, path);

        GameObject tGO = Terrain.CreateTerrainGameObject(td);
        tGO.name = "VillageTerrain";
        tGO.transform.position = Vector3.zero;

        EditorUtility.DisplayProgressBar("Generating", "Heightmap...", 0.15f);
        BuildHeightmap(td);
        EditorUtility.DisplayProgressBar("Generating", "Pool...",      0.30f);
        CarvePool(td);
        EditorUtility.DisplayProgressBar("Generating", "River...",     0.45f);
        CarveRiver(td);
        EditorUtility.DisplayProgressBar("Generating", "Buildings...", 0.65f);
        PlaceBuildings(td);
        EditorUtility.DisplayProgressBar("Generating", "Water...",     0.85f);
        CreateWater(td);
        EditorUtility.ClearProgressBar();

        AssetDatabase.SaveAssets();

        Vector3 lhPos = ToWorld(0.88f, 0.08f, td);
        Debug.Log($"[Village] Done! Posisi Mercusuar → {lhPos} (letakkan prefab di sini)");
    }

    // ============================================================
    // fBm – smooth organic hills
    // ============================================================
    float fBm(float x, float y, int octaves, float lacunarity = 2.0f, float gain = 0.5f)
    {
        float val = 0f, amp = 0.5f, freq = 1f;
        for (int o = 0; o < octaves; o++)
        {
            val  += Mathf.PerlinNoise(x * freq, y * freq) * amp;
            freq *= lacunarity;
            amp  *= gain;
        }
        return val;
    }

    // ============================================================
    // HEIGHTMAP
    //
    // Semua nilai height dalam normalized (0–1), dikali terrainHeight:
    //   Base village   : 0.055  → ~33m
    //   Bukit hutan    : +0.012 max → ~40m total (gentle hills)
    //   Waterfall cliff: +0.022 max → ~46m (terlihat tapi tidak menjulang)
    //   Pantai         : 0.018  → ~11m (gentle slope ke laut)
    //   Ocean floor    : 0.003  → ~2m
    //
    // Layout:
    //   Utara     : Waterfall cliff + pool
    //   Barat-laut: Hutan berbukit organik
    //   Tengah    : Village flat
    //   Barat     : Sungai dari pool ke laut
    //   Selatan   : Pantai landai (walkable)
    //   Tenggara  : Laut + pulau mercusuar
    //   Timur     : Kebun (sedikit elevated)
    // ============================================================
    void BuildHeightmap(TerrainData td)
    {
        int res = td.heightmapResolution;
        float[,] h = new float[res, res];

        float ox = 13.7f, oy = 9.3f; // Perlin seed offset

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float nx = (float)x / (res - 1); // 0=west, 1=east
            float ny = (float)y / (res - 1); // 0=south, 1=north

            float val = 0.055f; // base village height

            // ── HUTAN BARAT-LAUT (gentle rolling hills) ────────
            if (nx < 0.40f && ny > 0.58f)
            {
                float fx = Mathf.Clamp01((0.40f - nx) / 0.32f);
                float fy = Mathf.Clamp01((ny - 0.58f) / 0.38f);
                float mask = fx * fy;
                mask = mask * mask * (3f - 2f * mask); // smoothstep

                // fBm untuk bukit organik — tidak terlalu tinggi
                float bumps = fBm(nx * 4.5f + ox, ny * 4.5f + oy, 5) * 0.012f;
                val += mask * bumps;
            }

            // ── HUTAN TIMUR (gentle elevation) ─────────────────
            if (nx > 0.78f && ny > 0.32f)
            {
                float ex = Mathf.Clamp01((nx - 0.78f) / 0.22f);
                float ey = Mathf.Clamp01((ny - 0.32f) / 0.55f);
                float bumps = fBm(nx * 5f + ox + 5f, ny * 5f + oy + 5f, 4) * 0.010f;
                val += ex * ey * bumps;
            }

            // ── WATERFALL CLIFF (utara, moderate height) ────────
            // Hanya naik gentle ke utara, bukan cliff vertikal
            if (ny > 0.75f)
            {
                float t = Mathf.Clamp01((ny - 0.75f) / 0.25f);
                t = t * t * (3f - 2f * t); // smoothstep — tidak linear
                float cliffNoise = fBm(nx * 3f + ox, ny * 2f + oy, 4) * 0.006f;
                val += t * (0.018f + cliffNoise); // max +0.018 → ~11m di atas village
            }

            // Waterfall peak: tonjolan kecil tepat di sumber air
            float dWF = Dist(nx, ny, 0.52f, 0.96f);
            if (dWF < 0.08f)
            {
                float wfT = 1f - (dWF / 0.08f);
                wfT = wfT * wfT;
                val += wfT * 0.022f; // ~13m di atas base → total ~46m, moderate
            }

            // ── VILLAGE CENTER (benar-benar flat) ───────────────
            float dVC = Dist(nx, ny, 0.48f, 0.54f);
            if (dVC < 0.24f)
            {
                float flat = Mathf.Clamp01(1f - dVC / 0.24f);
                flat = flat * flat * (3f - 2f * flat);
                val = Mathf.Lerp(val, 0.055f, flat * 0.90f);
            }

            // ── PANTAI SELATAN (walkable gentle slope) ──────────
            // Pantai landai, tidak menjorok jurang
            if (ny < 0.30f)
            {
                // Gentle S-curve dari village ke pantai
                float t = Mathf.Clamp01(ny / 0.30f);
                t = t * t * (3f - 2f * t);
                float sandNoise = fBm(nx * 9f + ox + 20f, ny * 9f + oy + 20f, 3) * 0.002f;
                // Pantai level sedikit di bawah village, tapi tidak jauh
                float beachHeight = 0.040f + sandNoise; // ~24m (village ~33m, jadi turun ~9m saja)
                val = Mathf.Lerp(beachHeight, val, t);
            }

            // Sand flat strip (area pasir pantai yang lebar dan datar)
            if (ny > 0.05f && ny < 0.18f && nx > 0.10f && nx < 0.75f)
            {
                float t = Mathf.Clamp01(1f - Mathf.Abs(ny - 0.12f) / 0.07f) * 0.6f;
                val = Mathf.Lerp(val, 0.035f, t); // pasir datar ~21m
            }

            // ── OCEAN (hanya sangat dekat edge bawah) ────────────
            if (ny < 0.05f)
            {
                float t = Mathf.Clamp01(1f - ny / 0.05f);
                val = Mathf.Lerp(val, 0.025f, t); // ocean level ~15m, tidak terlalu dalam
            }

            // Ocean tenggara (lebih kecil, tidak terlalu menjorok)
            if (nx > 0.72f && ny < 0.12f)
            {
                float oX = Mathf.Clamp01((nx - 0.72f) / 0.28f);
                float oY = Mathf.Clamp01(1f - ny / 0.12f);
                float oT = oX * oY * 0.5f;
                val = Mathf.Lerp(val, 0.028f, oT);
            }

            // ── MERCUSUAR: tidak dibuat di terrain, pakai object saja ──

            // ── KEBUN TIMUR (sedikit elevated) ──────────────────
            if (nx > 0.73f && ny > 0.30f && ny < 0.70f)
            {
                float kx = Mathf.Clamp01((nx - 0.73f) / 0.20f);
                float ky = Mathf.Clamp01(1f - Mathf.Abs(ny - 0.50f) / 0.20f);
                float kBumps = fBm(nx * 6f + ox, ny * 6f + oy, 3) * 0.008f;
                val += kx * ky * (0.005f + kBumps);
            }

            // ── MICRO NOISE (detail seluruh terrain) ────────────
            float micro = fBm(nx * 14f + ox + 3f, ny * 14f + oy + 7f, 4) * 0.004f;
            // Mask noise: kurangi di village center dan pantai
            float noiseMask = 1f;
            if (dVC < 0.24f) noiseMask *= Mathf.Clamp01(dVC / 0.20f);
            if (ny < 0.28f)  noiseMask *= Mathf.Clamp01(ny / 0.14f);
            val += micro * noiseMask;

            h[y, x] = Mathf.Clamp01(val);
        }

        td.SetHeights(0, 0, h);
    }

    // ============================================================
    // POOL – cekungan di bawah waterfall (mangkuk kecil)
    // Posisi tepat di bawah waterfall peak (~0.52, 0.88)
    // ============================================================
    void CarvePool(TerrainData td)
    {
        int res = td.heightmapResolution;
        float[,] h = td.GetHeights(0, 0, res, res);

        float poolNX = 0.52f, poolNY = 0.88f;
        float poolRadius = 0.055f; // radius normalized
        float poolDepth  = 0.040f; // lebih dalam dari river → air terkumpul

        int cx  = Mathf.RoundToInt(poolNX * (res - 1));
        int cy  = Mathf.RoundToInt(poolNY * (res - 1));
        int rad = Mathf.RoundToInt(poolRadius * res);

        for (int dy = -rad; dy <= rad; dy++)
        for (int dx = -rad; dx <= rad; dx++)
        {
            int px = cx + dx, py = cy + dy;
            if (px < 0 || px >= res || py < 0 || py >= res) continue;

            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > rad) continue;

            // Bowl shape: tengah lebih dalam, pinggir gradual
            float t = d / rad;
            float bowl = 1f - t * t; // parabola
            h[py, px] = Mathf.Max(0.004f, h[py, px] - poolDepth * bowl);
        }

        td.SetHeights(0, 0, h);
    }

    // ============================================================
    // RIVER – dari pool (0.52, 0.88) ke barat lalu muara natural ke laut
    // Dermaga sudah dipindah ke pantai, muara sungai = natural/rocky
    // ============================================================
    void CarveRiver(TerrainData td)
    {
        int res = td.heightmapResolution;
        float[,] h = td.GetHeights(0, 0, res, res);

        // Titik awal dari tepi pool, bukan dari waterfall peak
        Vector2[] pts =
        {
            new Vector2(0.50f, 0.86f), // keluar dari pool
            new Vector2(0.44f, 0.80f), // belok ke barat
            new Vector2(0.34f, 0.74f),
            new Vector2(0.22f, 0.66f),
            new Vector2(0.14f, 0.56f),
            new Vector2(0.09f, 0.44f),
            new Vector2(0.07f, 0.32f),
            new Vector2(0.09f, 0.22f), // mendekati pantai
            new Vector2(0.13f, 0.13f), // muara natural (tidak ada dermaga)
            new Vector2(0.18f, 0.06f), // masuk ke laut
        };

        List<Vector2> smooth = CatmullRom(pts, 40);

        float rW    = 14f;
        float pW    = (rW / Mathf.Max(terrainWidth, terrainLength)) * res;
        float depth = 0.028f;

        for (int si = 0; si < smooth.Count; si++)
        {
            var pt = smooth[si];
            // Sungai sedikit melebar mendekati laut
            float widthMul = 1f + Mathf.Clamp01((float)si / smooth.Count) * 0.6f;

            int cx  = Mathf.RoundToInt(pt.x * (res - 1));
            int cy  = Mathf.RoundToInt(pt.y * (res - 1));
            int rad = Mathf.RoundToInt(pW * 0.5f * widthMul);

            for (int dy = -rad; dy <= rad; dy++)
            for (int dx = -rad; dx <= rad; dx++)
            {
                int px = cx + dx, py = cy + dy;
                if (px < 0 || px >= res || py < 0 || py >= res) continue;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > rad) continue;
                float f = 1f - Mathf.Pow(d / rad, 2f);
                h[py, px] = Mathf.Max(0.003f, h[py, px] - depth * f);
            }
        }

        td.SetHeights(0, 0, h);
    }

    // ============================================================
    // BUILDINGS
    // Dermaga → pantai selatan tengah (langsung ke laut)
    // Mercusuar → TIDAK di sini, letakkan prefab manual
    // ============================================================
    void PlaceBuildings(TerrainData td)
    {
        GameObject parent = new GameObject("Buildings");

        var buildings = new (string name, float x, float z, Vector3 size, Color col)[]
        {
            ("Rumah_Utama",        0.48f, 0.76f, new Vector3(10, 8,10), new Color(0.65f,0.30f,0.18f)),
            ("Toko_Umum",          0.55f, 0.68f, new Vector3( 9, 6, 8), new Color(0.70f,0.45f,0.25f)),
            ("Rumah_Petani",       0.32f, 0.70f, new Vector3( 8, 6, 8), new Color(0.55f,0.35f,0.20f)),
            ("Kincir_Angin",       0.72f, 0.70f, new Vector3( 5,15, 5), new Color(0.80f,0.70f,0.50f)),
            ("Penginapan",         0.28f, 0.50f, new Vector3(10, 7, 9), new Color(0.60f,0.28f,0.18f)),
            ("Bengkel_Tukang",     0.58f, 0.52f, new Vector3( 9, 6, 8), new Color(0.50f,0.35f,0.25f)),
            ("Rumah_Nelayan",      0.38f, 0.34f, new Vector3( 8, 5, 8), new Color(0.45f,0.45f,0.50f)),
            ("Rumah_Pembuat_Roti", 0.48f, 0.40f, new Vector3( 8, 6, 8), new Color(0.75f,0.50f,0.28f)),
            ("Rumah_Dokter",       0.75f, 0.50f, new Vector3( 8, 6, 9), new Color(0.35f,0.55f,0.35f)),
            ("Gudang",             0.62f, 0.36f, new Vector3(10, 5,12), new Color(0.50f,0.40f,0.30f)),
        };

        foreach (var b in buildings)
        {
            Vector3 pos = ToWorld(b.x, b.z, td);
            pos.y += b.size.y * 0.5f;
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = b.name;
            cube.transform.parent = parent.transform;
            cube.transform.position = pos;
            cube.transform.localScale = b.size;
            cube.transform.rotation = Quaternion.Euler(0, Random.Range(-15f, 15f), 0);
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = b.col };
            cube.GetComponent<Renderer>().sharedMaterial = m;
        }

        // Alun-alun
        Vector3 fountainPos = ToWorld(0.45f, 0.52f, td);
        fountainPos.y += 1f;
        var fountain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fountain.name = "Fountain_AlunAlun";
        fountain.transform.parent = parent.transform;
        fountain.transform.position = fountainPos;
        fountain.transform.localScale = new Vector3(5, 1.5f, 5);
        var fMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            { color = new Color(0.5f, 0.7f, 0.85f) };
        fountain.GetComponent<Renderer>().sharedMaterial = fMat;

        // ── DERMAGA (pantai selatan, langsung ke laut) ──────────
        // Posisi tengah pantai, menghadap laut selatan
        Vector3 dockPos = ToWorld(0.38f, 0.10f, td);
        dockPos.y += 0.5f;
        var dock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dock.name = "Dermaga";
        dock.transform.parent = parent.transform;
        dock.transform.position = dockPos;
        dock.transform.localScale = new Vector3(6, 1, 22);
        dock.transform.rotation = Quaternion.Euler(0, 0f, 0); // menghadap selatan (laut)
        var dMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            { color = new Color(0.50f, 0.35f, 0.20f) };
        dock.GetComponent<Renderer>().sharedMaterial = dMat;

        // Tiang dermaga kiri & kanan
        for (int i = 0; i < 3; i++)
        {
            foreach (float sx in new float[] { -2.5f, 2.5f })
            {
                var tiang = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tiang.name = $"Dermaga_Tiang_{i}";
                tiang.transform.parent = parent.transform;
                tiang.transform.position = dockPos + new Vector3(sx, -2f, -6f + i * 5f);
                tiang.transform.localScale = new Vector3(0.4f, 3f, 0.4f);
                tiang.GetComponent<Renderer>().sharedMaterial = dMat;
            }
        }

        // Kebun plots
        for (int i = 0; i < 3; i++)
        {
            Vector3 kPos = ToWorld(0.82f, 0.42f + i * 0.06f, td);
            kPos.y += 0.2f;
            var kebun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kebun.name = $"Kebun_{i + 1}";
            kebun.transform.parent = parent.transform;
            kebun.transform.position = kPos;
            kebun.transform.localScale = new Vector3(12, 0.4f, 6);
            var kMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                { color = new Color(0.30f, 0.55f + i * 0.05f, 0.20f) };
            kebun.GetComponent<Renderer>().sharedMaterial = kMat;
        }
    }

    // ============================================================
    // WATER
    // waterY dikalibrasi agar:
    //   - Di atas ocean floor (0.003 × 600 = 1.8m)
    //   - Di bawah pantai (0.015 × 600 = 9m)
    //   - Pool level sedikit lebih tinggi dari river
    // ============================================================
    void CreateWater(TerrainData td)
    {
        GameObject wParent = new GameObject("Water");

        // Ocean level: 0.010 × 600 = 6m
        // Pantai: ~0.015 → 9m → pantai tetap kering ✓
        float oceanY = terrainHeight * 0.010f;

        Material wMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Map/air/water.mat");
        if (wMat == null)
        {
            wMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wMat.color = new Color(0.10f, 0.32f, 0.58f, 0.85f);
        }

        Material poolMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        poolMat.color = new Color(0.20f, 0.50f, 0.72f, 0.90f); // lebih terang (calm water)

        // ── OCEAN SELATAN ──────────────────────────────────────
        var ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ocean.name = "Ocean";
        ocean.transform.parent = wParent.transform;
        ocean.transform.position = new Vector3(terrainWidth * 0.38f, oceanY, -terrainLength * 0.12f);
        ocean.transform.localScale = new Vector3(terrainWidth * 0.10f, 1, terrainLength * 0.07f);
        ocean.GetComponent<Renderer>().sharedMaterial = wMat;

        // Ocean tenggara (wrap ke pulau mercusuar)
        var oceanSE = GameObject.CreatePrimitive(PrimitiveType.Plane);
        oceanSE.name = "Ocean_SE";
        oceanSE.transform.parent = wParent.transform;
        oceanSE.transform.position = new Vector3(terrainWidth * 0.83f, oceanY, terrainLength * 0.09f);
        oceanSE.transform.localScale = new Vector3(terrainWidth * 0.04f, 1, terrainLength * 0.05f);
        oceanSE.GetComponent<Renderer>().sharedMaterial = wMat;

        // ── POOL (di bawah waterfall) ──────────────────────────
        // Pool level sedikit lebih tinggi dari river tapi lebih rendah dari terrain sekitar
        float poolY = terrainHeight * 0.058f; // ~35m, cukup di dalam cekungan
        var pool = GameObject.CreatePrimitive(PrimitiveType.Plane);
        pool.name = "Pool_Waterfall";
        pool.transform.parent = wParent.transform;
        pool.transform.position = new Vector3(terrainWidth * 0.52f, poolY, terrainLength * 0.88f);
        pool.transform.localScale = new Vector3(2.2f, 1, 2.2f);
        pool.GetComponent<Renderer>().sharedMaterial = poolMat;

        // ── SUNGAI ─────────────────────────────────────────────
        Vector2[] rPts =
        {
            new Vector2(0.50f, 0.86f),
            new Vector2(0.44f, 0.80f),
            new Vector2(0.34f, 0.74f),
            new Vector2(0.22f, 0.66f),
            new Vector2(0.14f, 0.56f),
            new Vector2(0.09f, 0.44f),
            new Vector2(0.07f, 0.32f),
            new Vector2(0.09f, 0.22f),
            new Vector2(0.13f, 0.13f),
        };

        // River Y sedikit di bawah pool, turun gradual ke ocean
        for (int i = 0; i < rPts.Length - 1; i++)
        {
            Vector2 a   = rPts[i], b = rPts[i + 1];
            Vector2 mid = (a + b) * 0.5f;
            Vector2 dir = new Vector2((b.x - a.x) * terrainWidth, (b.y - a.y) * terrainLength);
            float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            float len   = dir.magnitude;

            // Height turun dari pool ke ocean level
            float t = (float)i / (rPts.Length - 1);
            float segY = Mathf.Lerp(poolY - terrainHeight * 0.003f, oceanY + 1f, t);

            // Lebar bertambah mendekati muara
            float widthScale = 1.4f + t * 0.8f;

            var seg = GameObject.CreatePrimitive(PrimitiveType.Plane);
            seg.name = $"River_{i}";
            seg.transform.parent = wParent.transform;
            seg.transform.position = new Vector3(mid.x * terrainWidth, segY, mid.y * terrainLength);
            seg.transform.localScale = new Vector3(widthScale, 1, len / 10f);
            seg.transform.rotation = Quaternion.Euler(0, angle, 0);
            seg.GetComponent<Renderer>().sharedMaterial = wMat;
        }

        // ── WATERFALL (visual cube) ────────────────────────────
        var wf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wf.name = "Waterfall";
        wf.transform.parent = wParent.transform;
        float wfY = terrainHeight * 0.072f;
        wf.transform.position = new Vector3(terrainWidth * 0.52f, wfY, terrainLength * 0.93f);
        wf.transform.localScale = new Vector3(8, terrainHeight * 0.022f, 2);
        var wfMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            { color = new Color(0.55f, 0.82f, 0.96f) };
        wf.GetComponent<Renderer>().sharedMaterial = wfMat;
    }

    // ============================================================
    // HELPERS
    // ============================================================
    Vector3 ToWorld(float nx, float nz, TerrainData td)
    {
        float wx = nx * terrainWidth;
        float wz = nz * terrainLength;
        float wy = td.GetInterpolatedHeight(nx, nz);
        return new Vector3(wx, wy, wz);
    }

    float Dist(float x1, float y1, float x2, float y2)
    {
        float dx = x1 - x2, dy = y1 - y2;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    List<Vector2> CatmullRom(Vector2[] pts, int seg)
    {
        var r = new List<Vector2>();
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector2 p0 = pts[Mathf.Max(0, i - 1)];
            Vector2 p1 = pts[i];
            Vector2 p2 = pts[Mathf.Min(pts.Length - 1, i + 1)];
            Vector2 p3 = pts[Mathf.Min(pts.Length - 1, i + 2)];
            for (int j = 0; j < seg; j++)
            {
                float t  = j / (float)seg;
                float t2 = t * t, t3 = t2 * t;
                r.Add(0.5f * ((2f * p1) + (-p0 + p2) * t +
                    (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                    (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        r.Add(pts[pts.Length - 1]);
        return r;
    }
}
#endif