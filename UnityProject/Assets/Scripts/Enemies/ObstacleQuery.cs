using UnityEngine;

// Physics2D queries against static colliders (water composite, decor), the only things enemies path around.
public static class ObstacleQuery
{
    private static readonly Collider2D[] overlapBuffer = new Collider2D[16];
    private static readonly RaycastHit2D[] castBuffer = new RaycastHit2D[16];
    private static ContactFilter2D filter;
    private static bool filterReady;

    // True when nothing static blocks a circle of this radius travelling from one point to the other.
    public static bool IsClear(Vector2 from, Vector2 to, float radius)
    {
        EnsureFilter();
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.01f)
            return true;

        int count = Physics2D.CircleCast(from, radius, delta / distance, filter, castBuffer, distance);
        for (int i = 0; i < count; i++)
        {
            if (IsObstacle(castBuffer[i].collider))
                return false;
        }
        return true;
    }

    // True when a static collider overlaps a circle of this radius at the point.
    public static bool IsBlocked(Vector2 point, float radius)
    {
        EnsureFilter();
        int count = Physics2D.OverlapCircle(point, radius, filter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (IsObstacle(overlapBuffer[i]))
                return true;
        }
        return false;
    }

    // True when a static collider touches the whole square area, so small trunks and rocks can't slip between grid samples.
    public static bool IsAreaBlocked(Vector2 center, float size)
    {
        EnsureFilter();
        int count = Physics2D.OverlapBox(center, new Vector2(size, size), 0f, filter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (IsObstacle(overlapBuffer[i]))
                return true;
        }
        return false;
    }

    private static bool IsObstacle(Collider2D collider)
    {
        if (collider == null || collider.isTrigger)
            return false;

        Rigidbody2D body = collider.attachedRigidbody;
        return body == null || body.bodyType == RigidbodyType2D.Static;
    }

    private static void EnsureFilter()
    {
        if (filterReady)
            return;

        filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        filterReady = true;
    }
}
