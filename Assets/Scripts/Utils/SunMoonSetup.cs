#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// One-click setup for sun & moon visuals.
/// Creates unlit materials and assigns to DayNightCycle.
/// Menu: Tools > Environment > Setup Sun & Moon Visuals
/// </summary>
public class SunMoonSetup
{
    [MenuItem("Tools/Environment/Setup Sun and Moon Visuals")]
    static void Setup()
    {
        // Find SunVisual and MoonVisual in scene
        GameObject sunGO = GameObject.Find("SunVisual");
        GameObject moonGO = GameObject.Find("MoonVisual");

        if (sunGO == null || moonGO == null)
        {
            EditorUtility.DisplayDialog("Error", "SunVisual or MoonVisual not found in scene!", "OK");
            return;
        }

        // Create materials folder
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // Sun material (bright yellow unlit)
        string sunMatPath = "Assets/Materials/SunVisual.mat";
        Material sunMat = AssetDatabase.LoadAssetAtPath<Material>(sunMatPath);
        if (sunMat == null)
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
            sunMat = new Material(unlitShader);
            sunMat.color = new Color(1f, 0.85f, 0.3f); // warm yellow
            AssetDatabase.CreateAsset(sunMat, sunMatPath);
        }

        // Moon material (pale blue-white unlit)
        string moonMatPath = "Assets/Materials/MoonVisual.mat";
        Material moonMat = AssetDatabase.LoadAssetAtPath<Material>(moonMatPath);
        if (moonMat == null)
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
            moonMat = new Material(unlitShader);
            moonMat.color = new Color(0.85f, 0.88f, 0.95f); // pale blue-white
            AssetDatabase.CreateAsset(moonMat, moonMatPath);
        }

        // Assign materials
        sunGO.GetComponent<Renderer>().sharedMaterial = sunMat;
        moonGO.GetComponent<Renderer>().sharedMaterial = moonMat;

        // Find DayNightCycle and assign visuals
        DayNightCycle dnc = Object.FindObjectOfType<DayNightCycle>();
        if (dnc != null)
        {
            dnc.sunVisual = sunGO.transform;
            dnc.moonVisual = moonGO.transform;
            EditorUtility.SetDirty(dnc);
            Debug.Log("[Setup] Sun & Moon visuals assigned to DayNightCycle!");
        }
        else
        {
            Debug.LogWarning("[Setup] DayNightCycle not found in scene. Please assign sunVisual and moonVisual manually.");
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done",
            "Sun & Moon visuals setup complete!\n\n" +
            "- SunVisual: yellow unlit sphere (scale 10)\n" +
            "- MoonVisual: blue-white unlit sphere (scale 6)\n" +
            "- Colliders removed\n" +
            "- Assigned to DayNightCycle",
            "OK");
    }
}
#endif
