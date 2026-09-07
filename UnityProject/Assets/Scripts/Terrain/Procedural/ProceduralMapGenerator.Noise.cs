using UnityEngine;
using UnityEngine.Tilemaps;

public partial class ProceduralMapGenerator
{
    /// <summary>Is water.</summary>
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

    /// <summary>Is adjacent to water.</summary>
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

    /// <summary>Picks a land tile.</summary>
    private TileBase PickLandTile(int worldX, int worldY)
    {
        float biomeNoise = Mathf.PerlinNoise(
            (worldX + biomeOffsetX) * biomeNoiseScale,
            (worldY + biomeOffsetY) * biomeNoiseScale);

        int biomeIndex = Mathf.Clamp(Mathf.FloorToInt(biomeNoise * biomeTiles.Length), 0, biomeTiles.Length - 1);
        return biomeTiles[biomeIndex];
    }

    /// <summary>Platform noise.</summary>
    private float PlatformNoiseAt(int worldX, int worldY)
    {
        return Mathf.PerlinNoise(
            (worldX + platformOffsetX) * platformNoiseScale,
            (worldY + platformOffsetY) * platformNoiseScale);
    }

    /// <summary>Looks up water state from a precomputed chunk-local mask, falling back to a fresh
    /// noise sample if the requested cell falls outside the mask's coverage.</summary>
    private bool MaskIsWater(bool[,] waterMask, int maskOriginX, int maskOriginY, int worldX, int worldY)
    {
        int mx = worldX - maskOriginX;
        int my = worldY - maskOriginY;
        int size = waterMask.GetLength(0);

        if (mx < 0 || my < 0 || mx >= size || my >= size)
            return IsWater(worldX, worldY);

        return waterMask[mx, my];
    }

    /// <summary>Adjacent-to-water check backed by a precomputed chunk-local water mask.</summary>
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
