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
    [SerializeField] private int viewDistanceInChunks = 2;
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
    // Types
    // ---------------------------------------------------------------------
    private enum GapSide { South, West, East }

    private enum WallShape { Normal, Corner, CornerReversed, Pillar }

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
    private readonly Dictionary<Vector2Int, GameObject> structurePlatforms = new();
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
    }

    void Update()
    {
        Vector2Int playerChunk = WorldToChunk(player.position);

        if (hasGeneratedOnce && playerChunk == lastPlayerChunk)
            return;

        lastPlayerChunk = playerChunk;
        hasGeneratedOnce = true;
        UpdateChunks(playerChunk);
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

    /// <summary>Loads/unloads chunks around the player.</summary>
    private void UpdateChunks(Vector2Int centerChunk)
    {
        HashSet<Vector2Int> chunksInRange = new();
        for (int y = -viewDistanceInChunks; y <= viewDistanceInChunks; y++)
        {
            for (int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
            {
                chunksInRange.Add(centerChunk + new Vector2Int(x, y));
            }
        }

        foreach (Vector2Int chunk in chunksInRange)
        {
            if (!loadedChunks.Contains(chunk))
                GenerateChunk(chunk);
        }

        loadedChunks.RemoveWhere(chunk =>
        {
            if (chunksInRange.Contains(chunk))
                return false;

            UnloadChunk(chunk);
            return true;
        });

        loadedChunks.UnionWith(chunksInRange);
    }

    /// <summary>Generates one chunk.</summary>
    private void GenerateChunk(Vector2Int chunk)
    {
        int originX = chunk.x * chunkSize;
        int originY = chunk.y * chunkSize;

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int worldX = originX + x;
                int worldY = originY + y;
                Vector3Int cell = new(worldX, worldY, 0);
                bool isWater = IsWater(worldX, worldY);

                if (isWater)
                {
                    waterTilemap.SetTile(cell, waterTile);
                    landTilemap.SetTile(cell, null);
                }
                else
                {
                    waterTilemap.SetTile(cell, null);
                    landTilemap.SetTile(cell, PickLandTile(worldX, worldY));
                }

                bool isCoastalGround = !isWater && IsAdjacentToWater(worldX, worldY);

                if (isCoastalGround)
                {
                    waterBackgroundTilemap.SetTile(cell, waterBackgroundTile);
                    coastFoamTilemap.SetTile(cell, coastFoamTile);
                }
                else
                {
                    waterBackgroundTilemap.SetTile(cell, null);
                    coastFoamTilemap.SetTile(cell, null);
                }
            }
        }

        TryGenerateStructure(chunk, originX, originY);
    }

    /// <summary>Unloads one chunk.</summary>
    private void UnloadChunk(Vector2Int chunk)
    {
        int originX = chunk.x * chunkSize;
        int originY = chunk.y * chunkSize;

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                Vector3Int cell = new(originX + x, originY + y, 0);
                landTilemap.SetTile(cell, null);
                waterTilemap.SetTile(cell, null);
                waterBackgroundTilemap.SetTile(cell, null);
                coastFoamTilemap.SetTile(cell, null);
                platformTilemap.SetTile(cell, null);
                wallTilemap.SetTile(cell, null);
                shadowTilemap.SetTile(cell, null);
            }
        }

        if (structurePlatforms.TryGetValue(chunk, out GameObject platform))
        {
            Destroy(platform);
            structurePlatforms.Remove(chunk);
        }
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
