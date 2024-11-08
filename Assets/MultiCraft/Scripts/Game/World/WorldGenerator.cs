using MultiCraft.Scripts.Game.Blocks;
using MultiCraft.Scripts.Game.World.Generators;
using UnityEngine;

namespace MultiCraft.Scripts.Game.World
{
    [CreateAssetMenu(fileName = "WorldGenerator", menuName = "MultiCraft/WorldGenerator")]
    public class WorldGenerator : ScriptableObject
    {
        [Header("Generators")] public SurfaceGenerator SurfaceGenerator;
        public CaveGenerator CaveGenerator;
        public TreeGenerator TreeGenerator;

        [Header("Settings")] public const int BaseHeight = 64;

        public BlockType[,,] GenerateWorld(int xOffset, int yOffset, int zOffset, int seed)
        {
            var blocks = new BlockType[GameWorld.ChunkWidth, GameWorld.ChunkHeight, GameWorld.ChunkWidth];

            blocks = SurfaceGenerator.GenerateSurface(blocks, out var surfaceHeight, xOffset, yOffset, zOffset, seed);
            blocks = CaveGenerator.GenerateCave(blocks, surfaceHeight, xOffset, yOffset, zOffset, seed);
            blocks = TreeGenerator.GenerateTree(blocks, surfaceHeight, xOffset, yOffset, zOffset, seed);

            return blocks;
        }

        public void InitializeGenerators()
        {
            SurfaceGenerator.InitializeNoise();
            CaveGenerator.InitializeNoise();
            TreeGenerator.InitializeNoise();
        }
    }
}