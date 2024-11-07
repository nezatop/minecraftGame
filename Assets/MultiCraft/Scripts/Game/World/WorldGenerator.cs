using MultiCraft.Scripts.Game.Blocks;

namespace MultiCraft.Scripts.Game.World
{
    public static class WorldGenerator
    {
        public static BlockType[,,] GenerateWorld(int xOffset, int yOffset, int zOffset, int seed)
        {
            var blocks = new BlockType[GameWorld.ChunkWidth, GameWorld.ChunkHeight, GameWorld.ChunkWidth];

            for (int x = 0; x < GameWorld.ChunkWidth; x++)
            {
                for (int z = 0; z < GameWorld.ChunkWidth; z++)
                {
                    for (int y = 0; y < GameWorld.ChunkHeight; y++)
                    {
                        blocks[x, y, z] = BlockType.Air;
                        if (y < 64) blocks[x, y, z] = BlockType.Stone;
                    }
                }
            }
            
            return blocks;
        }
    }
}