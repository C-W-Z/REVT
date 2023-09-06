using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class FriendlyRuleTile : RuleTile<FriendlyRuleTile.Neighbor> {
    public bool customField;
    public TileBase[] friendTiles;

    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        public const int Null = 3;
        public const int NotNull = 4;
        public const int ThisOrFriend = 5;
        public const int NotThisAndNotFriend = 6;
    }

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        return neighbor switch
        {
            Neighbor.Null                => tile == null,
            Neighbor.NotNull             => tile != null,
            Neighbor.ThisOrFriend        => tile == this || HasFriendTile(tile),
            Neighbor.NotThisAndNotFriend => tile != this && !HasFriendTile(tile),
            _ => base.RuleMatch(neighbor, tile),
        };
    }

    bool HasFriendTile(TileBase tile)
    {
        if (tile == null)
            return false;
        for (int i = 0; i < friendTiles.Length; i++)
            if (friendTiles[i] == tile)
                return true;
        return false;
    }
}