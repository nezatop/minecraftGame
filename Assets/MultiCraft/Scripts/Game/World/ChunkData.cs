using UnityEngine;

namespace MultiCraft.Scripts.Game.World
{
    public class ChunkData
    {
        public Vector2Int ChunkPosition;
        public ChunkRenderer Renderer;
        public BlockType[,,] Blocks;
    }
}