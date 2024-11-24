using MultiCraft.Scripts.Game.Core.Worlds.Generators;
using UnityEngine;

namespace MultiCraft.Scripts.Core.Worlds.Generators
{
    [CreateAssetMenu(fileName = "Surface Generator", menuName = "MultiCraft/Generators/Surface Generator")]
    public class SurfaceGenerator : ScriptableObject
    {
        public NoiseOctaveSetting[] Octaves;
        public NoiseOctaveSetting DomainWarp;

        private FastNoiseLite[] _noise;
        private FastNoiseLite _warpNoise;

        public void InitializeNoise(int seed)
        {
            _noise = new FastNoiseLite[Octaves.Length];
            for (var i = 0; i < Octaves.Length; i++)
            {
                _noise[i] = new FastNoiseLite();
                _noise[i].SetNoiseType(Octaves[i].NoiseType);
                _noise[i].SetFrequency(Octaves[i].Frequency);
                _noise[i].SetSeed(seed);
            }

            _warpNoise = new FastNoiseLite();
            _warpNoise.SetNoiseType(DomainWarp.NoiseType);
            _warpNoise.SetFrequency(DomainWarp.Frequency);
            _warpNoise.SetDomainWarpAmp(DomainWarp.Amplitude);
            _warpNoise.SetSeed(seed);
        }


        public int[,,] GenerateSurface(int[,,] blocks,out int[,] surfaceHeight,int xOffset, int yOffset, int zOffset)
        {
            surfaceHeight = new int[World.ChunkWidth,World.ChunkWidth];
            for (var x = 0; x < World.ChunkWidth; x++)
            {
                for (var z = 0; z < World.ChunkWidth; z++)
                {
                    var height = GetHeight(x + xOffset, z + zOffset);
                    
                    for (var y = 0; y < (int)height + 1 && y < World.ChunkHeight; y++)
                    {
                        blocks[x, y, z] = 5;
                        if (y < height) blocks[x, y, z] = 4;
                        if (y < height - 1) blocks[x, y, z] = 3;
                        if (y < height - 3) blocks[x, y, z] = 2;
                        if (y < 1) blocks[x, y, z] = 1;
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