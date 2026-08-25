using System.Collections.Generic;

public partial class ProceduralMapGenerator
{
    /// <summary>Checks a horizontal stretch is water-free.</summary>
    private bool IsGroundedStretch(int platformOriginX, int wallRowY, int start, int width)
    {
        for (int i = -1; i <= width; i++)
        {
            if (IsWater(platformOriginX + start + i, wallRowY - 1))
                return false;
        }

        return true;
    }

    /// <summary>Checks a vertical column is water-free.</summary>
    private bool IsGroundedColumn(int worldX, int platformOriginY, int start, int width)
    {
        for (int i = -1; i <= width; i++)
        {
            if (IsWater(worldX, platformOriginY + start + i))
                return false;
        }

        return true;
    }

    /// <summary>Computes the south floor edge.</summary>
    private int[] ComputeSouthEdge(bool[,] floorMask, int platformWidth, int platformHeight)
    {
        int[] edge = new int[platformWidth];

        for (int lx = 0; lx < platformWidth; lx++)
        {
            edge[lx] = -1;
            for (int ly = 0; ly < platformHeight; ly++)
            {
                if (floorMask[lx, ly])
                {
                    edge[lx] = ly;
                    break;
                }
            }
        }

        return edge;
    }

    /// <summary>Computes the west floor edge.</summary>
    private int[] ComputeWestEdge(bool[,] floorMask, int platformWidth, int platformHeight)
    {
        int[] edge = new int[platformHeight];

        for (int ly = 0; ly < platformHeight; ly++)
        {
            edge[ly] = -1;
            for (int lx = 0; lx < platformWidth; lx++)
            {
                if (floorMask[lx, ly])
                {
                    edge[ly] = lx;
                    break;
                }
            }
        }

        return edge;
    }

    /// <summary>Computes the east floor edge.</summary>
    private int[] ComputeEastEdge(bool[,] floorMask, int platformWidth, int platformHeight)
    {
        int[] edge = new int[platformHeight];

        for (int ly = 0; ly < platformHeight; ly++)
        {
            edge[ly] = -1;
            for (int lx = platformWidth - 1; lx >= 0; lx--)
            {
                if (floorMask[lx, ly])
                {
                    edge[ly] = lx;
                    break;
                }
            }
        }

        return edge;
    }

    /// <summary>Finds flat runs in a silhouette.</summary>
    private List<(int start, int width, int coord)> FindFlatRuns(int[] edge)
    {
        List<(int start, int width, int coord)> runs = new();
        int runStart = -1;
        int runCoord = -1;

        for (int i = 0; i <= edge.Length; i++)
        {
            bool sameAsRun = i < edge.Length && runStart >= 0 && edge[i] == runCoord;

            if (!sameAsRun && runStart >= 0)
            {
                runs.Add((runStart, i - runStart, runCoord));
                runStart = -1;
                runCoord = -1;
            }

            if (i < edge.Length && edge[i] != -1 && runStart < 0)
            {
                runStart = i;
                runCoord = edge[i];
            }
        }

        return runs;
    }

    /// <summary>Finds an edge stairs gap.</summary>
    private StairsGap? FindEdgeStairsGap(
        System.Random chunkRandom, int platformOriginX, int platformOriginY, int platformHeight,
        List<(int start, int width, int coord)> westRuns, List<(int start, int width, int coord)> eastRuns,
        List<StairsGap> existingGaps, bool westAvailable, bool eastAvailable)
    {
        if (edgeStairsPrefab == null)
            return null;

        if (!westAvailable && !eastAvailable)
            return null;

        if (!eastAvailable)
            return FindEdgeStairsGapOnSide(chunkRandom, platformOriginX, platformOriginY, platformHeight, GapSide.West, westRuns, existingGaps);
        if (!westAvailable)
            return FindEdgeStairsGapOnSide(chunkRandom, platformOriginX, platformOriginY, platformHeight, GapSide.East, eastRuns, existingGaps);

        bool tryWestFirst = chunkRandom.Next(0, 2) == 0;

        if (tryWestFirst)
        {
            StairsGap? gap = FindEdgeStairsGapOnSide(chunkRandom, platformOriginX, platformOriginY, platformHeight, GapSide.West, westRuns, existingGaps);
            if (gap != null)
                return gap;
            return FindEdgeStairsGapOnSide(chunkRandom, platformOriginX, platformOriginY, platformHeight, GapSide.East, eastRuns, existingGaps);
        }
        else
        {
            StairsGap? gap = FindEdgeStairsGapOnSide(chunkRandom, platformOriginX, platformOriginY, platformHeight, GapSide.East, eastRuns, existingGaps);
            if (gap != null)
                return gap;
            return FindEdgeStairsGapOnSide(chunkRandom, platformOriginX, platformOriginY, platformHeight, GapSide.West, westRuns, existingGaps);
        }
    }

    /// <summary>Finds an edge stairs gap on one side.</summary>
    private StairsGap? FindEdgeStairsGapOnSide(
        System.Random chunkRandom, int platformOriginX, int platformOriginY, int platformHeight, GapSide side,
        List<(int start, int width, int coord)> runs, List<StairsGap> existingGaps)
    {
        List<(int start, int column)> candidates = new();

        foreach (var run in runs)
        {
            if (run.width < edgeStairsPrefabHeight)
                continue;

            int worldX;
            int extraWorldX;
            if (side == GapSide.West)
            {
                worldX = platformOriginX + run.coord - 1;
                extraWorldX = worldX - 1;
            }
            else
            {
                worldX = platformOriginX + run.coord + 1;
                extraWorldX = worldX + 1;
            }

            for (int offset = 0; offset <= run.width - edgeStairsWidth; offset++)
            {
                int candidateStart = run.start + offset;

                if (candidateStart == 0 || candidateStart + edgeStairsPrefabHeight >= platformHeight)
                    continue;

                if (!IsGroundedColumn(worldX, platformOriginY, candidateStart, edgeStairsWidth))
                    continue;

                if (!IsGroundedColumn(extraWorldX, platformOriginY, candidateStart, edgeStairsWidth))
                    continue;

                if (OverlapsExisting(existingGaps, side, run.coord, candidateStart, edgeStairsWidth))
                    continue;

                candidates.Add((candidateStart, run.coord));
            }
        }

        if (candidates.Count == 0)
            return null;

        var chosen = candidates[chunkRandom.Next(candidates.Count)];

        return new StairsGap
        {
            Start = chosen.start,
            Width = edgeStairsWidth,
            IsCorner = false,
            Flip = side == GapSide.East,
            Side = side,
            EdgeLocal = chosen.column,
        };
    }

    /// <summary>Finds corner stairs gaps.</summary>
    private List<StairsGap> FindCornerStairsGaps(
        int platformOriginX, int platformOriginY, int platformWidth, int platformHeight,
        bool[,] floorMask, List<(int start, int width, int coord)> southRuns)
    {
        List<StairsGap> corners = new();
        List<StairsGap> noGapsYet = new();

        foreach (var southRun in southRuns)
        {
            if (southRun.width < cornerStairsWidth)
                continue;

            int wallRowY = platformOriginY + southRun.coord - 1;
            int westStart = southRun.start;
            int eastStart = southRun.start + southRun.width - cornerStairsWidth;

            int westColumn = southRun.start;
            int eastColumn = southRun.start + southRun.width - 1;

            WallShape westShape = PickSouthWallShape(floorMask, platformWidth, platformHeight, westColumn, southRun.coord, noGapsYet);
            WallShape eastShape = PickSouthWallShape(floorMask, platformWidth, platformHeight, eastColumn, southRun.coord, noGapsYet);

            bool westNeighborClear = !WillPaintSouthWall(floorMask, platformWidth, platformHeight, westColumn - 1, southRun.coord + 1);
            bool eastNeighborClear = !WillPaintSouthWall(floorMask, platformWidth, platformHeight, eastColumn + 1, southRun.coord + 1);

            bool hasWestCorner = westShape == WallShape.Corner && westNeighborClear;
            bool hasEastCorner = eastShape == WallShape.CornerReversed && eastNeighborClear;

            if (westStart == eastStart)
                hasEastCorner = false;

            if (hasWestCorner && IsGroundedStretch(platformOriginX, wallRowY, westStart, cornerStairsWidth))
            {
                corners.Add(new StairsGap
                {
                    Start = westStart,
                    Width = cornerStairsWidth,
                    IsCorner = true,
                    Flip = false,
                    Side = GapSide.South,
                    EdgeLocal = southRun.coord,
                });
            }

            if (hasEastCorner && IsGroundedStretch(platformOriginX, wallRowY, eastStart, cornerStairsWidth))
            {
                corners.Add(new StairsGap
                {
                    Start = eastStart,
                    Width = cornerStairsWidth,
                    IsCorner = true,
                    Flip = true,
                    Side = GapSide.South,
                    EdgeLocal = southRun.coord,
                });
            }
        }

        return corners;
    }

    /// <summary>Checks a candidate gap overlaps an existing one.</summary>
    private bool OverlapsExisting(List<StairsGap> existingGaps, GapSide side, int edgeLocal, int candidateStart, int width)
    {
        foreach (StairsGap existing in existingGaps)
        {
            if (existing.Side != side || existing.EdgeLocal != edgeLocal)
                continue;

            if (candidateStart < existing.Start + existing.Width + 1 && candidateStart + width + 1 > existing.Start)
                return true;
        }

        return false;
    }
}
