using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MultiCraft.Scripts.Game.World
{
    [CreateAssetMenu(fileName = "World Terrain Generator", menuName = "MultiCraft/World/Terrain Generator")]
    public class TerrainGenerator : ScriptableObject
    {
        public float BaseHeight = 64f;
        public NoiseOctaveSetting[] Octaves;
        public NoiseOctaveSetting DomainWarp;
        public NoiseOctaveSetting CaveNoise;
        public NoiseOctaveSetting TreeNoise;
        
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
        private FastNoiseLite _treeNoise;
        
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
            
            _treeNoise = new FastNoiseLite();
            _treeNoise.SetNoiseType(TreeNoise.NoiseType);
            _treeNoise.SetFrequency(TreeNoise.Frequency);
        }

        public BlockType[,,] GenerateTerrain(int xOffset, int yOffset, int zOffset)
        {
            BlockType[,,] result = new BlockType[ChunkRenderer.ChunkWidth, ChunkRenderer.ChunkHeight, ChunkRenderer.ChunkWidth];
            List<Vector3Int> treePositions = new List<Vector3Int>();
            
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

                    if (height > 0 && height < ChunkRenderer.ChunkHeight)
                    {
                        float treeNoiseValue = _treeNoise.GetNoise(x + xOffset, z + zOffset);
                        if (treeNoiseValue > 0.5f)
                        {
                            treePositions.Add(new Vector3Int(x, (int)height, z));
                        }
                    }
                }
            }
            
            foreach (Vector3Int position in treePositions)
            {
                var x = position.x;
                var height = position.y;
                var z = position.z;
                int foliageRadius = (int)Random.Range(3f, 6f);
                if (x + foliageRadius is >= 0 and < ChunkRenderer.ChunkWidth &&
                    z + foliageRadius is >= 0 and < ChunkRenderer.ChunkWidth &&
                    x - foliageRadius is >= 0 and < ChunkRenderer.ChunkWidth &&
                    z - foliageRadius is >= 0 and < ChunkRenderer.ChunkWidth &&
                    result[x, (int)height, z] != BlockType.Air &&
                    result[x - 1, (int)height + 1, z] != BlockType.Wood &&
                    result[x + 1, (int)height + 1, z] != BlockType.Wood &&
                    result[x, (int)height + 1, z - 1] != BlockType.Wood &&
                    result[x, (int)height + 1, z + 1] != BlockType.Wood) 
                {
                    GenerateTree(result, x, height + 1, z, foliageRadius);
                }
            }
            
            return result;
        }

        private void GenerateTree(BlockType[,,] result, int x, int y, int z, int foliageRadius)
        {
            int treeHeight = Random.Range(5, 8); 
            
            for (int dy = 0; dy <= foliageRadius + 1; dy++)
            {
                for (int dx = -foliageRadius; dx <= foliageRadius; dx++)
                {
                    for (int dz = -foliageRadius; dz <= foliageRadius; dz++)
                    {
                        int foliageY = y + treeHeight - Random.Range(2, treeHeight - 2);
                        int distance = dx * dx + dy * dy + dz * dz;
                        if (distance <= foliageRadius * foliageRadius - 1 &&
                            foliageY < ChunkRenderer.ChunkHeight)
                        {
                            result[x + dx, foliageY + dy, z + dz] = BlockType.Leaves;
                        }
                    }
                }
            }
            
            for (int i = 0; i < treeHeight; i++)
            {
                if (y + i < ChunkRenderer.ChunkHeight)
                {
                    result[x, y + i, z] = BlockType.Wood;
                }
            }
            
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