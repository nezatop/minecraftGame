using System;
using UnityEngine;

namespace MultiCraft.Scripts.Game.World
{
    public class TerrainGenerator : MonoBehaviour
    {
        public BlockType[,,] GenerateTerrain(int xOffset, int yOffset, int zOffset)
        {
            BlockType[,,] result = new BlockType[ChunkRenderer.ChunkWidth, ChunkRenderer.ChunkHeight,
                ChunkRenderer.ChunkWidth];
            result[0, 0, 0] = BlockType.Grass;
            for (int x = 0; x < ChunkRenderer.ChunkWidth; x++)
            {
                for (int z = 0; z < ChunkRenderer.ChunkWidth; z++)
                {
                    float height = Mathf.PerlinNoise((x + xOffset) * 0.25f, (z + zOffset) * 0.25f) * 2.2f + 10;

                    for (int y = 0; y < height - 4; y++)
                    {
                        result[x, y, z] = BlockType.Stone;
                    }

                    for (int y = Mathf.FloorToInt(height - 4); y < height - 1; y++)
                    {
                        result[x, y, z] = BlockType.Dirt;
                    }

                    result[x, Mathf.FloorToInt(height), z] = BlockType.Grass;
                }
            }

            return result;
        }
        private void GenerateSurface(out BlockType[,,] blocks)
        {
            blocks = new BlockType[,,] { };
        }
        
        private void GenerateCave(out BlockType[,,] blocks)
        {
            blocks = new BlockType[,,] { };
        }

        private void GenerateOre(out BlockType[,,] blocks)
        {
            blocks = new BlockType[,,] { };
        }
        
        private void GenerateWater(out BlockType[,,] blocks)
        {
            blocks = new BlockType[,,] { };
        }
    }
}