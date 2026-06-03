#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Creates a waterfall with particle effects and a sloped path beside it.
/// Menu: Tools > Environment > Create Waterfall
/// </summary>
public class WaterfallSetup : EditorWindow
{
    private Vector3 position = new Vector3(303f, 21.5f, 205f);
    private float waterfallHeight = 25f;
    private float waterfallWidth = 8f;
    private float pathWidth = 4f;

    [MenuItem("Tools/Environment/Create Waterfall")]
    static void ShowWindow()
    {
        var window = GetWindow<WaterfallSetup>("Waterfall Creator");
        window.minSize = new Vector2(300, 250);
    }

    void OnGUI()
    {
        GUILayout.Label("Waterfall Creator", EditorStyles.boldLabel);
        GUILayout.Space(5);

        position = EditorGUILayout.Vector3Field("Base Position (near Cube)", position);
        waterfallHeight = EditorGUILayout.Slider("Waterfall Height", waterfallHeight, 10f, 50f);
        waterfallWidth = EditorGUILayout.Slider("Waterfall Width", waterfallWidth, 4f, 15f);
        pathWidth = EditorGUILayout.Slider("Path Width", pathWidth, 2f, 8f);

        GUILayout.Space(10);
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("CREATE WATERFALL", GUILayout.Height(35)))
        {
            CreateWaterfall();
        }
        GUI.backgroundColor = Color.white;
    }

    void CreateWaterfall()
    {
        GameObject parent = new GameObject("Waterfall_System");
        parent.transform.position = position;
        Undo.RegisterCreatedObjectUndo(parent, "Create Waterfall");

        // 1. Cliff/Rock wall behind waterfall
        CreateCliffWall(parent.transform);

        // 2. Water falling particles
        CreateWaterParticles(parent.transform);

        // 3. Splash at bottom
        CreateSplashParticles(parent.transform);

        // 4. Water pool at base
        CreateWaterPool(parent.transform);

        // 5. Mist/fog particles
        CreateMistParticles(parent.transform);

        // 6. Sloped path beside waterfall
        CreateSlopedPath(parent.transform);

        // 7. Rocks decoration
        CreateRocks(parent.transform);

        Selection.activeGameObject = parent;
        Debug.Log("[Waterfall] Created at " + position);
    }

    void CreateCliffWall(Transform parent)
    {
        GameObject cliff = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cliff.name = "Cliff_Wall";
        cliff.transform.parent = parent;
        cliff.transform.localPosition = new Vector3(0, waterfallHeight * 0.5f, -1f);
        cliff.transform.localScale = new Vector3(waterfallWidth + 6f, waterfallHeight, 3f);

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.35f, 0.30f, 0.25f); // dark rock
        cliff.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void CreateWaterParticles(Transform parent)
    {
        GameObject waterGO = new GameObject("Water_Falling");
        waterGO.transform.parent = parent;
        waterGO.transform.localPosition = new Vector3(0, waterfallHeight, 0);

        ParticleSystem ps = waterGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = waterfallHeight / 12f; // time to fall
        main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startColor = new Color(0.7f, 0.85f, 1f, 0.8f);
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 2.5f;

        var emission = ps.emission;
        emission.rateOverTime = 150f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(waterfallWidth * 0.8f, 0.5f, 0.3f);

        // Stretch particles to look like water streams
        var renderer = waterGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.3f;
        renderer.lengthScale = 2f;

        // Material
        Material waterMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        waterMat.color = new Color(0.6f, 0.82f, 0.95f, 0.7f);
        waterMat.SetFloat("_Surface", 1); // transparent
        renderer.sharedMaterial = waterMat;

        // Size over lifetime (thinner at top, wider at bottom)
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 1.5f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime (fade at end)
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.8f, 0.9f, 1f), 0f),
                new GradientColorKey(new Color(0.6f, 0.8f, 0.95f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.7f, 0.7f),
                new GradientAlphaKey(0.3f, 1f)
            }
        );
        colorOverLife.color = grad;
    }

    void CreateSplashParticles(Transform parent)
    {
        GameObject splashGO = new GameObject("Water_Splash");
        splashGO.transform.parent = parent;
        splashGO.transform.localPosition = new Vector3(0, 0.5f, 1f);

        ParticleSystem ps = splashGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startColor = new Color(0.8f, 0.9f, 1f, 0.6f);
        main.maxParticles = 200;
        main.gravityModifier = 1.5f;

        var emission = ps.emission;
        emission.rateOverTime = 80f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = waterfallWidth * 0.4f;
        shape.rotation = new Vector3(-90f, 0, 0);

        var renderer = splashGO.GetComponent<ParticleSystemRenderer>();
        Material splashMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        splashMat.color = new Color(0.85f, 0.92f, 1f, 0.5f);
        renderer.sharedMaterial = splashMat;
    }

    void CreateMistParticles(Transform parent)
    {
        GameObject mistGO = new GameObject("Mist");
        mistGO.transform.parent = parent;
        mistGO.transform.localPosition = new Vector3(0, 2f, 3f);

        ParticleSystem ps = mistGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startColor = new Color(0.9f, 0.95f, 1f, 0.15f);
        main.maxParticles = 50;
        main.gravityModifier = -0.05f; // float up slightly

        var emission = ps.emission;
        emission.rateOverTime = 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(waterfallWidth, 3f, 4f);

        var renderer = mistGO.GetComponent<ParticleSystemRenderer>();
        Material mistMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mistMat.color = new Color(1f, 1f, 1f, 0.1f);
        renderer.sharedMaterial = mistMat;

        // Size over lifetime (grow)
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.5f);
        curve.AddKey(1f, 1.5f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    void CreateWaterPool(Transform parent)
    {
        GameObject pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pool.name = "Water_Pool";
        pool.transform.parent = parent;
        pool.transform.localPosition = new Vector3(0, 0.1f, 2f);
        pool.transform.localScale = new Vector3(waterfallWidth * 1.5f, 0.2f, waterfallWidth * 1.2f);

        Material poolMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        poolMat.color = new Color(0.15f, 0.40f, 0.60f, 0.85f);
        pool.GetComponent<Renderer>().sharedMaterial = poolMat;
    }

    void CreateSlopedPath(Transform parent)
    {
        // Path going up beside the waterfall (right side)
        GameObject pathParent = new GameObject("Sloped_Path");
        pathParent.transform.parent = parent;
        pathParent.transform.localPosition = Vector3.zero;

        int segments = 8;
        float segHeight = waterfallHeight / segments;

        for (int i = 0; i < segments; i++)
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = $"Path_Segment_{i}";
            seg.transform.parent = pathParent.transform;

            // Path spirals up on the right side of the waterfall
            float t = (float)i / segments;
            float xOffset = (waterfallWidth * 0.5f) + pathWidth + 1f;
            float yPos = i * segHeight + segHeight * 0.5f;
            float zOffset = -2f + t * 4f; // slight forward movement

            seg.transform.localPosition = new Vector3(xOffset, yPos, zOffset);
            seg.transform.localScale = new Vector3(pathWidth, 0.3f, segHeight * 1.3f);
            // Tilt slightly for slope feel
            seg.transform.localRotation = Quaternion.Euler(0, 0, -8f);

            Material pathMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            pathMat.color = new Color(0.45f, 0.38f, 0.28f); // dirt/stone path
            seg.GetComponent<Renderer>().sharedMaterial = pathMat;
        }

        // Railing/edge stones along path
        for (int i = 0; i < segments; i++)
        {
            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = $"Path_Rail_{i}";
            rail.transform.parent = pathParent.transform;

            float t = (float)i / segments;
            float xOffset = (waterfallWidth * 0.5f) + 1f;
            float yPos = i * segHeight + segHeight * 0.5f + 0.3f;
            float zOffset = -2f + t * 4f;

            rail.transform.localPosition = new Vector3(xOffset, yPos, zOffset);
            rail.transform.localScale = new Vector3(0.5f, 0.8f, segHeight * 0.8f);

            Material railMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            railMat.color = new Color(0.40f, 0.35f, 0.30f); // darker stone
            rail.GetComponent<Renderer>().sharedMaterial = railMat;
        }
    }

    void CreateRocks(Transform parent)
    {
        GameObject rocksParent = new GameObject("Rocks");
        rocksParent.transform.parent = parent;

        // Rocks around the base of waterfall
        Vector3[] rockPositions = new Vector3[]
        {
            new Vector3(-waterfallWidth * 0.6f, 0.5f, 2f),
            new Vector3(waterfallWidth * 0.6f, 0.5f, 1.5f),
            new Vector3(-waterfallWidth * 0.4f, 0.3f, 4f),
            new Vector3(waterfallWidth * 0.3f, 0.4f, 3.5f),
            new Vector3(0, 0.3f, 5f),
            new Vector3(-waterfallWidth * 0.7f, 1f, 0f),
            new Vector3(waterfallWidth * 0.7f, 0.8f, 0.5f),
            // Rocks on cliff face
            new Vector3(-waterfallWidth * 0.5f, waterfallHeight * 0.3f, -0.5f),
            new Vector3(waterfallWidth * 0.5f, waterfallHeight * 0.5f, -0.5f),
            new Vector3(-waterfallWidth * 0.3f, waterfallHeight * 0.7f, -0.5f),
        };

        for (int i = 0; i < rockPositions.Length; i++)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = $"Rock_{i}";
            rock.transform.parent = rocksParent.transform;
            rock.transform.localPosition = rockPositions[i];

            float scale = Random.Range(0.8f, 2.5f);
            rock.transform.localScale = new Vector3(
                scale * Random.Range(0.8f, 1.5f),
                scale * Random.Range(0.6f, 1.2f),
                scale * Random.Range(0.8f, 1.3f));
            rock.transform.localRotation = Quaternion.Euler(
                Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-15f, 15f));

            Material rockMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            float shade = Random.Range(0.25f, 0.45f);
            rockMat.color = new Color(shade, shade * 0.9f, shade * 0.8f);
            rock.GetComponent<Renderer>().sharedMaterial = rockMat;
        }
    }
}
#endif
