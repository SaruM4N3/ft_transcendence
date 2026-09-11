using System.Collections.Generic;
using UnityEngine;

public partial class ProceduralMapGenerator
{
    private void GenerateDecor(Vector2Int chunk, int originX, int originY, bool[,] waterMask, int maskOriginX, int maskOriginY)
    {
        if (decorEntries == null || decorEntries.Length == 0)
            return;

        int entryCount = decorEntries.Length;
        float[] entryOffsetX = new float[entryCount];
        float[] entryOffsetY = new float[entryCount];
        for (int i = 0; i < entryCount; i++)
        {
            // Derived from seed+index (not a sequential RNG draw) so offsets stay stable if entries
            // are reordered/added later.
            System.Random entryRandom = new(seed ^ (i * -1640531527)); // 0x9E3779B9 as int32
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

            // Same-row decor shares a Y-based sortingOrder, which flickers on an unstable tie-break -
            // nudge by a small X-derived offset to break the tie. Set, not Add: a pooled instance may
            // carry a stale offset from its previous use.
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

    // New instances are parented under their category folder ("Decor/Tree", ...) once at creation and
    // keep that parent for life, even when reused by a different chunk later.
    private GameObject RentDecorInstance(GameObject prefab)
    {
        if (decorPool.TryGetValue(prefab, out Stack<GameObject> pool) && pool.Count > 0)
            return pool.Pop();

        return Instantiate(prefab, GetDecorCategoryParent(prefab));
    }

    // Category = prefab name with trailing variant digits stripped ("Tree1"/"Tree4" -> "Tree").
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

    // First entry (inspector order) that matches this cell's surface and noise/density roll.
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
