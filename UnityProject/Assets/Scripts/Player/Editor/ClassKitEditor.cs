using UnityEditor;
using UnityEngine;

// Hides the Projectile fields unless hasProjectile is checked, so non-projectile classes don't show them.
[CustomEditor(typeof(ClassKit))]
public class ClassKitEditor : Editor
{
    private static readonly string[] ProjectileOnlyFields = { "projectileSpeed", "projectileMaxDistance", "pierceCount" };
    private static readonly string[] ShieldOnlyFields = { "shieldHoldDuration", "shieldHealth", "shieldKnockbackForce", "shieldKnockbackDuration", "shieldPushInterval", "shieldTurnSpeed", "shieldHoldAnimation" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        bool hasProjectile = serializedObject.FindProperty("hasProjectile").boolValue;
        bool hasShield = serializedObject.FindProperty("hasShield").boolValue;

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (!hasProjectile && System.Array.IndexOf(ProjectileOnlyFields, property.name) >= 0)
                continue;
            if (!hasShield && System.Array.IndexOf(ShieldOnlyFields, property.name) >= 0)
                continue;

            EditorGUILayout.PropertyField(property);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
