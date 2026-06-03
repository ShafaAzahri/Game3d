#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Generates stairs/steps between two points with a beach/sand area at the bottom.
/// Place two empty GameObjects as start (top) and end (bottom) markers,
/// or use the default positions.
/// 
/// Menu: Tools > Environment > Create Stairs to Beach
/// </summary>
public class StairGenerator : EditorWindow
{
    private Transform startPoint; // top (village level)
    private Transform endPoint;   // bottom (beach/water level)

    private int stepCount = 12;
    private float stepWidth = 6f;
    private float stepDepth = 1.2f;
    private float railHeight = 1.2f;
    private bool addRails = true;
    private bool addBeach = true;
    private float beachWidth = 20f;
    private float beachLength = 30f;
    private string stairLayer = "Default";

    [MenuItem("Tools/Environment/Create Stairs to Beach")]
    static void ShowWindow()
    {
        GetWindow<StairGenerator>("Stairs to Beach");
    }

    void OnGUI()
    {
        GUILayout.Label("Stairs to Beach Generator", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "1. Place 2 empty GameObjects as markers:\n" +
            "   - Start = top (village level)\n" +
            "   - End = bottom (beach/water level)\n" +
            "2. Drag them here\n" +
            "3. Click Generate\n\n" +
            "Or leave empty to use selected object as top.",
            MessageType.Info);

        startPoint = (Transform)EditorGUILayout.ObjectField("Start (Top)", startPoint, typeof(Transform), true);
        endPoint = (Transform)EditorGUILayout.ObjectField("End (Bottom)", endPoint, typeof(Transform), true);

        GUILayout.Space(10);
        GUILayout.Label("Stair Settings", EditorStyles.boldLabel);
        stepCount = EditorGUILayout.IntSlider("Step Count", stepCount, 4, 30);
        stepWidth = EditorGUILayout.Slider("Step Width", stepWidth, 2f, 15f);
        stepDepth = EditorGUILayout.Slider("Step Depth", stepDepth, 0.5f, 3f);
        addRails = EditorGUILayout.Toggle("Add Rails", addRails);
        if (addRails)
            railHeight = EditorGUILayout.Slider("Rail Height", railHeight, 0.5f, 2f);

        GUILayout.Space(10);
        GUILayout.Label("Beach Settings", EditorStyles.boldLabel);
        addBeach = EditorGUILayout.Toggle("Add Beach Area", addBeach);
        if (addBeach)
        {
            beachWidth = EditorGUILayout.Slider("Beach Width", beachWidth, 10f, 60f);
            beachLength = EditorGUILayout.Slider("Beach Length", beachLength, 15f, 80f);
        }

        GUILayout.Space(10);
        stairLayer = EditorGUILayout.TextField("Stair Layer", stairLayer);

        GUILayout.Space(15);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("GENERATE STAIRS + BEACH", GUILayout.Height(35)))
        {
            GenerateStairs();
        }
        GUI.backgroundColor = Color.white;
    }

    void GenerateStairs()
    {
        Vector3 top, bottom;

        if (startPoint != null && endPoint != null)
        {
            top = startPoint.position;
            bottom = endPoint.position;
        }
        else if (Selection.activeTransform != null)
        {
            top = Selection.activeTransform.position;
            bottom = top + new Vector3(0, -15f, 20f);
            Debug.Log("[Stairs] Using selected object as top, auto-placing bottom 15m below, 20m forward.");
        }
        else
        {
            Debug.LogWarning("[Stairs] No start/end points! Select an object or assign transforms.");
            return;
        }

        GameObject parent = new GameObject("Stairs_To_Beach");
        parent.transform.position = top;
        Undo.RegisterCreatedObjectUndo(parent, "Create Stairs");

        // Direction from top to bottom
        Vector3 direction = (bottom - top).normalized;
        float totalDistance = Vector3.Distance(top, bottom);
        float heightDiff = top.y - bottom.y;

        // Step dimensions
        float stepHeight = heightDiff / stepCount;
        float horizontalDist = totalDistance / stepCount;

        // Horizontal direction (ignore Y)
        Vector3 horizontalDir = new Vector3(direction.x, 0, direction.z).normalized;
        Quaternion stairRotation = Quaternion.LookRotation(horizontalDir);

        // === GENERATE STEPS ===
        GameObject stepsParent = new GameObject("Steps");
        stepsParent.transform.parent = parent.transform;

        Material stepMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        stepMat.color = new Color(0.55f, 0.45f, 0.35f); // wood/stone color

        for (int i = 0; i < stepCount; i++)
        {
            float t = (float)i / stepCount;
            Vector3 stepPos = Vector3.Lerp(top, bottom, t);
            // Adjust Y to be stepped (not smooth slope)
            stepPos.y = top.y - (i * stepHeight);

            GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"Step_{i}";
            step.transform.parent = stepsParent.transform;
            step.transform.position = stepPos;
            step.transform.localScale = new Vector3(stepWidth, stepHeight * 0.8f, stepDepth);
            step.transform.rotation = stairRotation;
            step.GetComponent<Renderer>().sharedMaterial = stepMat;

            // Set layer
            int layerIndex = LayerMask.NameToLayer(stairLayer);
            if (layerIndex >= 0) step.layer = layerIndex;
        }

        // === RAILS ===
        if (addRails)
        {
            Material railMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            railMat.color = new Color(0.40f, 0.30f, 0.20f); // dark wood

            GameObject railsParent = new GameObject("Rails");
            railsParent.transform.parent = parent.transform;

            // Left rail
            CreateRail(top, bottom, stairRotation, -stepWidth * 0.55f, railMat, railsParent.transform, "Rail_Left");
            // Right rail
            CreateRail(top, bottom, stairRotation, stepWidth * 0.55f, railMat, railsParent.transform, "Rail_Right");
        }

        // === BEACH AREA ===
        if (addBeach)
        {
            GameObject beachParent = new GameObject("Beach");
            beachParent.transform.parent = parent.transform;

            // Sand ground plane at bottom of stairs
            Vector3 beachCenter = bottom + horizontalDir * (beachLength * 0.4f);
            beachCenter.y = bottom.y - 0.1f;

            GameObject sand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sand.name = "Sand_Ground";
            sand.transform.parent = beachParent.transform;
            sand.transform.position = beachCenter;
            sand.transform.localScale = new Vector3(beachWidth, 0.3f, beachLength);
            sand.transform.rotation = stairRotation;

            Material sandMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sandMat.color = new Color(0.85f, 0.78f, 0.60f); // sand color
            sand.GetComponent<Renderer>().sharedMaterial = sandMat;

            // Set sand as ground layer too
            int groundLayerIdx = LayerMask.NameToLayer(stairLayer);
            if (groundLayerIdx >= 0) sand.layer = groundLayerIdx;

            // Water edge (ocean plane at beach end)
            Vector3 waterPos = bottom + horizontalDir * (beachLength * 0.8f);
            waterPos.y = bottom.y - 0.5f;

            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Ocean_Water";
            water.transform.parent = beachParent.transform;
            water.transform.position = waterPos;
            water.transform.localScale = new Vector3(beachWidth * 0.3f, 1, beachLength * 0.2f);
            water.transform.rotation = stairRotation;

            // Try to use existing water material
            Material waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Map/air/water.mat");
            if (waterMat == null)
            {
                waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                waterMat.color = new Color(0.12f, 0.35f, 0.60f);
            }
            water.GetComponent<Renderer>().sharedMaterial = waterMat;

            // Rocks scattered on beach
            Material rockMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            rockMat.color = new Color(0.40f, 0.38f, 0.35f);

            for (int i = 0; i < 8; i++)
            {
                Vector3 rockPos = beachCenter +
                    stairRotation * new Vector3(
                        Random.Range(-beachWidth * 0.4f, beachWidth * 0.4f),
                        0.3f,
                        Random.Range(-beachLength * 0.3f, beachLength * 0.4f));

                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = $"Beach_Rock_{i}";
                rock.transform.parent = beachParent.transform;
                rock.transform.position = rockPos;
                float s = Random.Range(0.4f, 1.5f);
                rock.transform.localScale = new Vector3(s * 1.2f, s * 0.7f, s);
                rock.transform.rotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), 0);
                rock.GetComponent<Renderer>().sharedMaterial = rockMat;
            }
        }

        Selection.activeGameObject = parent;
        Debug.Log("[Stairs] Generated stairs + beach!");
    }

    void CreateRail(Vector3 top, Vector3 bottom, Quaternion rot, float sideOffset, Material mat, Transform parent, string name)
    {
        float totalDist = Vector3.Distance(top, bottom);
        Vector3 dir = (bottom - top).normalized;
        Vector3 mid = (top + bottom) * 0.5f;

        // Offset to the side
        Vector3 sideDir = rot * Vector3.right;
        mid += sideDir * sideOffset;

        // Rail posts (vertical)
        int postCount = 5;
        for (int i = 0; i < postCount; i++)
        {
            float t = (float)i / (postCount - 1);
            Vector3 postPos = Vector3.Lerp(top, bottom, t);
            postPos += sideDir * sideOffset;
            postPos.y += railHeight * 0.5f;

            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = $"{name}_Post_{i}";
            post.transform.parent = parent;
            post.transform.position = postPos;
            post.transform.localScale = new Vector3(0.15f, railHeight, 0.15f);
            post.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // Top rail (horizontal bar following slope)
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = name;
        rail.transform.parent = parent;
        rail.transform.position = mid + Vector3.up * railHeight;
        rail.transform.localScale = new Vector3(0.1f, 0.1f, totalDist);
        rail.transform.rotation = Quaternion.LookRotation(dir);
        rail.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
#endif
