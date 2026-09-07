using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public partial class ProceduralMapGenerator : MonoBehaviour
{
    // ---------------------------------------------------------------------
    // References
    // ---------------------------------------------------------------------
    [Header("References")]
    [SerializeField] private Tilemap landTilemap;
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private Tilemap waterBackgroundTilemap;
    [SerializeField] private Tilemap coastFoamTilemap;
    [FormerlySerializedAs("floorTilemap")]
    [SerializeField] private Tilemap platformTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap shadowTilemap;
    [SerializeField] private Transform player;

    // ---------------------------------------------------------------------
    // Chunk streaming
    // ---------------------------------------------------------------------
    [Header("Chunk Streaming")]
    [SerializeField] private int chunkSize = 16;
    [Tooltip("Extra chunks of buffer loaded beyond what the main camera actually shows, to avoid pop-in at the screen edges. The real load radius is computed every chunk update from the camera's current view plus this buffer.")]
    [SerializeField] private int viewDistanceInChunks = 1;
    [Tooltip("Max newly-needed chunks generated per frame once the initial area around the player is loaded (the very first load is never throttled). Keeps a burst of new chunks - e.g. crossing a chunk corner diagonally - from spiking a single frame; extra chunks stream in over the next few frames instead.")]
    [SerializeField] private int maxChunkGenerationsPerFrame = 2;
    [SerializeField] private int seed;
    [SerializeField] private float spawnSafeRadius = 5f;

    // ---------------------------------------------------------------------
    // Biome & water
    // ---------------------------------------------------------------------
    [Header("Biome & Water")]
    [SerializeField] private float biomeNoiseScale = 0.05f;
    [SerializeField] private TileBase[] biomeTiles;
    [SerializeField] private float waterNoiseScale = 0.1f;
    [SerializeField] private float waterThreshold = 0.35f;
    [SerializeField] private TileBase waterTile;
    [SerializeField] private TileBase waterBackgroundTile;
    [SerializeField] private TileBase coastFoamTile;

    // ---------------------------------------------------------------------
    // Structures (platforms)
    // ---------------------------------------------------------------------
    [Header("Structures")]
    [Tooltip("Whether chunks can spawn elevated platform structures at all. Disabling this generates plain land/water terrain only.")]
    [SerializeField] private bool generatePlatforms = true;
    [SerializeField] [Range(0f, 1f)] private float structureChance = 0.15f;
    [SerializeField] private Vector2Int roomSizeRange = new(5, 15);
    [SerializeField] private float platformNoiseScale = 0.15f;
    [SerializeField] [Range(0f, 1f)] private float platformFillThreshold = 0.45f;
    [Tooltip("How strongly the noise is pushed toward empty as it nears the bounding box edge, keeping a margin so the blob never touches the chunk border.")]
    [SerializeField] private float platformEdgeFalloff = 2f;
    [Tooltip("Minimum connected floor tiles required after flood-fill, otherwise the structure is skipped for this chunk.")]
    [SerializeField] private int minPlatformFloorTiles = 20;
    [SerializeField] private TileBase interiorFloorTile;

    // ---------------------------------------------------------------------
    // Walls
    // ---------------------------------------------------------------------
    [Header("Walls")]
    [Tooltip("Indexed by WallShape (Normal, Corner, CornerReversed, Pillar).")]
    [SerializeField] private TileBase[] wallTilesByShape = new TileBase[4];
    [Tooltip("Water-adjacent equivalents of wallTilesByShape, indexed the same way, used when the wall cell steps into water.")]
    [SerializeField] private TileBase[] waterWallTilesByShape = new TileBase[4];
    [SerializeField] private TileBase shadowTile;

    // ---------------------------------------------------------------------
    // Stairs
    // ---------------------------------------------------------------------
    [Header("Stairs")]
    [Tooltip("Used when the stairs gap touches the west or east corner of the wall row.")]
    [SerializeField] private GameObject cornerStairsPrefab;
    [SerializeField] private int cornerStairsWidth = 2;
    [Tooltip("Used when the stairs gap sits on the west or east side of the platform instead of the south wall. Drawn facing west; mirrored on X for east placements.")]
    [SerializeField] private GameObject edgeStairsPrefab;
    [Tooltip("How many floor rows tall the side gap (the walkable opening) is.")]
    [SerializeField] private int edgeStairsWidth = 3;
    [Tooltip("The edge stairs prefab's own physical height in tiles. The west/east contour run must be at least this tall before an edge gap is placed there, so the prefab always sits flush against a wall segment as tall as itself.")]
    [SerializeField] private int edgeStairsPrefabHeight = 4;
    [Tooltip("The stairs prefabs' own physical width in tiles (both share the same art width), used to compensate their position when mirrored - on the east side for edge gaps, and on either side for corner gaps.")]
    [SerializeField] private int stairsPrefabWidth = 1;
    [SerializeField] private int minStairsPerPlatform = 2;

    // ---------------------------------------------------------------------
    // Decor
    // ---------------------------------------------------------------------
    [Header("Decor")]
    [SerializeField] private DecorEntry[] decorEntries;

    // ---------------------------------------------------------------------
    // Types
    // ---------------------------------------------------------------------
    private enum GapSide { South, West, East }

    private enum WallShape { Normal, Corner, CornerReversed, Pillar }

    private enum DecorSurface { Land, Water }

    /// <summary>One decor prefab's placement rules: where it can spawn, how dense, and how it clusters.</summary>
    [System.Serializable]
    private class DecorEntry
    {
        public GameObject prefab;
        [Tooltip("Land: only on dry tiles. Water: only on water tiles.")]
        public DecorSurface surface = DecorSurface.Land;
        [Range(0f, 1f)]
        [Tooltip("Final chance applied to cells that already pass the noise threshold below - the main 'how much of this spawns' knob.")]
        public float density = 0.05f;
        [Tooltip("Perlin noise scale used to cluster placement into natural-looking patches. Smaller = larger, smoother clusters.")]
        public float noiseScale = 0.2f;
        [Range(0f, 1f)]
        [Tooltip("How high the clustering noise must be for a cell to even be considered - higher = sparser/rarer patches.")]
        public float noiseThreshold = 0.75f;
    }

    /// <summary>A stairs gap.</summary>
    private struct StairsGap
    {
        public int Start;
        public int Width;
        public bool IsCorner;
        public bool Flip;
        public GapSide Side;
        public int EdgeLocal;
    }

    // ---------------------------------------------------------------------
    // Runtime state
    // ---------------------------------------------------------------------
    private readonly HashSet<Vector2Int> loadedChunks = new();
    private readonly HashSet<Vector2Int> pendingChunks = new();
    private readonly Queue<Vector2Int> pendingChunkQueue = new();
    private readonly HashSet<Vector2Int> desiredChunks = new();
    private readonly Dictionary<Vector2Int, GameObject> structurePlatforms = new();
    private readonly Dictionary<Vector2Int, List<(GameObject instance, GameObject prefab)>> decorObjects = new();
    private readonly Dictionary<GameObject, Stack<GameObject>> decorPool = new();
    private Transform decorParent;
    private readonly Dictionary<string, Transform> decorCategoryParents = new();
    private Vector2Int lastPlayerChunk;
    private bool hasGeneratedOnce;
    private bool playerOnPlatform;
    private int playerLayer;
    private int groundBoundaryLayer;
    private int platformBoundaryLayer;
    private float biomeOffsetX;
    private float biomeOffsetY;
    private float waterOffsetX;
    private float waterOffsetY;
    private float platformOffsetX;
    private float platformOffsetY;
    private Vector2Int spawnCell;
    private int effectiveViewDistanceInChunks;

    // Structure generation scratch buffers, sized to the max room footprint and reused across
    // chunks to avoid a burst of small heap allocations every time a platform is generated.
    private bool[,] structureMaskBuffer;
    private bool[,] structureVisitedBuffer;
    private bool[,] structureLargestBuffer;
    private bool[,] structureReachedBuffer;
    private readonly List<Vector2Int> floodFillScratch = new();
    private readonly List<Vector2Int> floodFillLargest = new();
    private readonly Queue<Vector2Int> floodFillQueue = new();
    private static readonly Vector2Int[] CardinalDirections =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    // ---------------------------------------------------------------------
    // Unity lifecycle
    // ---------------------------------------------------------------------

    /// <summary>Setup.</summary>
    void Awake()
    {
        System.Random rng = new(seed);
        biomeOffsetX = rng.Next(-100000, 100000);
        biomeOffsetY = rng.Next(-100000, 100000);
        waterOffsetX = rng.Next(-100000, 100000);
        waterOffsetY = rng.Next(-100000, 100000);
        platformOffsetX = rng.Next(-100000, 100000);
        platformOffsetY = rng.Next(-100000, 100000);

        Vector3Int cell = landTilemap.WorldToCell(player.position);
        spawnCell = new Vector2Int(cell.x, cell.y);

        playerLayer = player.gameObject.layer;
        groundBoundaryLayer = LayerMask.NameToLayer("Ground-Floor-0");
        platformBoundaryLayer = LayerMask.NameToLayer("Ground-Floor-1");
        ApplyPlayerPlatformCollision();

        int maxRoomSize = Mathf.Max(1, roomSizeRange.y);
        structureMaskBuffer = new bool[maxRoomSize, maxRoomSize];
        structureVisitedBuffer = new bool[maxRoomSize, maxRoomSize];
        structureLargestBuffer = new bool[maxRoomSize, maxRoomSize];
        structureReachedBuffer = new bool[maxRoomSize, maxRoomSize];
    }

    /// <summary>Computes the chunk load radius needed to cover the main camera's current view, plus
    /// the inspector buffer. Uses ViewportToWorldPoint against the tilemap's plane so it works for
    /// both orthographic and perspective cameras (this project drives the camera via Cinemachine with
    /// a perspective lens), and is recomputed every call so it tracks zoom/follow-lag correctly instead
    /// of caching a value from before Cinemachine has positioned the camera.</summary>
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

        // The initial area around the player loads in full immediately (no visible world otherwise);
        // afterwards, newly-needed chunks are throttled so a burst (e.g. a diagonal step revealing a
        // whole new corner of chunks at once) streams in over a few frames instead of spiking one.
        ProcessPendingChunks(firstRun ? int.MaxValue : maxChunkGenerationsPerFrame);
    }

    // ---------------------------------------------------------------------
    // Player/platform collision
    // ---------------------------------------------------------------------

    /// <summary>Player platform state changed.</summary>
    public void SetPlayerOnPlatform(bool onPlatform)
    {
        if (onPlatform == playerOnPlatform)
            return;

        playerOnPlatform = onPlatform;
        ApplyPlayerPlatformCollision();
    }

    /// <summary>Syncs player collision to platform state.</summary>
    private void ApplyPlayerPlatformCollision()
    {
        Physics2D.IgnoreLayerCollision(playerLayer, groundBoundaryLayer, playerOnPlatform);
        Physics2D.IgnoreLayerCollision(playerLayer, platformBoundaryLayer, !playerOnPlatform);
    }

    // ---------------------------------------------------------------------
    // Chunk streaming
    // ---------------------------------------------------------------------

    /// <summary>Recomputes which chunks are wanted, queues newly-needed ones, and unloads stale ones.</summary>
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

    /// <summary>Generates up to `budget` queued chunks. Requests for chunks the player has since
    /// moved away from (no longer in desiredChunks) are dropped instead of generated.</summary>
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

    /// <summary>Generates one chunk.</summary>
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

        TryGenerateStructure(chunk, originX, originY, waterMask, maskOriginX, maskOriginY);
        GenerateDecor(chunk, originX, originY, waterMask, maskOriginX, maskOriginY);
    }

    /// <summary>Unloads one chunk.</summary>
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
        platformTilemap.SetTilesBlock(bounds, clearTiles);
        wallTilemap.SetTilesBlock(bounds, clearTiles);
        shadowTilemap.SetTilesBlock(bounds, clearTiles);

        if (structurePlatforms.TryGetValue(chunk, out GameObject platform))
        {
            Destroy(platform);
            structurePlatforms.Remove(chunk);
        }

        if (decorObjects.TryGetValue(chunk, out List<(GameObject instance, GameObject prefab)> decorList))
        {
            foreach ((GameObject instance, GameObject prefab) in decorList)
                ReturnDecorToPool(instance, prefab);
            decorObjects.Remove(chunk);
        }
    }

    /// <summary>Deactivates a decor instance and returns it to its prefab's pool instead of destroying it.</summary>
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

    // ---------------------------------------------------------------------
    // Coordinate helpers
    // ---------------------------------------------------------------------

    /// <summary>World to chunk coordinates.</summary>
    private Vector2Int WorldToChunk(Vector3 worldPosition)
    {
        Vector3Int cell = landTilemap.WorldToCell(worldPosition);
        return new Vector2Int(
            Mathf.FloorToInt(cell.x / (float)chunkSize),
            Mathf.FloorToInt(cell.y / (float)chunkSize));
    }

    /// <summary>Deterministic per-chunk RNG.</summary>
    private System.Random GetChunkRandom(Vector2Int chunk)
    {
        int hash = seed ^ (chunk.x * 73856093) ^ (chunk.y * 19349663);
        return new System.Random(hash);
    }
}
