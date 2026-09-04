using System.Collections.Generic;
using UnityEngine;

// Dijkstra cost grid centred on one player: every enemy chasing that player reads its direction in O(1).
public class EnemyFlowField
{
    public const float CellSize = 0.75f;
    public const int Radius = 36;

    private const int Size = Radius * 2 + 1;
    private const float Unreachable = float.PositiveInfinity;

    private struct BlockedEntry
    {
        public float expires;
        public bool blocked;
    }

    private struct HeapEntry
    {
        public int index;
        public float cost;
    }

    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
    };

    private static readonly Dictionary<int, BlockedEntry> blockedCache = new Dictionary<int, BlockedEntry>();
    private static readonly List<HeapEntry> heap = new List<HeapEntry>();

    private readonly float[] cost = new float[Size * Size];
    private Vector2Int origin;

    public bool IsBuilt { get; private set; }
    public float BuiltAt { get; private set; }
    public float LastRequestedAt { get; set; }
    public Vector2Int GoalCell => origin;

    public static Vector2Int ToCell(Vector2 world) => Vector2Int.RoundToInt(world / CellSize);

    // Recomputes costs outward from the goal over every walkable cell inside the window.
    public void Rebuild(Vector2 goalWorld)
    {
        origin = ToCell(goalWorld);
        for (int i = 0; i < cost.Length; i++)
            cost[i] = Unreachable;

        heap.Clear();
        int goalIndex = Index(origin);
        cost[goalIndex] = 0f;
        Push(goalIndex, 0f);

        while (heap.Count > 0)
        {
            HeapEntry entry = Pop();
            if (entry.cost > cost[entry.index])
                continue;

            Vector2Int cell = FromIndex(entry.index);
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbor = cell + direction;
                if (!InWindow(neighbor) || IsBlocked(neighbor))
                    continue;

                bool diagonal = direction.x != 0 && direction.y != 0;
                if (diagonal && (IsBlocked(cell + new Vector2Int(direction.x, 0)) || IsBlocked(cell + new Vector2Int(0, direction.y))))
                    continue;

                float newCost = entry.cost + (diagonal ? 1.4142f : 1f);
                int neighborIndex = Index(neighbor);
                if (newCost >= cost[neighborIndex])
                    continue;

                cost[neighborIndex] = newCost;
                Push(neighborIndex, newCost);
            }
        }

        IsBuilt = true;
        BuiltAt = Time.time;
    }

    // Downhill direction at a world position; false when it lies outside the window or the goal is unreachable.
    public bool TryGetDirection(Vector2 world, out Vector2 direction)
    {
        direction = Vector2.zero;
        Vector2Int cell = ToCell(world);
        if (!InWindow(cell))
            return false;

        float here = cost[Index(cell)];
        if (float.IsInfinity(here))
            here = LowestNeighborCost(cell) + 1f;
        if (float.IsInfinity(here))
            return false;

        foreach (Vector2Int step in Directions)
        {
            Vector2Int neighbor = cell + step;
            if (!InWindow(neighbor))
                continue;

            float neighborCost = cost[Index(neighbor)];
            if (float.IsInfinity(neighborCost) || neighborCost >= here)
                continue;

            direction += ((Vector2)step).normalized * ((here - neighborCost) / step.magnitude);
        }

        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();
        return true;
    }

    private float LowestNeighborCost(Vector2Int cell)
    {
        float lowest = Unreachable;
        foreach (Vector2Int step in Directions)
        {
            Vector2Int neighbor = cell + step;
            if (InWindow(neighbor))
                lowest = Mathf.Min(lowest, cost[Index(neighbor)]);
        }
        return lowest;
    }

    private static bool IsBlocked(Vector2Int cell)
    {
        int key = (cell.x & 0xFFFF) << 16 | (cell.y & 0xFFFF);
        float now = Time.time;
        if (blockedCache.TryGetValue(key, out BlockedEntry entry) && entry.expires > now)
            return entry.blocked;

        if (blockedCache.Count > 60000)
            blockedCache.Clear();

        bool blocked = ObstacleQuery.IsAreaBlocked((Vector2)cell * CellSize, CellSize);
        blockedCache[key] = new BlockedEntry { expires = now + Random.Range(6f, 10f), blocked = blocked };
        return blocked;
    }

    private bool InWindow(Vector2Int cell)
    {
        return Mathf.Abs(cell.x - origin.x) <= Radius && Mathf.Abs(cell.y - origin.y) <= Radius;
    }

    private int Index(Vector2Int cell) => (cell.y - origin.y + Radius) * Size + (cell.x - origin.x + Radius);

    private Vector2Int FromIndex(int index) => new Vector2Int(index % Size - Radius + origin.x, index / Size - Radius + origin.y);

    private static void Push(int index, float entryCost)
    {
        heap.Add(new HeapEntry { index = index, cost = entryCost });
        int i = heap.Count - 1;
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (heap[parent].cost <= heap[i].cost)
                break;
            (heap[parent], heap[i]) = (heap[i], heap[parent]);
            i = parent;
        }
    }

    private static HeapEntry Pop()
    {
        HeapEntry top = heap[0];
        int last = heap.Count - 1;
        heap[0] = heap[last];
        heap.RemoveAt(last);

        int i = 0;
        while (true)
        {
            int left = i * 2 + 1;
            int right = left + 1;
            int smallest = i;
            if (left < heap.Count && heap[left].cost < heap[smallest].cost)
                smallest = left;
            if (right < heap.Count && heap[right].cost < heap[smallest].cost)
                smallest = right;
            if (smallest == i)
                break;
            (heap[smallest], heap[i]) = (heap[i], heap[smallest]);
            i = smallest;
        }
        return top;
    }
}
