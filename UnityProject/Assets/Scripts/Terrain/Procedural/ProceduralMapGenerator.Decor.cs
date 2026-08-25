using System.Collections.Generic;
using UnityEngine;

public partial class ProceduralMapGenerator
{
    /// <summary>Scatters configured decor prefabs (trees, rocks, ...) across a chunk.</summary>
    private void GenerateDecor(Vector2Int chunk, int originX, int originY, bool[,] waterMask, int maskOriginX, int maskOriginY)
    {
        if (decorEntries == null || decorEntries.Length == 0)
            return;

        int entryCount = decorEntries.Length;
        float[] entryOffsetX = new float[entryCount];
        float[] entryOffsetY = new float[entryCount];
        for (int i = 0; i < entryCount; i++)
        {
            // Deterministic per-entry noise offset, independent of array order/count changing at
            // design time - derived from the seed and the entry's index rather than a sequential
            // RNG draw in Awake.
            System.Random entryRandom = new(seed ^ (i * -1640531527)); // -1640531527 = 0x9E3779B9 as int32
            entryOffsetX[i] = entryRandom.Next(-100000, 100000);
            entryOffsetY[i] = entryRandom.Next(-100000, 100000);
        }

        System.Random chunkRandom = GetChunkRandom(chunk);
        List<(Vector3 position, GameObject prefab, int worldX)> toSpawn = new();

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int worldX = originX + x;
                int worldY = originY + y;

                if (Vector2Int.Distance(new Vector2Int(worldX, worldY), spawnCell) <= spawnSafeRadius)
                    continue;

                Vector3Int cell = new(worldX, worldY, 0);
                bool isWater = MaskIsWater(waterMask, maskOriginX, maskOriginY, worldX, worldY);

                GameObject prefab = PickDecorPrefab(chunkRandom, worldX, worldY, isWater, entryOffsetX, entryOffsetY);
                if (prefab == null)
                    continue;

                toSpawn.Add((landTilemap.GetCellCenterWorld(cell), prefab, worldX));
            }
        }

        if (toSpawn.Count == 0)
            return;

        List<(GameObject instance, GameObject prefab)> spawned = new(toSpawn.Count);

        foreach ((Vector3 position, GameObject prefab, int worldX) in toSpawn)
        {
            GameObject instance = RentDecorInstance(prefab);
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            instance.SetActive(true);

            // Two decor items on the same grid row get the exact same Y-based sortingOrder, which
            // leaves their draw order to an unstable tie-break that can flicker frame to frame
            // (most visible with animated sprites). Nudge by a small, deterministic, X-derived
            // offset - just enough to break an exact integer tie (sortingOrder only needs a
            // nonzero difference) without meaningfully shifting where short decor (e.g. bushes)
            // crosses in front of/behind the player, since that threshold is this offset wide.
            // A Set (not Add) since this instance may be reused from the pool with a stale offset.
            SortingLayer_Auto sortScript = instance.GetComponent<SortingLayer_Auto>();
            if (sortScript != null)
            {
                int tiebreak = ((worldX % 5) + 5) % 5 - 2;
                sortScript.SetSortingOffset(tiebreak);
            }

            spawned.Add((instance, prefab));
        }

        decorObjects[chunk] = spawned;
    }

    /// <summary>Gets a pooled, inactive instance of this prefab if one is available, otherwise instantiates
    /// a new one under its category folder (e.g. "Decor/Tree", "Decor/Rock") - assigned once at creation,
    /// since a pooled instance keeps the same parent for its whole life regardless of which chunk rents it.</summary>
    private GameObject RentDecorInstance(GameObject prefab)
    {
        if (decorPool.TryGetValue(prefab, out Stack<GameObject> pool) && pool.Count > 0)
            return pool.Pop();

        return Instantiate(prefab, GetDecorCategoryParent(prefab));
    }

    /// <summary>Gets (creating if needed) the "Decor/{category}" transform for a prefab, where category is
    /// its name with any trailing variant digits stripped (e.g. "Tree1"/"Tree4" -> "Tree").</summary>
    private Transform GetDecorCategoryParent(GameObject prefab)
    {
        if (decorParent == null)
        {
            GameObject decorParentObject = new("Decor");
            decorParentObject.transform.SetParent(transform, false);
            decorParent = decorParentObject.transform;
        }

        string category = System.Text.RegularExpressions.Regex.Replace(prefab.name, @"\d+$", "");
        if (!decorCategoryParents.TryGetValue(category, out Transform categoryParent))
        {
            GameObject categoryObject = new(category);
            categoryObject.transform.SetParent(decorParent, false);
            categoryParent = categoryObject.transform;
            decorCategoryParents[category] = categoryParent;
        }

        return categoryParent;
    }

    /// <summary>Picks the first decor entry (in inspector order) that matches this cell's surface and noise/density roll.</summary>
    private GameObject PickDecorPrefab(
        System.Random chunkRandom, int worldX, int worldY, bool isWater, float[] entryOffsetX, float[] entryOffsetY)
    {
        for (int i = 0; i < decorEntries.Length; i++)
        {
            DecorEntry entry = decorEntries[i];
            if (entry == null || entry.prefab == null)
                continue;

            bool wantsWater = entry.surface == DecorSurface.Water;
            if (wantsWater != isWater)
                continue;

            float noise = Mathf.PerlinNoise(
                (worldX + entryOffsetX[i]) * entry.noiseScale,
                (worldY + entryOffsetY[i]) * entry.noiseScale);

            if (noise <= entry.noiseThreshold)
                continue;

            if (chunkRandom.NextDouble() >= entry.density)
                continue;

            return entry.prefab;
        }

        return null;
    }
}
