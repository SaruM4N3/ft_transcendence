using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class ProceduralMapGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap landTilemap;
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private Tilemap waterBackgroundTilemap;
    [SerializeField] private Tilemap coastFoamTilemap;
    [SerializeField] private Transform player;

    [Header("Chunk Streaming")]
    [SerializeField] private int chunkSize = 16;
    [Tooltip("Extra buffer chunks loaded beyond the camera's current view, to avoid pop-in at screen edges.")]
    [SerializeField] private int viewDistanceInChunks = 1;
    [Tooltip("Max newly-needed chunks generated per frame after the initial load, to spread bursts across frames.")]
    [SerializeField] private int maxChunkGenerationsPerFrame = 2;
    [SerializeField] private int seed;
    [SerializeField] private float spawnSafeRadius = 5f;

    [Header("Biome & Water")]
    [SerializeField] private float biomeNoiseScale = 0.05f;
    [SerializeField] private TileBase[] biomeTiles;
    [SerializeField] private float waterNoiseScale = 0.1f;
    [SerializeField] private float waterThreshold = 0.35f;
    [SerializeField] private TileBase waterTile;
    [SerializeField] private TileBase waterBackgroundTile;
    [SerializeField] private TileBase coastFoamTile;

    [Header("Decor")]
    [SerializeField] private DecorEntry[] decorEntries;

    private enum DecorSurface { Land, Water }

    [System.Serializable]
    private class DecorEntry
    {
        public GameObject prefab;
        [Tooltip("Land: only on dry tiles. Water: only on water tiles.")]
        public DecorSurface surface = DecorSurface.Land;
        [Range(0f, 1f)]
        [Tooltip("Chance applied to cells that pass the noise threshold - main density knob.")]
        public float density = 0.05f;
        [Tooltip("Perlin noise scale for clustering. Smaller = larger, smoother clusters.")]
        public float noiseScale = 0.2f;
        [Range(0f, 1f)]
        [Tooltip("Clustering noise cutoff - higher = sparser/rarer patches.")]
        public float noiseThreshold = 0.75f;
    }

    private readonly HashSet<Vector2Int> loadedChunks = new();
    private readonly HashSet<Vector2Int> pendingChunks = new();
    private readonly Queue<Vector2Int> pendingChunkQueue = new();
    private readonly HashSet<Vector2Int> desiredChunks = new();
    private readonly Dictionary<Vector2Int, List<(GameObject instance, GameObject prefab)>> decorObjects = new();
    private readonly Dictionary<GameObject, Stack<GameObject>> decorPool = new();
    private Transform decorParent;
    private readonly Dictionary<string, Transform> decorCategoryParents = new();
    private Vector2Int lastPlayerChunk;
    private bool hasGeneratedOnce;
    private float biomeOffsetX;
    private float biomeOffsetY;
    private float waterOffsetX;
    private float waterOffsetY;
    private Vector2Int spawnCell;
    private int effectiveViewDistanceInChunks;

    void Awake()
    {
        System.Random rng = new(seed);
        biomeOffsetX = rng.Next(-100000, 100000);
        biomeOffsetY = rng.Next(-100000, 100000);
        waterOffsetX = rng.Next(-100000, 100000);
        waterOffsetY = rng.Next(-100000, 100000);

        Vector3Int cell = landTilemap.WorldToCell(player.position);
        spawnCell = new Vector2Int(cell.x, cell.y);
    }

    // Chunk radius covering the camera's current view + buffer; recomputed every call to track Cinemachine zoom/lag.
    private int ComputeEffectiveViewDistance()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return viewDistanceInChunks;

        Vector3 toPlane = landTilemap.transform.position - cam.transform.position;
        float depth = Vector3.Dot(toPlane, cam.transform.forward);
        if (depth <= 0.01f)
            return viewDistanceInChunks;

        Vector3 worldMin = cam.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 worldMax = cam.ViewportToWorldPoint(new Vector3(1f, 1f, depth));

        float cellSize = landTilemap.cellSize.x > 0f ? landTilemap.cellSize.x : 1f;
        float chunkWorldSize = chunkSize * cellSize;

        float halfWidth = Mathf.Abs(worldMax.x - worldMin.x) * 0.5f;
        float halfHeight = Mathf.Abs(worldMax.y - worldMin.y) * 0.5f;

        int chunksNeededX = Mathf.CeilToInt(halfWidth / chunkWorldSize);
        int chunksNeededY = Mathf.CeilToInt(halfHeight / chunkWorldSize);
        int cameraChunkRadius = Mathf.Max(chunksNeededX, chunksNeededY);

        return cameraChunkRadius + viewDistanceInChunks;
    }

    void Update()
    {
        Vector2Int playerChunk = WorldToChunk(player.position);
        bool firstRun = !hasGeneratedOnce;

        if (firstRun || playerChunk != lastPlayerChunk)
        {
            lastPlayerChunk = playerChunk;
            hasGeneratedOnce = true;
            UpdateChunks(playerChunk);
        }

        // First load is unthrottled (no visible world otherwise); later bursts stream over a few frames.
        ProcessPendingChunks(firstRun ? int.MaxValue : maxChunkGenerationsPerFrame);
    }

    private void UpdateChunks(Vector2Int centerChunk)
    {
        effectiveViewDistanceInChunks = ComputeEffectiveViewDistance();

        desiredChunks.Clear();
        for (int y = -effectiveViewDistanceInChunks; y <= effectiveViewDistanceInChunks; y++)
        {
            for (int x = -effectiveViewDistanceInChunks; x <= effectiveViewDistanceInChunks; x++)
            {
                desiredChunks.Add(centerChunk + new Vector2Int(x, y));
            }
        }

        foreach (Vector2Int chunk in desiredChunks)
        {
            if (!loadedChunks.Contains(chunk) && pendingChunks.Add(chunk))
                pendingChunkQueue.Enqueue(chunk);
        }

        loadedChunks.RemoveWhere(chunk =>
        {
            if (desiredChunks.Contains(chunk))
                return false;

            UnloadChunk(chunk);
            return true;
        });
    }

    // Generates up to `budget` queued chunks; drops requests for chunks no longer in desiredChunks.
    private void ProcessPendingChunks(int budget)
    {
        while (budget > 0 && pendingChunkQueue.Count > 0)
        {
            Vector2Int chunk = pendingChunkQueue.Dequeue();
            pendingChunks.Remove(chunk);

            if (!desiredChunks.Contains(chunk))
                continue;

            GenerateChunk(chunk);
            loadedChunks.Add(chunk);
            budget--;
        }
    }

    private void GenerateChunk(Vector2Int chunk)
    {
        int originX = chunk.x * chunkSize;
        int originY = chunk.y * chunkSize;
        int maskOriginX = originX - 1;
        int maskOriginY = originY - 1;

        // Sample water noise once per cell (with a 1-cell border for adjacency checks) instead of
        // re-sampling it up to 8x per cell via IsAdjacentToWater below.
        int maskSize = chunkSize + 2;
        bool[,] waterMask = new bool[maskSize, maskSize];
        for (int y = 0; y < maskSize; y++)
        {
            for (int x = 0; x < maskSize; x++)
            {
                waterMask[x, y] = IsWater(maskOriginX + x, maskOriginY + y);
            }
        }

        int cellCount = chunkSize * chunkSize;
        TileBase[] landTiles = new TileBase[cellCount];
        TileBase[] waterTiles = new TileBase[cellCount];
        TileBase[] waterBackgroundTiles = new TileBase[cellCount];
        TileBase[] coastFoamTiles = new TileBase[cellCount];

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int worldX = originX + x;
                int worldY = originY + y;
                int index = x + y * chunkSize;

                bool isWater = MaskIsWater(waterMask, maskOriginX, maskOriginY, worldX, worldY);

                if (isWater)
                {
                    waterTiles[index] = waterTile;
                    landTiles[index] = null;
                }
                else
                {
                    waterTiles[index] = null;
                    landTiles[index] = PickLandTile(worldX, worldY);
                }

                bool isCoastalGround = !isWater && MaskIsAdjacentToWater(waterMask, maskOriginX, maskOriginY, worldX, worldY);

                if (isCoastalGround)
                {
                    waterBackgroundTiles[index] = waterBackgroundTile;
                    coastFoamTiles[index] = coastFoamTile;
                }
            }
        }

        BoundsInt bounds = new(originX, originY, 0, chunkSize, chunkSize, 1);
        landTilemap.SetTilesBlock(bounds, landTiles);
        waterTilemap.SetTilesBlock(bounds, waterTiles);
        waterBackgroundTilemap.SetTilesBlock(bounds, waterBackgroundTiles);
        coastFoamTilemap.SetTilesBlock(bounds, coastFoamTiles);

        GenerateDecor(chunk, originX, originY, waterMask, maskOriginX, maskOriginY);
    }

    private void UnloadChunk(Vector2Int chunk)
    {
        int originX = chunk.x * chunkSize;
        int originY = chunk.y * chunkSize;
        BoundsInt bounds = new(originX, originY, 0, chunkSize, chunkSize, 1);
        TileBase[] clearTiles = new TileBase[chunkSize * chunkSize];

        landTilemap.SetTilesBlock(bounds, clearTiles);
        waterTilemap.SetTilesBlock(bounds, clearTiles);
        waterBackgroundTilemap.SetTilesBlock(bounds, clearTiles);
        coastFoamTilemap.SetTilesBlock(bounds, clearTiles);

        if (decorObjects.TryGetValue(chunk, out List<(GameObject instance, GameObject prefab)> decorList))
        {
            foreach ((GameObject instance, GameObject prefab) in decorList)
                ReturnDecorToPool(instance, prefab);
            decorObjects.Remove(chunk);
        }
    }

    // Pooled, not destroyed - chunks reload often enough that reuse matters.
    private void ReturnDecorToPool(GameObject instance, GameObject prefab)
    {
        instance.SetActive(false);

        if (!decorPool.TryGetValue(prefab, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>();
            decorPool[prefab] = pool;
        }

        pool.Push(instance);
    }

    private Vector2Int WorldToChunk(Vector3 worldPosition)
    {
        Vector3Int cell = landTilemap.WorldToCell(worldPosition);
        return new Vector2Int(
            Mathf.FloorToInt(cell.x / (float)chunkSize),
            Mathf.FloorToInt(cell.y / (float)chunkSize));
    }

    // Seeded from chunk coords, so the same chunk always regenerates identically.
    private System.Random GetChunkRandom(Vector2Int chunk)
    {
        int hash = seed ^ (chunk.x * 73856093) ^ (chunk.y * 19349663);
        return new System.Random(hash);
    }
}
