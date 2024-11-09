using System.Collections.Generic;
using MultiCraft.Scripts.Game.Blocks;
using MultiCraft.Scripts.Game.World;
using UnityEngine;

namespace MultiCraft.Scripts.Game.Chunks
{
    public static class MeshBuilder
    {
        public static GeneratedMesh GenerateMesh(Chunk chunk)
        {
            List<GeneratedMeshVertex> vertices = new List<GeneratedMeshVertex>();

            int maxY = 0;
            for (int y = 0; y < GameWorld.ChunkHeight; y++)
            {
                for (int x = 0; x < GameWorld.ChunkWidth; x++)
                {
                    for (int z = 0; z < GameWorld.ChunkWidth; z++)
                    {
                        if (GenerateBlock(x, y, z, vertices, chunk))
                        {
                            if (maxY < y) maxY = y;
                        }
                    }
                }
            }

            var mesh = new GeneratedMesh();
            mesh.Vertices = vertices.ToArray();

            Vector3 boundsSize = new Vector3(GameWorld.ChunkWidth, maxY+2, GameWorld.ChunkWidth);
            mesh.Bounds = new Bounds(boundsSize / 2, boundsSize);
            mesh.Chunk = chunk;

            return mesh;
        }

        private static bool GenerateBlock(int x, int y, int z, List<GeneratedMeshVertex> vertices, Chunk chunk)
        {
            Vector3Int blockPosition = new Vector3Int(x, y, z);

            var blockType = GetBlockPosition(blockPosition, chunk);
            if (blockType == BlockType.Air) return false;

            if (GetBlockPosition(blockPosition + Vector3Int.right, chunk) == BlockType.Air)
                GenerateRightSide(blockPosition, vertices, blockType);
            if (GetBlockPosition(blockPosition + Vector3Int.left, chunk) == BlockType.Air)
                GenerateLeftSide(blockPosition, vertices, blockType);
            if (GetBlockPosition(blockPosition + Vector3Int.up, chunk) == BlockType.Air)
                GenerateTopSide(blockPosition, vertices, blockType);
            if (blockPosition.y > 0 && GetBlockPosition(blockPosition + Vector3Int.down, chunk) == BlockType.Air)
                GenerateBottomSide(blockPosition, vertices, blockType);
            if (GetBlockPosition(blockPosition + Vector3Int.forward, chunk) == BlockType.Air)
                GenerateFrontSide(blockPosition, vertices, blockType);
            if (GetBlockPosition(blockPosition + Vector3Int.back, chunk) == BlockType.Air)
                GenerateBackSide(blockPosition, vertices, blockType);

            return true;
        }

        private static BlockType GetBlockPosition(Vector3Int blockPosition, Chunk chunk)
        {
            if (blockPosition.x is >= 0 and < GameWorld.ChunkWidth &&
                blockPosition.y is >= 0 and < GameWorld.ChunkHeight &&
                blockPosition.z is >= 0 and < GameWorld.ChunkWidth)
                return chunk.Blocks[blockPosition.x, blockPosition.y, blockPosition.z];
            else
            {
                if (blockPosition.x < 0)
                    if (chunk.LeftChunk != null)
                        return chunk.LeftChunk.Blocks[blockPosition.x + GameWorld.ChunkWidth, blockPosition.y,
                            blockPosition.z];
                if (blockPosition.x >= GameWorld.ChunkWidth)
                    if (chunk.RightChunk != null)
                        return chunk.RightChunk.Blocks[blockPosition.x - GameWorld.ChunkWidth, blockPosition.y,
                            blockPosition.z];
                if (blockPosition.y < 0)
                    if (chunk.UpChunk != null)
                        return chunk.DownChunk.Blocks[blockPosition.x, blockPosition.y + GameWorld.ChunkHeight,
                            blockPosition.z];
                if (blockPosition.y >= GameWorld.ChunkHeight)
                    if (chunk.UpChunk != null)
                        return chunk.UpChunk.Blocks[blockPosition.x, blockPosition.y - GameWorld.ChunkHeight,
                            blockPosition.z];
                if (blockPosition.z < 0)
                    if (chunk.BackChunk != null)
                        return chunk.BackChunk.Blocks[blockPosition.x, blockPosition.y,
                            blockPosition.z + GameWorld.ChunkWidth];
                if (blockPosition.z >= GameWorld.ChunkWidth)
                    if (chunk.FrontChunk != null)
                        return chunk.FrontChunk.Blocks[blockPosition.x, blockPosition.y,
                            blockPosition.z - GameWorld.ChunkWidth];
            }

            return BlockType.Air;
        }

        private static void GenerateTopSide(Vector3Int blockPosition, List<GeneratedMeshVertex> vertices,
            BlockType blockType)
        {
            GeneratedMeshVertex vertex = new GeneratedMeshVertex();
            vertex.NormalX = 0;
            vertex.NormalY = sbyte.MaxValue;
            vertex.NormalZ = 0;
            vertex.NormalW = 1;

            Vector2 Uv1, Uv2, Uv3, Uv4;
            GetUvs(blockType, Vector3Int.up, out Uv1, out Uv2, out Uv3, out Uv4);

            vertex.Position = new Vector3(0, 1, 0) + blockPosition;
            vertex.Uv = Uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 1, 1) + blockPosition;
            vertex.Uv = Uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 0) + blockPosition;
            vertex.Uv = Uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 1) + blockPosition;
            vertex.Uv = Uv4;
            vertices.Add(vertex);
        }

        private static void GenerateBottomSide(Vector3Int blockPosition, List<GeneratedMeshVertex> vertices,
            BlockType blockType)
        {
            GeneratedMeshVertex vertex = new GeneratedMeshVertex();
            vertex.NormalX = 0;
            vertex.NormalY = sbyte.MinValue;
            vertex.NormalZ = 0;
            vertex.NormalW = 1;

            Vector2 Uv1, Uv2, Uv3, Uv4;
            GetUvs(blockType, Vector3Int.down, out Uv1, out Uv2, out Uv3, out Uv4);

            vertex.Position = new Vector3(0, 0, 0) + blockPosition;
            vertex.Uv = Uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 0, 0) + blockPosition;
            vertex.Uv = Uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 0, 1) + blockPosition;
            vertex.Uv = Uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 0, 1) + blockPosition;
            vertex.Uv = Uv4;
            vertices.Add(vertex);
        }

        private static void GenerateFrontSide(Vector3Int blockPosition, List<GeneratedMeshVertex> vertices,
            BlockType blockType)
        {
            GeneratedMeshVertex vertex = new GeneratedMeshVertex();
            vertex.NormalX = 0;
            vertex.NormalY = 0;
            vertex.NormalZ = sbyte.MaxValue;
            vertex.NormalW = 1;

            Vector2 Uv1, Uv2, Uv3, Uv4;
            GetUvs(blockType, Vector3Int.forward, out Uv1, out Uv2, out Uv3, out Uv4);

            vertex.Position = new Vector3(1, 0, 1) + blockPosition;
            vertex.Uv = Uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 1) + blockPosition;
            vertex.Uv = Uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 0, 1) + blockPosition;
            vertex.Uv = Uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 1, 1) + blockPosition;
            vertex.Uv = Uv4;
            vertices.Add(vertex);
        }

        private static void GenerateBackSide(Vector3Int blockPosition, List<GeneratedMeshVertex> vertices,
            BlockType blockType)
        {
            GeneratedMeshVertex vertex = new GeneratedMeshVertex();
            vertex.NormalX = 0;
            vertex.NormalY = 0;
            vertex.NormalZ = sbyte.MinValue;
            vertex.NormalW = 1;

            Vector2 Uv1, Uv2, Uv3, Uv4;
            GetUvs(blockType, Vector3Int.back, out Uv1, out Uv2, out Uv3, out Uv4);

            vertex.Position = new Vector3(0, 0, 0) + blockPosition;
            vertex.Uv = Uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 1, 0) + blockPosition;
            vertex.Uv = Uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 0, 0) + blockPosition;
            vertex.Uv = Uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 0) + blockPosition;
            vertex.Uv = Uv4;
            vertices.Add(vertex);
        }

        private static void GenerateRightSide(Vector3Int blockPosition, List<GeneratedMeshVertex> vertices,
            BlockType blockType)
        {
            GeneratedMeshVertex vertex = new GeneratedMeshVertex();
            vertex.NormalX = sbyte.MaxValue;
            vertex.NormalY = 0;
            vertex.NormalZ = 0;
            vertex.NormalW = 1;

            Vector2 Uv1, Uv2, Uv3, Uv4;
            GetUvs(blockType, Vector3Int.right, out Uv1, out Uv2, out Uv3, out Uv4);

            vertex.Position = new Vector3(1, 0, 0) + blockPosition;
            vertex.Uv = Uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 0) + blockPosition;
            vertex.Uv = Uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 0, 1) + blockPosition;
            vertex.Uv = Uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 1) + blockPosition;
            vertex.Uv = Uv4;
            vertices.Add(vertex);
        }

        private static void GenerateLeftSide(Vector3Int blockPosition, List<GeneratedMeshVertex> vertices,
            BlockType blockType)
        {
            GeneratedMeshVertex vertex = new GeneratedMeshVertex();
            vertex.NormalX = sbyte.MinValue;
            vertex.NormalY = 0;
            vertex.NormalZ = 0;
            vertex.NormalW = 1;

            Vector2 Uv1, Uv2, Uv3, Uv4;
            GetUvs(blockType, Vector3Int.left, out Uv1, out Uv2, out Uv3, out Uv4);

            vertex.Position = new Vector3(0, 0, 1) + blockPosition;
            vertex.Uv = Uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 1, 1) + blockPosition;
            vertex.Uv = Uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 0, 0) + blockPosition;
            vertex.Uv = Uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 1, 0) + blockPosition;
            vertex.Uv = Uv4;
            vertices.Add(vertex);
        }

        private static void GetUvs(BlockType blockType, Vector3Int normal, out Vector2 uv1, out Vector2 uv2,
            out Vector2 uv3, out Vector2 uv4)
        {
            var block = BlockDataBase.GetBlock(blockType);
            if (block == null)
            {
                uv1 = default;
                uv2 = default;
                uv3 = default;
                uv4 = default;
                return;
            }

            var textureAtlasSize = BlockDataBase.TextureAtlasSize;
            var textureResolution = BlockDataBase.TextureResolution;
            var pixelOffset = block.GetUvsPixelsOffset(normal);

            uv1 = new Vector2(
                (pixelOffset.x * textureResolution) / textureAtlasSize.x,
                (pixelOffset.y * textureResolution) / textureAtlasSize.y);
            uv2 = new Vector2(
                (pixelOffset.x * textureResolution) / textureAtlasSize.x,
                ((pixelOffset.y + 1) * textureResolution) / textureAtlasSize.y);
            uv3 = new Vector2(
                ((pixelOffset.x + 1) * textureResolution) / textureAtlasSize.x,
                (pixelOffset.y * textureResolution) / textureAtlasSize.y);
            uv4 = new Vector2(
                ((pixelOffset.x + 1) * textureResolution) / textureAtlasSize.x,
                ((pixelOffset.y + 1) * textureResolution) / textureAtlasSize.y);
        }
    }
}