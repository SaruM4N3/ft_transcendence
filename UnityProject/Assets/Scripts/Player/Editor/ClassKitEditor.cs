using UnityEditor;
using UnityEngine;

// Hides the Projectile fields unless hasProjectile is checked, so non-projectile classes don't show them.
[CustomEditor(typeof(ClassKit))]
public class ClassKitEditor : Editor
{
    private static readonly string[] ProjectileOnlyFields = { "projectileSpeed", "projectileMaxDistance", "pierceCount" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        bool hasProjectile = serializedObject.FindProperty("hasProjectile").boolValue;

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (!hasProjectile && System.Array.IndexOf(ProjectileOnlyFields, property.name) >= 0)
                continue;

            EditorGUILayout.PropertyField(property);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
