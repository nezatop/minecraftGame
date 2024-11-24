using MultiCraft.Scripts.Game.Core.Worlds.Generators;
using UnityEngine;

namespace MultiCraft.Scripts.Core.Worlds.Generators
{
    [CreateAssetMenu(fileName = "Tree Generator", menuName = "MultiCraft/Generators/Tree Generator")]
    public class TreeGenerator : ScriptableObject
    {
        public float TreeFrequency = 0.5f;

        public NoiseOctaveSetting[] Octaves;
        private FastNoiseLite[] _noise;

        public void InitializeNoise(int seed)
        {
            _noise = new FastNoiseLite[Octaves.Length];
            for (int i = 0; i < Octaves.Length; i++)
            {
                _noise[i] = new FastNoiseLite();
                _noise[i].SetNoiseType(Octaves[i].NoiseType);
                _noise[i].SetFrequency(Octaves[i].Frequency);
                _noise[i].SetSeed(seed);
            }
        }

        public int[,,] GenerateTree(int[,,] blocks, int[,] surfaceHeight, int xOffset, int yOffset,
            int zOffset)
        {
            for (int x = 0; x < World.ChunkWidth; x++)
            {
                for (int z = 0; z < World.ChunkWidth; z++)
                {
                    int height = surfaceHeight[x, z];

                    float treeNoiseValue = GetTreeNoise(x + xOffset, z + zOffset);

                    if (treeNoiseValue > TreeFrequency && height > WorldGenerator.BaseHeight - 1)
                    {
                        int treeHeight = 7;
                        int foliageRadius = 3;

                        if (x + foliageRadius is >= 0 and < World.ChunkWidth &&
                            x - foliageRadius is >= 0 and < World.ChunkWidth &&
                            z + foliageRadius is >= 0 and < World.ChunkWidth &&
                            z - foliageRadius is >= 0 and < World.ChunkWidth &&
                            blocks[x, height - 1, z] != 0)
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
                                            foliageY < World.ChunkHeight)
                                        {
                                            blocks[x + dx, foliageY + dy, z + dz] = 7;
                                        }
                                    }
                                }
                            }

                            for (int dy = 0; dy < treeHeight; dy++)
                                blocks[x, height + dy, z] = 5;
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