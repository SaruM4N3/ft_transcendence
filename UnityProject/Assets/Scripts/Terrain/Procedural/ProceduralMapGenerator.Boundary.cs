using System.Collections.Generic;
using UnityEngine;

public partial class ProceduralMapGenerator
{
    /// <summary>Creates platform edge colliders.</summary>
    private void CreatePlatformBoundary(
        int platformOriginX, int platformOriginY, int platformWidth, int platformHeight,
        bool[,] floorMask, List<StairsGap> stairsGaps, Transform parent)
    {
        GameObject boundary = new("PlatformBoundary");
        boundary.transform.SetParent(parent, false);

        GameObject boundaryGround = new("BoundaryGround") { layer = groundBoundaryLayer };
        boundaryGround.transform.SetParent(boundary.transform, false);

        GameObject boundaryPlatform = new("BoundaryPlatform") { layer = platformBoundaryLayer };
        boundaryPlatform.transform.SetParent(boundary.transform, false);

        AddWestEastBoundary(boundary, boundaryGround, boundaryPlatform, platformOriginX, platformOriginY, platformWidth, platformHeight, floorMask, GapSide.West, stairsGaps);
        AddWestEastBoundary(boundary, boundaryGround, boundaryPlatform, platformOriginX, platformOriginY, platformWidth, platformHeight, floorMask, GapSide.East, stairsGaps);

        AddHorizontalBoundary(boundaryGround, platformOriginX, platformOriginY, platformWidth, platformHeight, floorMask, 1, -1);
        AddHorizontalBoundary(boundaryPlatform, platformOriginX, platformOriginY, platformWidth, platformHeight, floorMask, 1, 0);
    }

    /// <summary>Adds west/east edge colliders, splitting north-corner cells onto the ground/platform layers.</summary>
    private void AddWestEastBoundary(
        GameObject boundary, GameObject boundaryGround, GameObject boundaryPlatform,
        int platformOriginX, int platformOriginY, int platformWidth, int platformHeight,
        bool[,] floorMask, GapSide side, List<StairsGap> stairsGaps)
    {
        int dx;
        int faceOffset;

        if (side == GapSide.West)
        {
            dx = -1;
            faceOffset = 0;
        }
        else
        {
            dx = 1;
            faceOffset = 1;
        }

        for (int lx = 0; lx < platformWidth; lx++)
        {
            int neighborLx = lx + dx;
            bool boundaryColumn = neighborLx < 0 || neighborLx >= platformWidth;
            int worldX = platformOriginX + lx + faceOffset;

            int runStart = -1;

            for (int ly = 0; ly <= platformHeight; ly++)
            {
                bool exposed = ly < platformHeight
                    && floorMask[lx, ly]
                    && (boundaryColumn || !floorMask[neighborLx, ly])
                    && !FindGap(side, lx, ly, stairsGaps).HasValue;

                bool isNorthCell = exposed && (ly + 1 >= platformHeight || !floorMask[lx, ly + 1]);

                if (exposed && !isNorthCell && runStart < 0)
                    runStart = ly;

                if ((!exposed || isNorthCell) && runStart >= 0)
                {
                    int fromY = platformOriginY + runStart;
                    int toY = platformOriginY + ly;
                    AddBoundarySegment(boundary, worldX, fromY, worldX, toY);
                    runStart = -1;
                }

                if (isNorthCell)
                {
                    int fromY = platformOriginY + ly;
                    int toY = platformOriginY + ly + 1;
                    AddBoundarySegment(boundaryGround, worldX, fromY - 1, worldX, toY - 1);
                    AddBoundarySegment(boundaryPlatform, worldX, fromY, worldX, toY);
                }
            }
        }
    }

    /// <summary>Adds north/south edge colliders.</summary>
    private void AddHorizontalBoundary(
        GameObject boundary, int platformOriginX, int platformOriginY, int platformWidth, int platformHeight,
        bool[,] floorMask, int dy, int yShift = 0)
    {
        int faceOffset;
        if (dy > 0)
            faceOffset = 1;
        else
            faceOffset = 0;

        for (int ly = 0; ly < platformHeight; ly++)
        {
            int neighborLy = ly + dy;
            bool boundaryRow = neighborLy < 0 || neighborLy >= platformHeight;
            int worldY = platformOriginY + ly + faceOffset + yShift;

            int runStart = -1;

            for (int lx = 0; lx <= platformWidth; lx++)
            {
                bool exposed = lx < platformWidth
                    && floorMask[lx, ly]
                    && (boundaryRow || !floorMask[lx, neighborLy]);

                if (exposed && runStart < 0)
                    runStart = lx;

                if (!exposed && runStart >= 0)
                {
                    int fromX = platformOriginX + runStart;
                    int toX = platformOriginX + lx;
                    AddBoundarySegment(boundary, fromX, worldY, toX, worldY);
                    runStart = -1;
                }
            }
        }
    }

    /// <summary>Adds one edge collider segment.</summary>
    private void AddBoundarySegment(GameObject boundary, int fromX, int fromY, int toX, int toY)
    {
        Vector3 worldA = platformTilemap.CellToWorld(new Vector3Int(fromX, fromY, 0));
        Vector3 worldB = platformTilemap.CellToWorld(new Vector3Int(toX, toY, 0));

        EdgeCollider2D edge = boundary.AddComponent<EdgeCollider2D>();
        edge.points = new Vector2[]
        {
            boundary.transform.InverseTransformPoint(worldA),
            boundary.transform.InverseTransformPoint(worldB),
        };
    }
}
