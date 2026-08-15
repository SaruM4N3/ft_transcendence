using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SortingLayer_Auto))]
[CanEditMultipleObjects]
public class SortingLayer_AutoEditor : Editor
{
    private void OnSceneGUI()
    {
        SortingLayer_Auto sortScript = (SortingLayer_Auto)target;
        Transform t = sortScript.transform;

        Vector3 worldPivot = sortScript.SortPivotWorldPosition;

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(worldPivot, Vector3.forward, 0.08f);
        Handles.DrawLine(t.position, worldPivot);

        EditorGUI.BeginChangeCheck();
        Vector3 newWorldPivot = Handles.PositionHandle(worldPivot, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(sortScript, "Move Sort Pivot");
            sortScript.SortPivotOffset = t.InverseTransformPoint(newWorldPivot);
            EditorUtility.SetDirty(sortScript);
        }
    }
}
