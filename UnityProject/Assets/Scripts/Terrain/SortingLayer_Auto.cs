using UnityEngine;

/// <summary>Automatically sets sprite sorting order based on Y position.</summary>
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

    /// <summary>World-space position the sort order is sampled from.</summary>
    public Vector3 SortPivotWorldPosition => transform.TransformPoint(sortPivotOffset);

    /// <summary>Adds to the sorting offset - used to break ties between objects that land on the exact same Y (e.g. grid-aligned decor on the same row).</summary>
    public void AddSortingOffset(int extra)
    {
        sortingOffset += extra;
    }

    /// <summary>Sets the sorting offset to an absolute value - use this (not AddSortingOffset) for
    /// pooled/reused objects, since Add would keep compounding on top of a previous reuse's offset.</summary>
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
