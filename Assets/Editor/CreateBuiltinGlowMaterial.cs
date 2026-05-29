using UnityEditor;
using UnityEngine;

public class CreateBuiltinGlowMaterial : MonoBehaviour
{
    [MenuItem("Assets/Create/Glow Trail Material (Built-in)")]
    public static void CreateGlowMaterial()
    {
        // Crear material con el shader Standard Multiplicative (brillo simple)
        Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null)
        {
            Debug.LogError("No se encontró el shader Legacy Shaders/Particles/Additive");
            return;
        }

        Material mat = new Material(shader);
        mat.name = "GlowTrail";

        // Color emisivo brillante
        mat.SetColor("_TintColor", new Color(0.15f, 0.7f, 1f, 1f)); // Azul eléctrico

        // Guardar archivo
        string path = "Assets/Materials/GlowTrail.mat";
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = mat;

        Debug.Log("GlowTrail.mat creado correctamente en " + path);
    }
}
