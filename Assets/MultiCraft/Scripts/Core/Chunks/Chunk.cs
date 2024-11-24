using UnityEngine;

namespace MultiCraft.Scripts.Core.Chunks
{
    public class Chunk
    {
        public Vector3Int Position = Vector3Int.zero;
        public ChunkRenderer Renderer;
        
        public int[,,] Blocks;
        public ChunkState State = ChunkState.Unloaded;

        public Chunk LeftChunk = null;
        public Chunk RightChunk = null;
        public Chunk UpChunk = null;
        public Chunk DownChunk = null;
        public Chunk FrontChunk = null;
        public Chunk BackChunk = null;
    }
}