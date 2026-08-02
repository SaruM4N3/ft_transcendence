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
}
