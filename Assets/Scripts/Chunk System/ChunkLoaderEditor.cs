using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChunkLoader))]
public class ChunkLoaderEditor : Editor
{
    public void OnEnable()
    {
        
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ChunkLoader ChunkLoader = (ChunkLoader)target;

        EditorGUILayout.LabelField("Chunk Generation Testing");
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Generate Chunks"))
        {
            ChunkLoader.ClearExistingChunks(); // Not very certain about this one

            ChunkLoader.HandleChunkLoading();
        }

        if (GUILayout.Button("Clear Chunks"))
        {
            ChunkLoader.ClearExistingChunks();
        }

        EditorGUILayout.EndHorizontal();

    }
}
