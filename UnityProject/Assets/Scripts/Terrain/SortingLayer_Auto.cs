using UnityEngine;

public class SortingLayer_Auto : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int sortingOffset = 0;

    [Tooltip("Local-space offset from this transform used as the Y-sort reference point instead of the transform's own origin. Drag the handle in the Scene view (SortingLayer_AutoEditor) to align it with the sprite's visual base - e.g. a tall tree's trunk - when the sprite pivot doesn't already sit there. Zero (default) keeps the old behavior of sorting on the transform's own position.")]
    [SerializeField] private Vector3 sortPivotOffset = Vector3.zero;

    /// <summary>Local-space Y-sort reference point, exposed for the Scene view handle.</summary>
    public Vector3 SortPivotOffset
    {
        get => sortPivotOffset;
        set => sortPivotOffset = value;
    }

    public Vector3 SortPivotWorldPosition => transform.TransformPoint(sortPivotOffset);

    /// <summary>Breaks ties between objects landing on the same Y (e.g. grid-aligned decor).</summary>
    public void AddSortingOffset(int extra)
    {
        sortingOffset += extra;
    }

    // Use over AddSortingOffset for pooled/reused objects - Add would keep compounding on a stale offset.
    public void SetSortingOffset(int value)
    {
        sortingOffset = value;
    }

    void LateUpdate()
    {
        spriteRenderer.sortingOrder = Mathf.RoundToInt(SortPivotWorldPosition.y * -100) + sortingOffset;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(SortPivotWorldPosition, 0.08f);
    }
}
