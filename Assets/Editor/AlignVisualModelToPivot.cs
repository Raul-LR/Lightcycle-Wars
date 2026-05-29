// Assets/Editor/AlignVisualModelToPivot.cs
using UnityEngine;
using UnityEditor;

public class AlignVisualModelToPivot
{
    [MenuItem("Tools/Align VisualModel To Pivot")]
    private static void AlignSelectedVisualModel()
    {
        if (Selection.activeTransform == null)
        {
            EditorUtility.DisplayDialog("Align VisualModel", "Select the LightCycle root object in the Hierarchy first.", "OK");
            return;
        }

        Transform lightCycle = Selection.activeTransform;

        // Try to find VisualModel inside LightCycleModel or at known locations.
        Transform visualModel = lightCycle.Find("LightCycleModel/VisualModel");
        if (visualModel == null)
            visualModel = lightCycle.Find("VisualModel");
        if (visualModel == null)
        {
            EditorUtility.DisplayDialog("Align VisualModel", "Could not find VisualModel as child of the selected object.\nExpected: LightCycle/LightCycleModel/VisualModel or LightCycle/VisualModel", "OK");
            return;
        }

        // Try to find BackWheel inside the visual model (common names)
        Transform backWheel = visualModel.Find("BackWheel");
        if (backWheel == null)
        {
            // fallback: search recursively
            backWheel = visualModel.GetComponentInChildren<Transform>();
            if (backWheel != null)
            {
                // prefer a child with "back" in name
                Transform[] all = visualModel.GetComponentsInChildren<Transform>();
                foreach (var t in all)
                {
                    if (t.name.ToLower().Contains("back") || t.name.ToLower().Contains("rear") || t.name.ToLower().Contains("wheel"))
                    {
                        backWheel = t;
                        break;
                    }
                }
            }
        }

        if (backWheel == null)
        {
            EditorUtility.DisplayDialog("Align VisualModel", "Could not find BackWheel under VisualModel. Please ensure BackWheel is a child of VisualModel.", "OK");
            return;
        }

        // Record undo for safe edit
        Undo.RecordObject(visualModel, "Align VisualModel To Pivot");

        // Calculate world delta from backWheel to lightCycle pivot (world)
        Vector3 worldDelta = backWheel.position - lightCycle.position;

        // Move visualModel by -worldDelta so backWheel ends up at lightCycle.position
        visualModel.position -= worldDelta;

        // Optionally snap small floats
        visualModel.localPosition = new Vector3(
            Mathf.Round(visualModel.localPosition.x * 1000f) / 1000f,
            Mathf.Round(visualModel.localPosition.y * 1000f) / 1000f,
            Mathf.Round(visualModel.localPosition.z * 1000f) / 1000f
        );

        EditorUtility.SetDirty(visualModel);
        Debug.Log($"Aligned VisualModel '{visualModel.name}' so BackWheel '{backWheel.name}' sits at LightCycle pivot.");
    }

    [MenuItem("Tools/Align VisualModel To Pivot", true)]
    private static bool ValidateAlign()
    {
        return Selection.activeTransform != null;
    }
}