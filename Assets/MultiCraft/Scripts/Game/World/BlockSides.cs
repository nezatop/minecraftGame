using UnityEngine;

namespace MultiCraft.Scripts.Game.World
{
    [CreateAssetMenu(menuName = "MultiCraft/World/Blocks/Sides Block")]
    public class BlockSides : Block
    {
        public Vector2Int pixelsOffsetLeft;
        public Vector2Int pixelsOffsetRight;
        public Vector2Int pixelsOffsetFront;
        public Vector2Int pixelsOffsetBack;
        public Vector2Int pixelsOffsetBottom;

        public override Vector2Int GetPixelsOffset(Vector3Int normal)
        {
            if (normal == Vector3Int.left) return pixelsOffsetLeft;
            if (normal == Vector3Int.right) return pixelsOffsetRight;
            if (normal == Vector3Int.forward) return pixelsOffsetFront;
            if (normal == Vector3Int.back) return pixelsOffsetBack;
            if (normal == Vector3Int.down) return pixelsOffsetBottom;

            return pixelsOffset;
        }
    }
}