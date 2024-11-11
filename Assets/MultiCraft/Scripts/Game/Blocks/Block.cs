using UnityEngine;

namespace MultiCraft.Scripts.Game.Blocks
{
    [System.Serializable]
    public class Block
    {
        public BlockType Type = BlockType.Air;
        public int Hardness = -1;
        public int Durability = -1;

        public int UvsPixelsOffsetLeftX = 0;
        public int UvsPixelsOffsetLeftY = 0;
        public int UvsPixelsOffsetRightX = 0;
        public int UvsPixelsOffsetRightY = 0;
        public int UvsPixelsOffsetFrontX = 0;
        public int UvsPixelsOffsetFrontY = 0;
        public int UvsPixelsOffsetBackX = 0;
        public int UvsPixelsOffsetBackY = 0;
        public int UvsPixelsOffsetBottomX = 0;
        public int UvsPixelsOffsetBottomY = 0;
        public int UvsPixelsOffsetTopX = 0;
        public int UvsPixelsOffsetTopY = 0;

        public Vector2Int GetUvsPixelsOffset(Vector3Int normal)
        {
            if (normal == Vector3Int.left) return new Vector2Int(UvsPixelsOffsetLeftX, UvsPixelsOffsetLeftY);
            if (normal == Vector3Int.right) return new Vector2Int(UvsPixelsOffsetRightX, UvsPixelsOffsetRightY);
            if (normal == Vector3Int.forward) return new Vector2Int(UvsPixelsOffsetFrontX, UvsPixelsOffsetFrontY);
            if (normal == Vector3Int.back) return new Vector2Int(UvsPixelsOffsetBackX, UvsPixelsOffsetBackY);
            if (normal == Vector3Int.down) return new Vector2Int(UvsPixelsOffsetBottomX, UvsPixelsOffsetBottomY);
            return new Vector2Int(UvsPixelsOffsetTopX, UvsPixelsOffsetTopY);
        }
    }
}