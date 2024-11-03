using System;
using UnityEngine;

namespace MultiCraft.Scripts.Game.World
{
    [CreateAssetMenu(fileName = "World Terrain Generator", menuName = "MultiCraft/World/Terrain Generator")]
    public class TerrainGenerator : ScriptableObject
    {
        public float BaseHeight = 64f;
        public NoiseOctaveSetting[] Octaves;
        public NoiseOctaveSetting DomainWarp;
        public NoiseOctaveSetting CaveNoise;
        
        [Serializable]
        public class NoiseOctaveSetting
        {
            public FastNoiseLite.NoiseType NoiseType;
            public float Frequency;
            public float Amplitude;
        }
        
        private FastNoiseLite[] _noise;
        private FastNoiseLite _warpNoise;
        private FastNoiseLite _caveNoise;
        
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
            
            _caveNoise = new FastNoiseLite();
            _caveNoise.SetNoiseType(CaveNoise.NoiseType);
            _caveNoise.SetFrequency(CaveNoise.Frequency);
        }

        public BlockType[,,] GenerateTerrain(int xOffset, int yOffset, int zOffset)
        {
            BlockType[,,] result = new BlockType[ChunkRenderer.ChunkWidth, ChunkRenderer.ChunkHeight, ChunkRenderer.ChunkWidth];
    
            for (int x = 0; x < ChunkRenderer.ChunkWidth; x++)
            {
                for (int z = 0; z < ChunkRenderer.ChunkWidth; z++)
                {
                    float height = GetHeight(x + xOffset, z + zOffset);

                    for (int y = 0; y < ChunkRenderer.ChunkHeight; y++)
                    {
                        if (y < height)
                        {
                            float caveNoiseValue = _caveNoise.GetNoise(x + xOffset, y + yOffset, z + zOffset);
                            
                            result[x, y, z] = BlockType.Grass;
                            if (y < height - 1) result[x, y, z] = BlockType.Dirt;
                            if (y < height-3) result[x, y, z] = BlockType.Stone;
                            if (y < 2) result[x, y, z] = BlockType.Stone;
                            if (caveNoiseValue > CaveNoise.Amplitude || caveNoiseValue < -CaveNoise.Amplitude) 
                            {
                                    result[x, y, z] = BlockType.Air;
                            }
                        }
                        else
                        {
                            result[x, y, z] = BlockType.Air;
                        }
                    }
                }
            }

            return result;
        }

        private float GetHeight(float x, float z)
        {
            _warpNoise.DomainWarp(ref x, ref z);
            
            float result = BaseHeight;

            for (int i = 0; i < _noise.Length; i++)
            {
                float noise = _noise[i].GetNoise(x, z);
                result += noise * Octaves[i].Amplitude/2;
            }
            
            return result;
        } 
    }
}