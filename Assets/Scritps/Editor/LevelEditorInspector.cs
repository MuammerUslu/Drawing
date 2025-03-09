using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelEditor))]
public class LevelEditorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        LevelEditor editor = (LevelEditor)target;

        if (GUILayout.Button("Save Level Data"))
        {
            editor.SaveLevelData();
        }

        if (GUILayout.Button("Clear Level Data"))
        {
            editor.ClearLevelData();
        }
    }
}