using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
#endif

// ============================================================
// GRASS DATA - stores painted grass positions
// ============================================================
[CreateAssetMenu(fileName = "GrassData", menuName = "Grass System/Grass Data")]
public class GrassData : ScriptableObject
{
    [System.Serializable]
    public struct GrassInstance
    {
        public Vector3 position;
        public float rotation;
        public float scale;
        public Color color;
    }

    public List<GrassInstance> instances = new List<GrassInstance>();

    public void AddInstance(Vector3 pos, float rot, float scale, Color color)
    {
        instances.Add(new GrassInstance
        {
            position = pos,
            rotation = rot,
            scale = scale,
            color = color
        });
    }

    public void RemoveInstancesInRadius(Vector3 center, float radius)
    {
        float sqrRadius = radius * radius;
        instances.RemoveAll(g => (g.position - center).sqrMagnitude < sqrRadius);
    }

    public void Clear()
    {
        instances.Clear();
    }
}

// ============================================================
// GRASS RENDERER - renders grass with GPU Instancing
// ============================================================
public class GrassRenderer : MonoBehaviour
{
    [Header("Data")]
    public GrassData grassData;

    [Header("Rendering")]
    public Mesh grassMesh;
    public Material grassMaterial;

    [Header("Wind")]
    public float windSpeed = 1.0f;
    public float windStrength = 0.3f;
    public Vector3 windDirection = new Vector3(1, 0, 0.5f);

    [Header("Interaction")]
    [Tooltip("Drag your Player here")]
    public Transform playerTransform;
    public float interactionRadius = 2.0f;
    public float interactionStrength = 1.5f;

    [Header("Performance")]
    public float renderDistance = 100f;

    private Matrix4x4[] matrices;
    private Vector4[] interactorPositions = new Vector4[10];
    private MaterialPropertyBlock propertyBlock;
    private const int BATCH_SIZE = 1023;

    void Start()
    {
        propertyBlock = new MaterialPropertyBlock();
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
        if (matrices == null) matrices = new Matrix4x4[BATCH_SIZE];
    }

    void Update()
    {
        if (grassData == null || grassMesh == null || grassMaterial == null) return;
        if (grassData.instances.Count == 0) return;

        UpdateShader();
        Render();
    }

    void UpdateShader()
    {
        Shader.SetGlobalFloat("_WindSpeed", windSpeed);
        Shader.SetGlobalFloat("_WindStrength", windStrength);
        Shader.SetGlobalVector("_WindDirection", new Vector4(windDirection.x, windDirection.y, windDirection.z, 0));
        Shader.SetGlobalFloat("_InteractionRadius", interactionRadius);
        Shader.SetGlobalFloat("_InteractionStrength", interactionStrength);

        int count = 0;
        if (playerTransform != null)
        {
            Vector3 p = playerTransform.position;
            interactorPositions[0] = new Vector4(p.x, p.y, p.z, 1);
            count = 1;
        }
        for (int i = count; i < 10; i++) interactorPositions[i] = Vector4.zero;

        Shader.SetGlobalVectorArray("_GrassInteractorPositions", interactorPositions);
        Shader.SetGlobalInt("_GrassInteractorCount", count);
    }

    void Render()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;
        float sqrDist = renderDistance * renderDistance;
        var instances = grassData.instances;
        int total = instances.Count;
        int batchIndex = 0;

        for (int i = 0; i < total; i++)
        {
            var g = instances[i];
            if ((g.position - camPos).sqrMagnitude > sqrDist) continue;

            matrices[batchIndex] = Matrix4x4.TRS(
                g.position,
                Quaternion.Euler(0, g.rotation, 0),
                Vector3.one * g.scale);
            batchIndex++;

            if (batchIndex >= BATCH_SIZE)
            {
                Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, matrices, batchIndex, propertyBlock);
                batchIndex = 0;
            }
        }

        if (batchIndex > 0)
            Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, matrices, batchIndex, propertyBlock);
    }
}

// ============================================================
// EDITOR TOOLS - Grass Painter & Mesh Generator
// ============================================================
#if UNITY_EDITOR
[EditorTool("Grass Painter")]
public class GrassPainterTool : EditorTool
{
    private float brushSize = 3f;
    private float density = 5f;
    private float minScale = 0.7f;
    private float maxScale = 1.3f;
    private Color baseColor = new Color(0.2f, 0.55f, 0.1f, 1f);
    private Color tipColor = new Color(0.4f, 0.85f, 0.2f, 1f);
    private LayerMask paintMask = ~0;
    private GrassData targetData;
    private bool isPainting = false;
    private float lastPaintTime = 0f;

    [MenuItem("Tools/Grass System/Open Grass Painter")]
    static void ActivateTool() => ToolManager.SetActiveTool<GrassPainterTool>();

    public override void OnActivated()
    {
        var r = Object.FindObjectOfType<GrassRenderer>();
        if (r != null) targetData = r.grassData;
        SceneView.lastActiveSceneView?.ShowNotification(
            new GUIContent("Grass Painter\nLMB = Paint | Shift+LMB = Erase"));
    }

    public override void OnToolGUI(EditorWindow window)
    {
        if (window is not SceneView sv) return;

        Handles.BeginGUI();
        DrawPanel();
        Handles.EndGUI();

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, paintMask)) return;

        // Brush preview
        Handles.color = e.shift ? new Color(1, 0.3f, 0.3f, 0.3f) : new Color(0.3f, 1, 0.3f, 0.3f);
        Handles.DrawSolidDisc(hit.point, hit.normal, brushSize);
        Handles.color = Color.white;
        Handles.DrawWireDisc(hit.point, hit.normal, brushSize);

        if (e.type == EventType.ScrollWheel && e.control)
        {
            brushSize = Mathf.Clamp(brushSize + e.delta.y * -0.3f, 0.5f, 30f);
            e.Use();
        }

        if (e.type == EventType.MouseDown && e.button == 0)
        { isPainting = true; Paint(hit.point, e.shift); e.Use(); }
        else if (e.type == EventType.MouseDrag && e.button == 0 && isPainting)
        {
            if (Time.realtimeSinceStartup - lastPaintTime > 0.05f)
            { Paint(hit.point, e.shift); lastPaintTime = Time.realtimeSinceStartup; }
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        { isPainting = false; e.Use(); }

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        sv.Repaint();
    }

    void Paint(Vector3 center, bool erase)
    {
        if (targetData == null)
        { Debug.LogWarning("[Grass] No GrassData! Create: Assets > Create > Grass System > Grass Data"); return; }

        Undo.RecordObject(targetData, erase ? "Erase Grass" : "Paint Grass");

        if (erase) { targetData.RemoveInstancesInRadius(center, brushSize); }
        else
        {
            int count = Mathf.RoundToInt(density * brushSize * 0.5f);
            for (int i = 0; i < count; i++)
            {
                Vector2 rnd = Random.insideUnitCircle * brushSize;
                Vector3 pos = center + new Vector3(rnd.x, 0, rnd.y);
                if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit snap, 30f, paintMask))
                    pos = snap.point;
                targetData.AddInstance(pos, Random.Range(0f, 360f),
                    Random.Range(minScale, maxScale),
                    Color.Lerp(baseColor, tipColor, Random.value));
            }
        }
        EditorUtility.SetDirty(targetData);
    }

    void DrawPanel()
    {
        GUILayout.BeginArea(new Rect(10, 10, 250, 320));
        GUILayout.BeginVertical("box");
        GUILayout.Label("Grass Painter", EditorStyles.boldLabel);

        targetData = (GrassData)EditorGUILayout.ObjectField("Grass Data", targetData, typeof(GrassData), false);

        var r = Object.FindObjectOfType<GrassRenderer>();
        if (r != null && targetData != null && r.grassData != targetData)
        { r.grassData = targetData; EditorUtility.SetDirty(r); }

        brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.5f, 30f);
        density = EditorGUILayout.Slider("Density", density, 1f, 20f);
        minScale = EditorGUILayout.Slider("Min Scale", minScale, 0.3f, 2f);
        maxScale = EditorGUILayout.Slider("Max Scale", maxScale, 0.5f, 3f);

        GUILayout.Space(3);
        baseColor = EditorGUILayout.ColorField("Base Color", baseColor);
        tipColor = EditorGUILayout.ColorField("Tip Color", tipColor);

        GUILayout.Space(5);
        int c = targetData != null ? targetData.instances.Count : 0;
        GUILayout.Label($"Grass Count: {c}");

        if (GUILayout.Button("Clear All") && targetData != null)
        {
            if (EditorUtility.DisplayDialog("Clear", "Remove all grass?", "Yes", "No"))
            { Undo.RecordObject(targetData, "Clear"); targetData.Clear(); EditorUtility.SetDirty(targetData); }
        }

        GUILayout.Label("LMB=Paint  Shift+LMB=Erase  Ctrl+Scroll=Size", EditorStyles.miniLabel);
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

public static class GrassMeshGenerator
{
    [MenuItem("Tools/Grass System/Generate Grass Blade Mesh")]
    public static void GenerateBlade()
    {
        Mesh mesh = CreateBlade();
        SaveMesh(mesh, "GrassBlade");
    }

    [MenuItem("Tools/Grass System/Generate Grass Clump Mesh")]
    public static void GenerateClump()
    {
        CombineInstance[] combine = new CombineInstance[3];
        for (int i = 0; i < 3; i++)
        {
            combine[i].mesh = CreateBlade();
            combine[i].transform = Matrix4x4.TRS(
                new Vector3((i - 1) * 0.03f, 0, 0),
                Quaternion.Euler(0, i * 60f, Random.Range(-5f, 5f)),
                Vector3.one * Random.Range(0.85f, 1.15f));
        }
        Mesh clump = new Mesh { name = "GrassClump" };
        clump.CombineMeshes(combine, true, true);
        clump.RecalculateNormals();
        SaveMesh(clump, "GrassClump");
    }

    static Mesh CreateBlade()
    {
        Mesh m = new Mesh { name = "GrassBlade" };
        m.vertices = new Vector3[] {
            new(-0.05f,0,0), new(0.05f,0,0),
            new(-0.04f,0.3f,0), new(0.04f,0.3f,0),
            new(-0.025f,0.6f,0.02f), new(0.025f,0.6f,0.02f),
            new(0,1f,0.04f)
        };
        m.uv = new Vector2[] {
            new(0,0), new(1,0), new(0,0.3f), new(1,0.3f),
            new(0.15f,0.6f), new(0.85f,0.6f), new(0.5f,1)
        };
        m.triangles = new int[] { 0,2,1, 1,2,3, 2,4,3, 3,4,5, 4,6,5 };
        m.colors = new Color[] {
            new(0,1,0,0), new(0,1,0,0),
            new(0,1,0,0.3f), new(0,1,0,0.3f),
            new(0,1,0,0.6f), new(0,1,0,0.6f),
            new(0,1,0,1)
        };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static void SaveMesh(Mesh mesh, string name)
    {
        if (!AssetDatabase.IsValidFolder("Assets/GrassSystem/Meshes"))
            AssetDatabase.CreateFolder("Assets/GrassSystem", "Meshes");
        string path = $"Assets/GrassSystem/Meshes/{name}.asset";
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Grass] Mesh saved: {path}");
        Selection.activeObject = mesh;
    }
}
#endif
