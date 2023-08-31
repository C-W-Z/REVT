using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class FriendlyRuleTile : RuleTile<FriendlyRuleTile.Neighbor> {
    public bool customField;

    public TileBase[] friendTiles;

    public class Neighbor : RuleTile.TilingRule.Neighbor {
        public const int Null = 3;
        public const int NotNull = 4;
        public const int ThisOrFriend = 5;
        public const int NotThisAndNotFriend = 6;
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        switch (neighbor) {
            case Neighbor.Null: return tile == null;
            case Neighbor.NotNull: return tile != null;
            case Neighbor.ThisOrFriend: return tile == this || HasFriendTile(tile);
            case Neighbor.NotThisAndNotFriend: return tile != this && !HasFriendTile(tile);
        }
        return base.RuleMatch(neighbor, tile);
    }

    bool HasFriendTile(TileBase tile)
        {
            if (tile == null)
                return false;
 
            for (int i = 0; i < friendTiles.Length; i++)
            {
                if (friendTiles[i] == tile)
                    return true;
            }
            return false;
        }
}