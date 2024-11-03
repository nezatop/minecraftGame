using UnityEngine;

namespace MultiCraft.Scripts.Game.World
{
    public class Chunk
    {
        public Vector3Int Position;
        
        public ChunkRenderer Renderer;
        public BlockType[,,] Blocks; 
    }
}