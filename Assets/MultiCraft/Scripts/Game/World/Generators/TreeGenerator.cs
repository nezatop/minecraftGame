using MultiCraft.Scripts.Game.Blocks;
using MultiCraft.Scripts.Game.Chunks;
using UnityEngine;

namespace MultiCraft.Scripts.Game.World.Generators
{
    [CreateAssetMenu(fileName = "Tree Generator", menuName = "MultiCraft/Generators/Tree Generator")]
    public class TreeGenerator : ScriptableObject
    {
        public float TreeFrequency = 0.5f;

        public NoiseOctaveSetting[] Octaves;
        private FastNoiseLite[] _noise;

        public void InitializeNoise()
        {
            _noise = new FastNoiseLite[Octaves.Length];
            for (int i = 0; i < Octaves.Length; i++)
            {
                _noise[i] = new FastNoiseLite();
                _noise[i].SetNoiseType(Octaves[i].NoiseType);
                _noise[i].SetFrequency(Octaves[i].Frequency);
            }
        }

        public BlockType[,,] GenerateTree(BlockType[,,] blocks, int[,] surfaceHeight, int xOffset, int yOffset,
            int zOffset, int seed)
        {
            for (int x = 0; x < GameWorld.ChunkWidth; x++)
            {
                for (int z = 0; z < GameWorld.ChunkWidth; z++)
                {
                    int height = surfaceHeight[x, z];

                    float treeNoiseValue = GetTreeNoise(x + xOffset, z + zOffset);

                    if (treeNoiseValue > TreeFrequency && height > WorldGenerator.BaseHeight - 1)
                    {
                        int treeHeight = 7;
                        int foliageRadius = 3;

                        if (x + foliageRadius is >= 0 and < GameWorld.ChunkWidth &&
                            x - foliageRadius is >= 0 and < GameWorld.ChunkWidth &&
                            z + foliageRadius is >= 0 and < GameWorld.ChunkWidth &&
                            z - foliageRadius is >= 0 and < GameWorld.ChunkWidth)
                        {
                            int foliageY = height + treeHeight - 2;
                            for (int dy = 0; dy <= foliageRadius; dy++)
                            {
                                for (int dx = -foliageRadius; dx <= foliageRadius; dx++)
                                {
                                    for (int dz = -foliageRadius; dz <= foliageRadius; dz++)
                                    {
                                        int distance = dx * dx + dy * dy + dz * dz;
                                        if (distance <= foliageRadius * foliageRadius - 1 &&
                                            foliageY < GameWorld.ChunkHeight)
                                        {
                                            blocks[x + dx, foliageY + dy, z + dz] = BlockType.Leaves;
                                        }
                                    }
                                }
                            }

                            for (int dy = 0; dy < treeHeight; dy++)
                                blocks[x, height + dy, z] = BlockType.Wood;
                        }
                    }
                }
            }

            return blocks;
        }

        private float GetTreeNoise(float x, float z)
        {
            float result = 0;

            for (int i = 0; i < _noise.Length; i++)
            {
                float noise = _noise[i].GetNoise(x, z);
                result += noise * Octaves[i].Amplitude / 2;
            }

            return result;
        }
    }
}