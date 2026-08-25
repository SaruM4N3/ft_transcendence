using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralMapGenerator))]
public class ProceduralMapGeneratorEditor : Editor
{
    private struct Section
    {
        public string Key;
        public string Label;
        public string[] Properties;

        public Section(string key, string label, params string[] properties)
        {
            Key = key;
            Label = label;
            Properties = properties;
        }
    }

    private static readonly Section[] Sections =
    {
        new("References", "References",
            "landTilemap", "waterTilemap", "waterBackgroundTilemap", "coastFoamTilemap", "player"),
        new("ChunkStreaming", "Chunk Streaming",
            "chunkSize", "viewDistanceInChunks", "maxChunkGenerationsPerFrame", "seed", "spawnSafeRadius"),
        new("BiomeWater", "Biome & Water",
            "biomeNoiseScale", "biomeTiles", "waterNoiseScale", "waterThreshold",
            "waterTile", "waterBackgroundTile", "coastFoamTile"),
        new("Decor", "Decor", "decorEntries"),
    };

    private const string FoldoutPrefPrefix = "ProceduralMapGeneratorEditor.Foldout.";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        foreach (Section section in Sections)
        {
            string prefKey = FoldoutPrefPrefix + section.Key;
            bool expanded = EditorPrefs.GetBool(prefKey, true);

            EditorGUILayout.Space(2);
            // Plain Foldout, not BeginFoldoutHeaderGroup - that stack-based API throws if a nested field opens its own foldout.
            bool newExpanded = EditorGUILayout.Foldout(expanded, section.Label, true, EditorStyles.foldoutHeader);
            if (newExpanded != expanded)
                EditorPrefs.SetBool(prefKey, newExpanded);

            if (newExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (string propertyName in section.Properties)
                {
                    SerializedProperty property = serializedObject.FindProperty(propertyName);
                    if (property != null)
                        EditorGUILayout.PropertyField(property, true);
                }
                EditorGUI.indentLevel--;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
