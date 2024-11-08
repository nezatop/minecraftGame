using MultiCraft.Scripts.Game.Blocks;
using MultiCraft.Scripts.Game.Chunks;
using UnityEngine;

namespace MultiCraft.Scripts.Game.World.Generators
{
    [CreateAssetMenu(fileName = "Cave Generator", menuName = "MultiCraft/Generators/Cave Generator")]
    public class CaveGenerator : ScriptableObject
    {
        public NoiseOctaveSetting Octaves;
        public float CaveFrequency = 0.5f;

        private FastNoiseLite _caveNoise;

        public void InitializeNoise()
        {
            _caveNoise = new FastNoiseLite();
            _caveNoise.SetNoiseType(Octaves.NoiseType);
            _caveNoise.SetFrequency(CaveFrequency);
        }

        public BlockType[,,] GenerateCave(BlockType[,,] blocks, int[,] surfaceHeight, int xOffset, int yOffset,
            int zOffset, int seed)
        {
            for (int x = 0; x < GameWorld.ChunkWidth; x++)
            {
                for (int z = 0; z < GameWorld.ChunkWidth; z++)
                {
                    for (int y = 0; y < surfaceHeight[x, z] + 1; y++)
                    {
                        float caveNoiseValue = _caveNoise.GetNoise(x + xOffset, y + yOffset, z + zOffset);
                        if (caveNoiseValue > CaveFrequency)
                        {
                            if (y > 0)
                                blocks[x, y, z] = BlockType.Air;
                        }
                    }
                }
            }

            return blocks;
        }
    }
}