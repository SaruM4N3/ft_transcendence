using UnityEditor;
using UnityEngine;

// Hides the ranged-only fields unless isRanged is checked, so melee kits don't show them.
[CustomEditor(typeof(EnemyKit))]
public class EnemyKitEditor : Editor
{
    private static readonly string[] RangedOnlyFields = { "projectilePrefab", "projectileSpeed", "projectileMaxDistance", "projectileArcHeight" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        bool isRanged = serializedObject.FindProperty("isRanged").boolValue;

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (!isRanged && System.Array.IndexOf(RangedOnlyFields, property.name) >= 0)
                continue;

            EditorGUILayout.PropertyField(property);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
