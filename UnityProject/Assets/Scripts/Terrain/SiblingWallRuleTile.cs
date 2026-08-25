using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>RuleTile that treats configured tiles as connected "sibling" neighbors.</summary>
[CreateAssetMenu(fileName = "New Sibling Wall Rule Tile", menuName = "Tiles/Sibling Wall Rule Tile")]
public class SiblingWallRuleTile : RuleTile<SiblingWallRuleTile.Neighbor>
{
    public class Neighbor : RuleTile.TilingRuleOutput.Neighbor
    {
        public const int Sibling = 3;
        public const int NotSibling = 4;
    }

    [Tooltip("Other wall tiles that should count as connected neighbors (e.g. the water variant of this wall).")]
    public List<TileBase> siblingTiles = new();

    /// <summary>Returns true when the neighbor matches this tile or its siblings.</summary>
    public override bool RuleMatch(int neighbor, TileBase other)
    {
        switch (neighbor)
        {
            case Neighbor.Sibling:
                return other == this || siblingTiles.Contains(other);
            case Neighbor.NotSibling:
                return other != this && !siblingTiles.Contains(other);
        }

        return base.RuleMatch(neighbor, other);
    }
}
