using UnityEngine;

/// <summary>Automatically sets sprite sorting order based on Y position.</summary>
public class SortingLayer_Auto : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int sortingOffset = 0;

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
        spriteRenderer.sortingOrder = Mathf.RoundToInt(transform.position.y * -100) + sortingOffset;
    }
}
