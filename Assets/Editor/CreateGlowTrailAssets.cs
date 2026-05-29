// Assets/Editor/CreateGlowTrailAssets.cs
using UnityEditor;
using UnityEngine;
using System.IO;

public class CreateGlowTrailAssets
{
    [MenuItem("Tools/TronTrail/Create URP Glow Material")]
    public static void CreateURPGlowMaterial()
    {
        // Asegúrate de que el proyecto está usando URP y que existe el shader "Universal Render Pipeline/Lit"
        string materialPath = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(materialPath))
            AssetDatabase.CreateFolder("Assets", "Materials");

        string assetPath = Path.Combine(materialPath, "GlowTrail_URP.mat");

        // Tratar de encontrar el shader URP Lit
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Shader no encontrado",
                "No se encontró el shader 'Universal Render Pipeline/Lit'. Asegúrate de que URP está instalado en el proyecto.",
                "OK");
            return;
        }

        // Crear material
        Material mat = new Material(urpLit);
        mat.name = "GlowTrail_URP";

        // Surface type opaque (URP Lit uses keywords; set properties accordingly)
        // Emission
        mat.EnableKeyword("_EMISSION");
        // Base color -> negro
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.black);
        // Emission color HDR: cyan-ish (use high intensity)
        Color emission = new Color(0f, 6f, 6f, 1f); // HDR value
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);

        // Optional: make it unlit-looking by reducing smoothness/metallic
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

        // Save asset
        AssetDatabase.CreateAsset(mat, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Material creado", $"Material creado en: {assetPath}\nRecuerda activar Bloom en tu Volume o Renderer para ver el glow.", "OK");
    }

    [MenuItem("Tools/TronTrail/Create TrailSegment Prefab (with material)")]
    public static void CreateTrailSegmentPrefab()
    {
        // Ensure material exists (try to load)
        string matPath = "Assets/Materials/GlowTrail_URP.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null)
        {
            if (!EditorUtility.DisplayDialog("Material no encontrado",
                $"No se encontró {matPath}. ¿Crear material ahora?", "Sí", "No"))
            {
                return;
            }
            CreateURPGlowMaterial();
            material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                EditorUtility.DisplayDialog("Error", "No se pudo crear el material automáticamente.", "OK");
                return;
            }
        }

        // Create a GameObject and add components
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "TrailSegment";
        go.transform.localScale = new Vector3(0.2f, 0.2f, 1f); // default; length se ajusta por código

        // Configure mesh renderer
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        // Configure collider: non-trigger by default (will be enabled/disabled by script)
        BoxCollider col = go.GetComponent<BoxCollider>();
        col.isTrigger = false;
        col.center = Vector3.zero;
        col.size = Vector3.one;

        // Save prefab
        string prefabFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string prefabPath = Path.Combine(prefabFolder, "TrailSegment.prefab");
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);

        // Clean up scene object
        Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Prefab creado", $"Prefab creado en: {prefabPath}", "OK");
    }
}
