using System.Linq;
using MultiCraft.Scripts.Game.Blocks;
using UnityEngine;

namespace MultiCraft.Scripts.Game.World.Generators
{
    [CreateAssetMenu(fileName = "Surface Generator", menuName = "MultiCraft/Generators/Surface Generator")]
    public class SurfaceGenerator : ScriptableObject
    {
        public NoiseOctaveSetting[] Octaves;
        public NoiseOctaveSetting DomainWarp;

        private FastNoiseLite[] _noise;
        private FastNoiseLite _warpNoise;

        public void InitializeNoise()
        {
            _noise = new FastNoiseLite[Octaves.Length];
            for (int i = 0; i < Octaves.Length; i++)
            {
                _noise[i] = new FastNoiseLite();
                _noise[i].SetNoiseType(Octaves[i].NoiseType);
                _noise[i].SetFrequency(Octaves[i].Frequency);
            }

            _warpNoise = new FastNoiseLite();
            _warpNoise.SetNoiseType(DomainWarp.NoiseType);
            _warpNoise.SetFrequency(DomainWarp.Frequency);
            _warpNoise.SetDomainWarpAmp(DomainWarp.Amplitude);
        }


        public BlockType[,,] GenerateSurface(BlockType[,,] blocks,out int[,] surfaceHeight,int xOffset, int yOffset, int zOffset, int seed)
        {
            surfaceHeight = new int[GameWorld.ChunkWidth, GameWorld.ChunkWidth];
            for (int x = 0; x < GameWorld.ChunkWidth; x++)
            {
                for (int z = 0; z < GameWorld.ChunkWidth; z++)
                {
                    float height = GetHeight(x + xOffset, z + zOffset);

                    for (int y = 0; y < (int)height + 1; y++)
                    {
                        blocks[x, y, z] = BlockType.Air;
                        if (y < height) blocks[x, y, z] = BlockType.Grass;
                        if (y < height - 1) blocks[x, y, z] = BlockType.Dirt;
                        if (y < height - 3) blocks[x, y, z] = BlockType.Stone;
                        if (y < 1) blocks[x, y, z] = BlockType.Cobblestone;
                    }
                    surfaceHeight[x,z] = (int)height;
                }
            }

            return blocks;
        }

        private float GetHeight(float x, float z)
        {
            _warpNoise.DomainWarp(ref x, ref z);

            float result = WorldGenerator.BaseHeight; 
            
            for (int i = 0; i < _noise.Length; i++)
            {
                float noise = _noise[i].GetNoise(x, z);
                result += noise * Octaves[i].Amplitude / 2;
            }

            return result;
        }
    }
}