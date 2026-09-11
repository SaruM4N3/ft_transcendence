using UnityEngine;
using UnityEngine.Tilemaps;

public partial class ProceduralMapGenerator
{
    private bool IsWater(int worldX, int worldY)
    {
        bool withinSpawnSafeZone = Vector2Int.Distance(new Vector2Int(worldX, worldY), spawnCell) <= spawnSafeRadius;

        if (withinSpawnSafeZone)
            return false;

        float waterNoise = Mathf.PerlinNoise(
            (worldX + waterOffsetX) * waterNoiseScale,
            (worldY + waterOffsetY) * waterNoiseScale);

        return waterNoise < waterThreshold;
    }

    private bool IsAdjacentToWater(int worldX, int worldY)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if ((x != 0 || y != 0) && IsWater(worldX + x, worldY + y))
                    return true;
            }
        }

        return false;
    }

    private TileBase PickLandTile(int worldX, int worldY)
    {
        float biomeNoise = Mathf.PerlinNoise(
            (worldX + biomeOffsetX) * biomeNoiseScale,
            (worldY + biomeOffsetY) * biomeNoiseScale);

        int biomeIndex = Mathf.Clamp(Mathf.FloorToInt(biomeNoise * biomeTiles.Length), 0, biomeTiles.Length - 1);
        return biomeTiles[biomeIndex];
    }

    // Falls back to a fresh noise sample if the cell is outside the mask's precomputed coverage.
    private bool MaskIsWater(bool[,] waterMask, int maskOriginX, int maskOriginY, int worldX, int worldY)
    {
        int mx = worldX - maskOriginX;
        int my = worldY - maskOriginY;
        int size = waterMask.GetLength(0);

        if (mx < 0 || my < 0 || mx >= size || my >= size)
            return IsWater(worldX, worldY);

        return waterMask[mx, my];
    }

    private bool MaskIsAdjacentToWater(bool[,] waterMask, int maskOriginX, int maskOriginY, int worldX, int worldY)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if ((x != 0 || y != 0) && MaskIsWater(waterMask, maskOriginX, maskOriginY, worldX + x, worldY + y))
                    return true;
            }
        }

        return false;
    }
}
