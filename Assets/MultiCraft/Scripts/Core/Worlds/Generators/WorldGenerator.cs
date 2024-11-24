using UnityEngine;

namespace MultiCraft.Scripts.Core.Worlds.Generators
{
    [CreateAssetMenu(fileName = "WorldGenerator", menuName = "MultiCraft/WorldGenerator")]
    public class WorldGenerator : ScriptableObject
    {
        [Header("Generators")] 
        public SurfaceGenerator surfaceGenerator;
        public CaveGenerator CaveGenerator;
        public TreeGenerator TreeGenerator;

        [Header("Settings")] public const int BaseHeight = 64;

        public int[,,] Generate(int xOffset, int yOffset, int zOffset)
        {
            var blocks = new int[World.ChunkWidth, World.ChunkHeight, World.ChunkWidth];

            blocks = surfaceGenerator.GenerateSurface(blocks, out var surfaceHeight, xOffset, yOffset, zOffset);
            blocks = CaveGenerator.GenerateCave(blocks, surfaceHeight, xOffset, yOffset, zOffset);
            blocks = TreeGenerator.GenerateTree(blocks, surfaceHeight, xOffset, yOffset, zOffset);

            return blocks;
        }

        public void InitializeGenerators(int seed)
        {
            surfaceGenerator.InitializeNoise(seed);
            CaveGenerator.InitializeNoise(seed);
            TreeGenerator.InitializeNoise(seed);
        }
    }
}