using UnityEngine;

/// <summary>Automatically sets sprite sorting order based on Y position.</summary>
public class SortingLayer_Auto : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int sortingOffset = 0;

    void LateUpdate()
    {
        spriteRenderer.sortingOrder = Mathf.RoundToInt(transform.position.y * -100) + sortingOffset;
    }
}
