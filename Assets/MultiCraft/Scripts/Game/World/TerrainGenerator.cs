using Unity.Mathematics;
using UnityEngine;

namespace MultiCraft.Scripts.Game.World
{
    public static class TerrainGenerator
    {
        public static BlockType[,,] GenerateTerrain(int xOffset, int zOffset)
        {
            BlockType[,,] result = new BlockType[ChunkRenderer.ChunkWidth, ChunkRenderer.ChunkHeight, ChunkRenderer.ChunkWidth];
            for (int x = 0; x < ChunkRenderer.ChunkWidth; x++)
            {
                for (int z = 0; z < ChunkRenderer.ChunkWidth; z++)
                {
                    float height = Mathf.PerlinNoise((x + xOffset) * 0.25f, (z + zOffset) * 0.25f) * 5 + 10;

                    for (int y = 0; y < height; y++)
                    {
                        result[x, y, z] = BlockType.Grass;
                    }
                }
            }
            return result;
        }
    }
}