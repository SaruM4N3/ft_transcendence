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
            "landTilemap", "waterTilemap", "waterBackgroundTilemap", "coastFoamTilemap",
            "platformTilemap", "wallTilemap", "shadowTilemap", "player"),
        new("ChunkStreaming", "Chunk Streaming",
            "chunkSize", "viewDistanceInChunks", "maxChunkGenerationsPerFrame", "seed", "spawnSafeRadius"),
        new("BiomeWater", "Biome & Water",
            "biomeNoiseScale", "biomeTiles", "waterNoiseScale", "waterThreshold",
            "waterTile", "waterBackgroundTile", "coastFoamTile"),
        new("Structures", "Structures",
            "generatePlatforms", "structureChance", "roomSizeRange", "platformNoiseScale",
            "platformFillThreshold", "platformEdgeFalloff", "minPlatformFloorTiles", "interiorFloorTile"),
        new("Walls", "Walls",
            "wallTilesByShape", "waterWallTilesByShape", "shadowTile"),
        new("Stairs", "Stairs",
            "cornerStairsPrefab", "cornerStairsWidth", "edgeStairsPrefab", "edgeStairsWidth",
            "edgeStairsPrefabHeight", "stairsPrefabWidth", "minStairsPerPlatform"),
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
            // Plain Foldout (not BeginFoldoutHeaderGroup): that API is stack-based and throws
            // "can't nest Foldout Headers" as soon as a drawn property (e.g. an array/list field)
            // opens its own foldout-like control while one of these is still open.
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
