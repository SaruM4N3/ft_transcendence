using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class ProceduralMapGenerator
{
    /// <summary>Tries to generate a structure.</summary>
    private void TryGenerateStructure(
        Vector2Int chunk, int originX, int originY, bool[,] waterMask, int maskOriginX, int maskOriginY)
    {
        if (!generatePlatforms)
            return;

        System.Random chunkRandom = GetChunkRandom(chunk);

        if (chunkRandom.NextDouble() > structureChance)
            return;

        int platformWidth = chunkRandom.Next(roomSizeRange.x, roomSizeRange.y + 1);
        int platformHeight = chunkRandom.Next(roomSizeRange.x, roomSizeRange.y + 1);

        int maxOffsetX = Mathf.Max(0, chunkSize - platformWidth);
        int maxOffsetY = Mathf.Max(0, chunkSize - platformHeight);
        int platformOriginX = originX + chunkRandom.Next(0, maxOffsetX + 1);
        int minOffsetY = Mathf.Min(1, maxOffsetY);
        int platformOriginY = originY + chunkRandom.Next(minOffsetY, maxOffsetY + 1);

        bool[,] rawMask = GenerateFloorMask(platformOriginX, platformOriginY, platformWidth, platformHeight);
        bool[,] largestMask = KeepLargestComponent(rawMask, platformWidth, platformHeight, out int floorCount);

        if (floorCount < minPlatformFloorTiles)
            return;

        bool[,] floorMask = FillInteriorHoles(largestMask, platformWidth, platformHeight);

        List<StairsGap> stairsGaps = PlanStairsGaps(
            chunkRandom, platformOriginX, platformOriginY, platformWidth, platformHeight, floorMask);

        PaintPlatformFloor(
            platformOriginX, platformOriginY, platformWidth, platformHeight, floorMask,
            waterMask, maskOriginX, maskOriginY);

        GameObject platformRoot = new("Platform");
        platformRoot.transform.SetParent(platformTilemap.transform, false);
        structurePlatforms[chunk] = platformRoot;

        CreatePlatformBoundary(
            platformOriginX, platformOriginY, platformWidth, platformHeight, floorMask, stairsGaps, platformRoot.transform);

        PaintWalls(platformOriginX, platformOriginY, platformWidth, platformHeight, floorMask, stairsGaps);

        SpawnStairsPrefabs(platformOriginX, platformOriginY, stairsGaps, platformRoot.transform);
    }

    /// <summary>Clears the active [0, width) x [0, height) region of a reused scratch buffer.</summary>
    private static void ClearBufferRegion(bool[,] buffer, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buffer[x, y] = false;
            }
        }
    }

    /// <summary>Generates the floor mask.</summary>
    private bool[,] GenerateFloorMask(int platformOriginX, int platformOriginY, int platformWidth, int platformHeight)
    {
        // Fully overwritten below, cell by cell, so no clearing needed before reuse.
        bool[,] mask = structureMaskBuffer;

        for (int ly = 0; ly < platformHeight; ly++)
        {
            for (int lx = 0; lx < platformWidth; lx++)
            {
                int worldX = platformOriginX + lx;
                int worldY = platformOriginY + ly;

                float noise = PlatformNoiseAt(worldX, worldY);

                int edgeDist = Mathf.Min(Mathf.Min(lx, platformWidth - 1 - lx), Mathf.Min(ly, platformHeight - 1 - ly));
                float falloff;
                if (platformEdgeFalloff > 0f)
                    falloff = Mathf.Clamp01(edgeDist / platformEdgeFalloff);
                else
                    falloff = 1f;

                mask[lx, ly] = noise * falloff > platformFillThreshold;
            }
        }

        return mask;
    }

    /// <summary>Keeps the largest floor component.</summary>
    private bool[,] KeepLargestComponent(bool[,] mask, int platformWidth, int platformHeight, out int floorCount)
    {
        bool[,] visited = structureVisitedBuffer;
        ClearBufferRegion(visited, platformWidth, platformHeight);

        floodFillLargest.Clear();
        bool hasLargest = false;

        for (int ly = 0; ly < platformHeight; ly++)
        {
            for (int lx = 0; lx < platformWidth; lx++)
            {
                if (!mask[lx, ly] || visited[lx, ly])
                    continue;

                List<Vector2Int> component = FloodFillComponent(mask, visited, platformWidth, platformHeight, lx, ly);
                if (!hasLargest || component.Count > floodFillLargest.Count)
                {
                    floodFillLargest.Clear();
                    floodFillLargest.AddRange(component);
                    hasLargest = true;
                }
            }
        }

        bool[,] result = structureLargestBuffer;
        ClearBufferRegion(result, platformWidth, platformHeight);
        if (hasLargest)
        {
            foreach (Vector2Int cell in floodFillLargest)
                result[cell.x, cell.y] = true;
        }

        floorCount = hasLargest ? floodFillLargest.Count : 0;

        return result;
    }

    /// <summary>Fills interior holes.</summary>
    private bool[,] FillInteriorHoles(bool[,] mask, int width, int height)
    {
        bool[,] reachedFromOutside = structureReachedBuffer;
        ClearBufferRegion(reachedFromOutside, width, height);
        floodFillQueue.Clear();

        for (int x = 0; x < width; x++)
        {
            EnqueueIfEmpty(mask, reachedFromOutside, floodFillQueue, x, 0);
            EnqueueIfEmpty(mask, reachedFromOutside, floodFillQueue, x, height - 1);
        }
        for (int y = 0; y < height; y++)
        {
            EnqueueIfEmpty(mask, reachedFromOutside, floodFillQueue, 0, y);
            EnqueueIfEmpty(mask, reachedFromOutside, floodFillQueue, width - 1, y);
        }

        while (floodFillQueue.Count > 0)
        {
            Vector2Int cell = floodFillQueue.Dequeue();

            foreach (Vector2Int dir in CardinalDirections)
            {
                Vector2Int next = cell + dir;
                if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
                    continue;

                EnqueueIfEmpty(mask, reachedFromOutside, floodFillQueue, next.x, next.y);
            }
        }

        // `mask` is `structureLargestBuffer` at this point in the pipeline, so `structureMaskBuffer`
        // (the raw mask, already consumed by KeepLargestComponent) is safe to reuse as the result.
        bool[,] result = structureMaskBuffer;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = mask[x, y] || !reachedFromOutside[x, y];
            }
        }

        return result;
    }

    /// <summary>Enqueues an empty cell.</summary>
    private void EnqueueIfEmpty(bool[,] mask, bool[,] reachedFromOutside, Queue<Vector2Int> queue, int x, int y)
    {
        if (mask[x, y] || reachedFromOutside[x, y])
            return;

        reachedFromOutside[x, y] = true;
        queue.Enqueue(new Vector2Int(x, y));
    }

    /// <summary>Flood-fills a component.</summary>
    private List<Vector2Int> FloodFillComponent(bool[,] mask, bool[,] visited, int width, int height, int startX, int startY)
    {
        floodFillScratch.Clear();
        floodFillQueue.Clear();
        floodFillQueue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        while (floodFillQueue.Count > 0)
        {
            Vector2Int cell = floodFillQueue.Dequeue();
            floodFillScratch.Add(cell);

            foreach (Vector2Int dir in CardinalDirections)
            {
                Vector2Int next = cell + dir;
                if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
                    continue;
                if (visited[next.x, next.y] || !mask[next.x, next.y])
                    continue;

                visited[next.x, next.y] = true;
                floodFillQueue.Enqueue(next);
            }
        }

        return floodFillScratch;
    }

    /// <summary>Paints the platform floor.</summary>
    private void PaintPlatformFloor(
        int platformOriginX, int platformOriginY, int platformWidth, int platformHeight, bool[,] floorMask,
        bool[,] waterMask, int maskOriginX, int maskOriginY)
    {
        int cellCount = platformWidth * platformHeight;
        TileBase[] platformTiles = new TileBase[cellCount];
        TileBase[] landTiles = new TileBase[cellCount];
        TileBase[] waterTiles = new TileBase[cellCount];
        TileBase[] waterBackgroundTiles = new TileBase[cellCount];
        TileBase[] coastFoamTiles = new TileBase[cellCount];

        for (int ly = 0; ly < platformHeight; ly++)
        {
            for (int lx = 0; lx < platformWidth; lx++)
            {
                int index = lx + ly * platformWidth;
                int floorWorldX = platformOriginX + lx;
                int floorWorldY = platformOriginY + ly;

                bool isWater = MaskIsWater(waterMask, maskOriginX, maskOriginY, floorWorldX, floorWorldY);

                if (!floorMask[lx, ly])
                {
                    // Not part of the platform footprint: re-derive exactly what GenerateChunk
                    // already painted here (same mask, same formula) instead of leaving it
                    // untouched, since the whole region is written back in one SetTilesBlock call.
                    waterTiles[index] = isWater ? waterTile : null;
                    landTiles[index] = isWater ? null : PickLandTile(floorWorldX, floorWorldY);

                    bool baseCoastal = !isWater && MaskIsAdjacentToWater(waterMask, maskOriginX, maskOriginY, floorWorldX, floorWorldY);
                    waterBackgroundTiles[index] = baseCoastal ? waterBackgroundTile : null;
                    coastFoamTiles[index] = baseCoastal ? coastFoamTile : null;
                    continue;
                }

                platformTiles[index] = interiorFloorTile;
                landTiles[index] = isWater ? null : PickLandTile(floorWorldX, floorWorldY);
                waterTiles[index] = null;

                bool isCoastalFloor = MaskIsAdjacentToWater(waterMask, maskOriginX, maskOriginY, floorWorldX, floorWorldY);
                waterBackgroundTiles[index] = isCoastalFloor ? waterBackgroundTile : null;
                coastFoamTiles[index] = isCoastalFloor ? coastFoamTile : null;
            }
        }

        BoundsInt bounds = new(platformOriginX, platformOriginY, 0, platformWidth, platformHeight, 1);
        platformTilemap.SetTilesBlock(bounds, platformTiles);
        landTilemap.SetTilesBlock(bounds, landTiles);
        waterTilemap.SetTilesBlock(bounds, waterTiles);
        waterBackgroundTilemap.SetTilesBlock(bounds, waterBackgroundTiles);
        coastFoamTilemap.SetTilesBlock(bounds, coastFoamTiles);
    }

    /// <summary>Paints the south walls.</summary>
    private void PaintWalls(
        int platformOriginX, int platformOriginY, int platformWidth, int platformHeight,
        bool[,] floorMask, List<StairsGap> stairsGaps)
    {
        for (int ly = 0; ly < platformHeight; ly++)
        {
            for (int lx = 0; lx < platformWidth; lx++)
            {
                if (!floorMask[lx, ly])
                    continue;

                bool southExposed = ly == 0 || !floorMask[lx, ly - 1];
                if (!southExposed)
                    continue;

                int worldX = platformOriginX + lx;
                int worldY = platformOriginY + ly;

                WallShape shape = PickSouthWallShape(floorMask, platformWidth, platformHeight, lx, ly, stairsGaps);
                PaintSideWallCell(GapSide.South, lx, ly, worldX, worldY - 1, 0, -1, stairsGaps, shape);
            }
        }
    }

    /// <summary>Picks the south wall shape.</summary>
    private WallShape PickSouthWallShape(bool[,] floorMask, int platformWidth, int platformHeight, int lx, int ly, List<StairsGap> stairsGaps)
    {
        bool leftOpen = IsWallSideOpen(floorMask, platformWidth, platformHeight, lx - 1, ly, stairsGaps);
        bool rightOpen = IsWallSideOpen(floorMask, platformWidth, platformHeight, lx + 1, ly, stairsGaps);

        if (leftOpen && rightOpen)
            return WallShape.Pillar;
        if (leftOpen)
            return WallShape.Corner;
        if (rightOpen)
            return WallShape.CornerReversed;

        return WallShape.Normal;
    }

    /// <summary>Checks if a wall side is open.</summary>
    private bool IsWallSideOpen(bool[,] floorMask, int platformWidth, int platformHeight, int neighborLx, int ly, List<StairsGap> stairsGaps)
    {
        if (neighborLx < 0 || neighborLx >= platformWidth)
            return true;

        int wallRowLy = ly - 1;
        if (wallRowLy >= 0 && wallRowLy < platformHeight && floorMask[neighborLx, wallRowLy])
            return false;

        if (WillPaintSouthWall(floorMask, platformWidth, platformHeight, neighborLx, ly)
            && !FindGap(GapSide.South, neighborLx, ly, stairsGaps).HasValue)
            return false;

        return true;
    }

    /// <summary>Checks if a south wall will be painted.</summary>
    private bool WillPaintSouthWall(bool[,] floorMask, int platformWidth, int platformHeight, int lx, int ly)
    {
        if (lx < 0 || lx >= platformWidth || ly < 0 || ly >= platformHeight)
            return false;
        if (!floorMask[lx, ly])
            return false;

        return ly == 0 || !floorMask[lx, ly - 1];
    }

    /// <summary>Paints or clears a wall cell.</summary>
    private void PaintSideWallCell(
        GapSide side, int lx, int ly, int wallWorldX, int wallWorldY, int dx, int dy, List<StairsGap> stairsGaps,
        WallShape shape = WallShape.Normal)
    {
        if (FindGap(side, lx, ly, stairsGaps).HasValue)
        {
            Vector3Int wallCell = new(wallWorldX, wallWorldY, 0);
            wallTilemap.SetTile(wallCell, null);
            landTilemap.SetTile(wallCell, PickLandTile(wallWorldX, wallWorldY));
            waterTilemap.SetTile(wallCell, null);
            shadowTilemap.SetTile(wallCell, null);
            waterBackgroundTilemap.SetTile(wallCell, null);
            coastFoamTilemap.SetTile(wallCell, null);
            return;
        }

        PaintWallCell(wallWorldX, wallWorldY, dx, dy, shape);
    }

    /// <summary>Paints a wall cell.</summary>
    private void PaintWallCell(int wallWorldX, int wallWorldY, int dx, int dy, WallShape shape = WallShape.Normal)
    {
        Vector3Int wallCell = new(wallWorldX, wallWorldY, 0);

        bool stepsIntoWater = IsWater(wallWorldX + dx, wallWorldY + dy);
        TileBase tileToUse = ResolveWallTile(shape, stepsIntoWater);
        wallTilemap.SetTile(wallCell, tileToUse);

        if (stepsIntoWater)
        {
            waterBackgroundTilemap.SetTile(wallCell, waterBackgroundTile);
            coastFoamTilemap.SetTile(wallCell, coastFoamTile);
            shadowTilemap.SetTile(wallCell, null);
        }
        else
        {
            waterBackgroundTilemap.SetTile(wallCell, null);
            coastFoamTilemap.SetTile(wallCell, null);
            shadowTilemap.SetTile(wallCell, shadowTile);
        }
    }

    /// <summary>Resolves the wall tile.</summary>
    private TileBase ResolveWallTile(WallShape shape, bool stepsIntoWater)
    {
        if (stepsIntoWater)
            return waterWallTilesByShape[(int)shape];

        return wallTilesByShape[(int)shape];
    }

    /// <summary>Finds a stairs gap.</summary>
    private StairsGap? FindGap(GapSide side, int lx, int ly, List<StairsGap> stairsGaps)
    {
        foreach (StairsGap gap in stairsGaps)
        {
            if (gap.IsCorner)
            {
                if (gap.EdgeLocal != ly)
                    continue;
                if (side == GapSide.West && !gap.Flip && lx == gap.Start)
                    return gap;
                if (side == GapSide.East && gap.Flip && lx == gap.Start + gap.Width - 1)
                    return gap;

                continue;
            }

            if (gap.Side != side)
                continue;

            if (side == GapSide.South)
            {
                if (gap.EdgeLocal != ly)
                    continue;

                int local = lx - gap.Start;
                if (local >= 0 && local < gap.Width)
                    return gap;
            }
            else
            {
                if (gap.EdgeLocal != lx)
                    continue;

                int local = ly - gap.Start;
                if (local >= 0 && local < gap.Width)
                    return gap;
            }
        }

        return null;
    }

    /// <summary>Plans the stairs gaps.</summary>
    private List<StairsGap> PlanStairsGaps(
        System.Random chunkRandom, int platformOriginX, int platformOriginY, int platformWidth, int platformHeight,
        bool[,] floorMask)
    {
        List<StairsGap> stairsGaps = new();

        int[] southEdge = ComputeSouthEdge(floorMask, platformWidth, platformHeight);
        int[] westEdge = ComputeWestEdge(floorMask, platformWidth, platformHeight);
        int[] eastEdge = ComputeEastEdge(floorMask, platformWidth, platformHeight);

        List<(int start, int width, int coord)> southRuns = FindFlatRuns(southEdge);
        List<(int start, int width, int coord)> westRuns = FindFlatRuns(westEdge);
        List<(int start, int width, int coord)> eastRuns = FindFlatRuns(eastEdge);

        List<StairsGap> cornerCandidates;
        if (cornerStairsPrefab != null)
            cornerCandidates = FindCornerStairsGaps(platformOriginX, platformOriginY, platformWidth, platformHeight, floorMask, southRuns);
        else
            cornerCandidates = new List<StairsGap>();

        int stairsPlaced = 0;
        int attempts = 0;
        int maxAttempts = minStairsPerPlatform * 6;
        bool westUsed = false;
        bool eastUsed = false;

        while (stairsPlaced < minStairsPerPlatform && attempts < maxAttempts)
        {
            attempts++;

            StairsGap? gap;
            if (chunkRandom.Next(0, 2) == 0)
                gap = TakeRandomCornerGap(chunkRandom, cornerCandidates, stairsGaps, !westUsed, !eastUsed);
            else
                gap = FindEdgeStairsGap(
                    chunkRandom, platformOriginX, platformOriginY, platformHeight, westRuns, eastRuns, stairsGaps,
                    !westUsed, !eastUsed);

            if (gap == null)
                continue;

            if (gap.Value.Flip)
                eastUsed = true;
            else
                westUsed = true;

            stairsGaps.Add(gap.Value);
            stairsPlaced++;
        }

        return stairsGaps;
    }

    /// <summary>Picks a random corner gap.</summary>
    private StairsGap? TakeRandomCornerGap(
        System.Random chunkRandom, List<StairsGap> cornerCandidates, List<StairsGap> existingGaps,
        bool westAvailable, bool eastAvailable)
    {
        List<StairsGap> available = new();
        foreach (StairsGap candidate in cornerCandidates)
        {
            bool sideAvailable;
            if (candidate.Flip)
                sideAvailable = eastAvailable;
            else
                sideAvailable = westAvailable;

            if (!sideAvailable)
                continue;
            if (!OverlapsExisting(existingGaps, candidate.Side, candidate.EdgeLocal, candidate.Start, candidate.Width))
                available.Add(candidate);
        }

        if (available.Count == 0)
            return null;

        return available[chunkRandom.Next(available.Count)];
    }

    /// <summary>Spawns the stairs prefabs.</summary>
    private void SpawnStairsPrefabs(int platformOriginX, int platformOriginY, List<StairsGap> stairsGaps, Transform parent)
    {
        foreach (StairsGap gap in stairsGaps)
        {
            GameObject prefabToUse;
            Vector3Int anchorCell;
            int flipShift;

            if (gap.IsCorner)
            {
                prefabToUse = cornerStairsPrefab;
                int worldX;
                if (gap.Flip)
                    worldX = platformOriginX + gap.Start + gap.Width;
                else
                    worldX = platformOriginX + gap.Start - 1;
                anchorCell = new Vector3Int(worldX, platformOriginY + gap.EdgeLocal - 1, 0);
                flipShift = stairsPrefabWidth;
            }
            else if (gap.Side == GapSide.West)
            {
                prefabToUse = edgeStairsPrefab;
                anchorCell = new Vector3Int(platformOriginX + gap.EdgeLocal - 1, platformOriginY + gap.Start, 0);
                flipShift = stairsPrefabWidth;
            }
            else
            {
                prefabToUse = edgeStairsPrefab;
                anchorCell = new Vector3Int(platformOriginX + gap.EdgeLocal + 1, platformOriginY + gap.Start, 0);
                flipShift = stairsPrefabWidth;
            }

            Vector3 stairsPosition = platformTilemap.CellToWorld(anchorCell);

            if (gap.Flip)
                stairsPosition.x += flipShift;

            GameObject stairs = Instantiate(prefabToUse, stairsPosition, Quaternion.identity, parent);

            if (gap.Flip)
            {
                Vector3 scale = stairs.transform.localScale;
                scale.x *= -1;
                stairs.transform.localScale = scale;
            }

            AddStairsTriggers(gap, platformOriginX, platformOriginY, parent);
        }
    }

    /// <summary>Adds the stairs triggers.</summary>
    private void AddStairsTriggers(StairsGap gap, int platformOriginX, int platformOriginY, Transform parent)
    {
        Vector3Int platformCell;
        Vector3Int groundCell;
        Vector2 size;

        if (gap.IsCorner)
        {
            int platformCol;
            int groundCol;
            if (gap.Flip)
            {
                platformCol = gap.Start + gap.Width - 1;
                groundCol = gap.Start + gap.Width;
            }
            else
            {
                platformCol = gap.Start;
                groundCol = gap.Start - 1;
            }

            platformCell = new Vector3Int(platformOriginX + platformCol, platformOriginY + gap.EdgeLocal, 0);
            groundCell = new Vector3Int(platformOriginX + groundCol, platformOriginY + gap.EdgeLocal - 1, 0);
            size = Vector2.one;
        }
        else
        {
            int exteriorCol;
            if (gap.Side == GapSide.West)
                exteriorCol = gap.EdgeLocal - 1;
            else
                exteriorCol = gap.EdgeLocal + 1;

            platformCell = new Vector3Int(platformOriginX + gap.EdgeLocal, platformOriginY + gap.Start, 0);
            groundCell = new Vector3Int(platformOriginX + exteriorCol, platformOriginY + gap.Start, 0);
            size = new Vector2(1f, gap.Width);
        }

        Vector3 cellSize = platformTilemap.cellSize;
        Vector3 centerOffset = new(cellSize.x * size.x / 2f, cellSize.y * size.y / 2f, 0);
        Vector3 platformCenter = platformTilemap.CellToWorld(platformCell) + centerOffset;
        Vector3 groundCenter = platformTilemap.CellToWorld(groundCell) + centerOffset;

        CreateStairsTrigger(parent, platformCenter, size, true);
        CreateStairsTrigger(parent, groundCenter, size, false);
    }

    /// <summary>Creates a stairs trigger.</summary>
    private void CreateStairsTrigger(Transform parent, Vector3 center, Vector2 size, bool onPlatform)
    {
        string name;
        if (onPlatform)
            name = "PlatformTrigger";
        else
            name = "GroundTrigger";

        GameObject triggerObject = new(name);
        triggerObject.transform.SetParent(parent, false);
        triggerObject.transform.position = center;

        BoxCollider2D box = triggerObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;

        PlatformStairsTrigger trigger = triggerObject.AddComponent<PlatformStairsTrigger>();
        trigger.Initialize(this, onPlatform);
    }
}
