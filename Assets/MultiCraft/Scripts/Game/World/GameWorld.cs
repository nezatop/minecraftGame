using System.Collections.Generic;
using UnityEngine;

namespace MultiCraft.Scripts.Game.World
{
    public class GameWorld: MonoBehaviour
    {
        public Dictionary<Vector2Int, ChunkData> ChunkDatas = new Dictionary<Vector2Int, ChunkData>();
        public ChunkRenderer ChunkPrefab;
        void Start()
        {
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    int xPos = x * ChunkRenderer.ChunkWidth;
                    int yPos = y * ChunkRenderer.ChunkWidth;
                    
                    ChunkData chunkData = new ChunkData();
                    chunkData.ChunkPosition = new Vector2Int(x, y);
                    chunkData.Blocks =
                        TerrainGenerator.GenerateTerrain(xPos, yPos);
                    ChunkDatas.Add(new Vector2Int(x, y), chunkData);
                    
                    var chunk = Instantiate(ChunkPrefab, new Vector3(xPos, 0, yPos), Quaternion.identity, transform);
                    chunk.ChunkData = chunkData;
                    chunk.ParentWorld = this;
                }
            }
        }
    }
}