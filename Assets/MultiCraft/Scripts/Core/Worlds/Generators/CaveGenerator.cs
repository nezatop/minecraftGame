using MultiCraft.Scripts.Game.Core.Worlds.Generators;
using UnityEngine;

namespace MultiCraft.Scripts.Core.Worlds.Generators
{
    [CreateAssetMenu(fileName = "Cave Generator", menuName = "MultiCraft/Generators/Cave Generator")]
    public class CaveGenerator : ScriptableObject
    {
        public NoiseOctaveSetting Octaves;
        public float CaveFrequency = 0.5f;

        private FastNoiseLite _caveNoise;

        public void InitializeNoise(int seed)
        {
            _caveNoise = new FastNoiseLite();
            _caveNoise.SetNoiseType(Octaves.NoiseType);
            _caveNoise.SetFrequency(CaveFrequency);
            _caveNoise.SetSeed(seed);
        }

        public int[,,] GenerateCave(int[,,] blocks, int[,] surfaceHeight, int xOffset, int yOffset,
            int zOffset)
        {
            
            for (int x = 0; x < World.ChunkWidth; x++)
            {
                for (int z = 0; z < World.ChunkWidth; z++)
                {
                    for (int y = 0; y < surfaceHeight[x, z] + 1; y++)
                    {
                        float caveNoiseValue = _caveNoise.GetNoise(x + xOffset, y + yOffset, z + zOffset);
                        if (caveNoiseValue < -CaveFrequency || caveNoiseValue > CaveFrequency)
                        {
                            if (y > 0)
                                blocks[x, y, z] = 0;
                        }
                    }
                }
            }

            return blocks;
        }
    }
}