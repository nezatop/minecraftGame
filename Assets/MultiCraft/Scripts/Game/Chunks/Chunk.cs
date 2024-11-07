using MultiCraft.Scripts.Game.Blocks;
using UnityEngine;

namespace MultiCraft.Scripts.Game.Chunks
{
    public class Chunk
    {
        public Vector3Int Position;
        public BlockType[,,] Blocks;
        public ChunkRenderer Renderer;
        public ChunkState State = ChunkState.Unloaded;
        
        public Chunk LeftChunk;
        public Chunk RightChunk;
        public Chunk UpChunk;
        public Chunk DownChunk;
        public Chunk FrontChunk;
        public Chunk BackChunk;
    }
}