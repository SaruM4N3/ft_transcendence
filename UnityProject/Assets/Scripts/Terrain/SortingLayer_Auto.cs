using UnityEngine;

public class SortingLayer_Auto : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int sortingOffset = 0;

    [Tooltip("Local-space Y-sort reference point offset from the transform origin. Drag the Scene view handle to align it with the sprite's visual base. Zero = sort on the transform's own position.")]
    [SerializeField] private Vector3 sortPivotOffset = Vector3.zero;

    public Vector3 SortPivotOffset
    {
        get => sortPivotOffset;
        set => sortPivotOffset = value;
    }

    public Vector3 SortPivotWorldPosition => transform.TransformPoint(sortPivotOffset);

    public void AddSortingOffset(int extra)
    {
        sortingOffset += extra;
    }

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
