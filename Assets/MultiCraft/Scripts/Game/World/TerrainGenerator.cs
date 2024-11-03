using System;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiCraft.Scripts.Game.World
{
    public class TerrainGenerator : MonoBehaviour
    {
        public float BaseHeight = 64f;
        public NoiseOctaveSettring[] Octaves;
        public NoiseOctaveSettring DomainWarp;

        [Serializable]
        public class NoiseOctaveSettring
        {
            public FastNoiseLite.NoiseType NoiseType;
            public float Frequency;
            public float Amplitude;
        }
        
        private FastNoiseLite[] _noise;
        private FastNoiseLite _warpNoise;
        
        private void Awake()
        {
           InitializeNoise();
        }

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

        public BlockType[,,] GenerateTerrain(int xOffset, int yOffset, int zOffset)
        {
            BlockType[,,] result = new BlockType[ChunkRenderer.ChunkWidth, ChunkRenderer.ChunkHeight,
                ChunkRenderer.ChunkWidth];
            
            for (int x = 0; x < ChunkRenderer.ChunkWidth; x++)
            {
                for (int z = 0; z < ChunkRenderer.ChunkWidth; z++)
                {
                    float height = GetHeight( x + xOffset, z + zOffset);

                    for (int y = 0; y < height; y++)
                    {
                        result[x, y, z] = BlockType.Grass;
                    }
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