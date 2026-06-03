#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Auto-paints terrain texture based on height and slope only.
/// NO paths/roads - user will paint those manually.
/// 
/// Layers: 0=Grass, 1=Jalanan, 2=Pasir Pantai, 3=Tanah, 4=Jalan Bukit
/// 
/// Rules:
/// - Grass: flat areas (village, forest floor)
/// - Jalanan: steep cliff faces (waterfall area)
/// - Pasir Pantai: low beach areas near ocean
/// - Tanah: moderate slopes, transition areas
/// - Jalan Bukit: high elevation rocky areas
/// 
/// Menu: Tools > Terrain > Auto Paint Texture
/// </summary>
public class TerrainAutoPainter : EditorWindow
{
    private Terrain terrain;

    [MenuItem("Tools/Terrain/Auto Paint Texture")]
    static void ShowWindow()
    {
        GetWindow<TerrainAutoPainter>("Auto Paint Texture");
    }

    void OnGUI()
    {
        GUILayout.Label("Auto Paint Terrain Texture", EditorStyles.boldLabel);

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);

        if (terrain == null)
        {
            if (GUILayout.Button("Auto-Select Active Terrain"))
                terrain = Terrain.activeTerrain;
            return;
        }

        int layerCount = terrain.terrainData.terrainLayers.Length;
        for (int i = 0; i < layerCount; i++)
        {
            var layer = terrain.terrainData.terrainLayers[i];
            EditorGUILayout.LabelField($"  [{i}] {(layer != null ? layer.name : "null")}");
        }

        EditorGUILayout.HelpBox(
            "Paint rules:\n" +
            "- Grass: flat areas (dominant)\n" +
            "- Jalanan: steep cliffs only\n" +
            "- Pasir Pantai: beach (low + flat)\n" +
            "- Tanah: moderate slopes\n" +
            "- Jalan Bukit: high rocky areas\n\n" +
            "Jalan/path TIDAK di-paint, gambar sendiri.",
            MessageType.Info);

        GUILayout.Space(10);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("PAINT TERRAIN", GUILayout.Height(35)))
            PaintTerrain();
        GUI.backgroundColor = Color.white;
    }

    void PaintTerrain()
    {
        TerrainData td = terrain.terrainData;
        int res = td.alphamapResolution;
        int layers = td.terrainLayers.Length;
        float[,,] map = new float[res, res, layers];

        Undo.RegisterCompleteObjectUndo(td, "Auto Paint Terrain");

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float nx = (float)x / (res - 1);
            float ny = (float)y / (res - 1);

            float height = td.GetInterpolatedHeight(nx, ny) / td.size.y;
            float slope = td.GetSteepness(nx, ny);

            float grass = 0f, jalanan = 0f, pasir = 0f, tanah = 0f, jalanBukit = 0f;

            // === PASIR PANTAI: very low + flat (beach) ===
            if (height < 0.015f && slope < 20f)
            {
                pasir = 1f;
            }
            // === JALANAN: very steep (>45 deg, cliff face) ===
            else if (slope > 45f)
            {
                jalanan = 1f;
            }
            // === JALAN BUKIT: high elevation + moderate slope ===
            else if (height > 0.09f && slope > 20f)
            {
                jalanBukit = 0.7f;
                jalanan = 0.3f;
            }
            // === JALAN BUKIT: very high (mountain top) ===
            else if (height > 0.10f)
            {
                jalanBukit = 0.6f;
                grass = 0.4f;
            }
            // === TANAH: moderate slope (25-45 deg) ===
            else if (slope > 25f)
            {
                float t = (slope - 25f) / 20f;
                tanah = t * 0.6f;
                jalanan = t * 0.2f;
                grass = 1f - tanah - jalanan;
            }
            // === TANAH blend: slightly elevated areas ===
            else if (height > 0.07f)
            {
                float t = (height - 0.07f) / 0.03f;
                tanah = t * 0.4f;
                grass = 1f - tanah;
            }
            // === GRASS: everything else (dominant) ===
            else
            {
                grass = 1f;
            }

            // Beach transition blend
            if (height >= 0.015f && height < 0.025f && slope < 15f)
            {
                float blend = 1f - (height - 0.015f) / 0.010f;
                pasir = blend * 0.4f;
                grass = Mathf.Max(0, grass - pasir);
            }

            // Normalize
            float total = grass + jalanan + pasir + tanah + jalanBukit;
            if (total > 0)
            {
                grass /= total;
                jalanan /= total;
                pasir /= total;
                tanah /= total;
                jalanBukit /= total;
            }
            else grass = 1f;

            // Apply layers
            if (layers > 0) map[y, x, 0] = grass;
            if (layers > 1) map[y, x, 1] = jalanan;
            if (layers > 2) map[y, x, 2] = pasir;
            if (layers > 3) map[y, x, 3] = tanah;
            if (layers > 4) map[y, x, 4] = jalanBukit;
            for (int i = 5; i < layers; i++) map[y, x, i] = 0f;
        }

        td.SetAlphamaps(0, 0, map);
        Debug.Log("[Auto Paint] Done!");
    }
}
#endif
