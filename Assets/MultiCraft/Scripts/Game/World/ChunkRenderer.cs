using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiCraft.Scripts.Game.World
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class ChunkRenderer:MonoBehaviour
    {
        public const int ChunkWidth = 16;
        public const int ChunkHeight = 256;

        public ChunkData ChunkData;
        public GameWorld ParentWorld;
        
        public Mesh chunkMesh;
        
        private List<Vector3> vertices = new List<Vector3>();
        private List<Vector2> uvs = new List<Vector2>();
        private List<int> triangles = new List<int>();

        private void Start()
        {
            chunkMesh = new Mesh();

            RegenerateMesh();
        }

        private void RegenerateMesh()
        {
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
            
            for (int y = 0; y < ChunkHeight; y++)
            {
                for (int x = 0; x < ChunkWidth; x++)
                {
                    for (int z = 0; z < ChunkWidth; z++)
                    {
                        GenerateBlock(x, y, z);
                    }
                }
            }
            
            chunkMesh.triangles = Array.Empty<int>();
            chunkMesh.vertices = vertices.ToArray();
            chunkMesh.uv = uvs.ToArray();
            chunkMesh.triangles = triangles.ToArray();
            
            chunkMesh.Optimize();
            
            chunkMesh.RecalculateBounds();
            chunkMesh.RecalculateNormals();
            
            GetComponent<MeshFilter>().mesh = chunkMesh;
            GetComponent<MeshCollider>().sharedMesh = chunkMesh;
        }

        public void SpawnBlock(Vector3Int blockPosition, BlockType block)
        {
            ChunkData.Blocks[blockPosition.x, blockPosition.y, blockPosition.z] = block;
            RegenerateMesh();
        }
        public void DestroyBlock(Vector3Int blockPosition)
        {
            ChunkData.Blocks[blockPosition.x, blockPosition.y, blockPosition.z] = BlockType.Air;
            RegenerateMesh();
        }
        private void GenerateBlock(int x, int y, int z)
        {
            Vector3Int blockPosition = new Vector3Int(x, y, z);
            
            if(GetBlockPosition(blockPosition) == BlockType.Air) return;

            if (GetBlockPosition(blockPosition + Vector3Int.right) == BlockType.Air) GenerateRightSide(blockPosition);
            if (GetBlockPosition(blockPosition + Vector3Int.left) == BlockType.Air) GenerateLeftSide(blockPosition);
            if (GetBlockPosition(blockPosition + Vector3Int.up) == BlockType.Air) GenerateTopSide(blockPosition);
            if (GetBlockPosition(blockPosition + Vector3Int.down) == BlockType.Air) GenerateBottomSide(blockPosition);
            if (GetBlockPosition(blockPosition + Vector3Int.forward) == BlockType.Air) GenerateFrontSide(blockPosition);
            if (GetBlockPosition(blockPosition + Vector3Int.back) == BlockType.Air) GenerateBackSide(blockPosition);
        }
        private BlockType GetBlockPosition(Vector3Int blockPosition)
        {
            if (blockPosition.x is >= 0 and < ChunkWidth &&
                blockPosition.y is >= 0 and < ChunkHeight &&
                blockPosition.z is >= 0 and < ChunkWidth)
            {
                return ChunkData.Blocks[blockPosition.x, blockPosition.y, blockPosition.z];
            }
            else
            {
                if (blockPosition.y < 0 || blockPosition.y >= ChunkHeight) return BlockType.Air;
                
                Vector2Int adjustedChunkPosition = ChunkData.ChunkPosition;

                if (blockPosition.x < 0)
                {
                    adjustedChunkPosition.x--;
                    blockPosition.x += ChunkWidth;
                }else if (blockPosition.x >= ChunkWidth)
                {
                    adjustedChunkPosition.x++;
                    blockPosition.x -= ChunkWidth;
                }
                if (blockPosition.z < 0)
                {
                    adjustedChunkPosition.y--;
                    blockPosition.z += ChunkWidth;
                }else if (blockPosition.z >= ChunkWidth)
                {
                    adjustedChunkPosition.y++;
                    blockPosition.z -= ChunkWidth;
                }

                if (ParentWorld.ChunkDatas.TryGetValue(adjustedChunkPosition, out ChunkData adjustedChunk))
                {
                    return adjustedChunk.Blocks[blockPosition.x, blockPosition.y, blockPosition.z];
                } else
                {
                    return BlockType.Air;
                }
            }
        }
        private void GenerateRightSide(Vector3Int blockPosition)
        {
            vertices.Add(new Vector3(1, 0, 0) + blockPosition);
            vertices.Add(new Vector3(1, 1, 0) + blockPosition);
            vertices.Add(new Vector3(1, 0, 1) + blockPosition);
            vertices.Add(new Vector3(1, 1, 1) + blockPosition);
            
            AddLastVerticesSquare();
        } 
        private void GenerateLeftSide(Vector3Int blockPosition)
        {
            vertices.Add(new Vector3(0, 0, 0) + blockPosition);
            vertices.Add(new Vector3(0, 0, 1) + blockPosition);
            vertices.Add(new Vector3(0, 1, 0) + blockPosition);
            vertices.Add(new Vector3(0, 1, 1) + blockPosition);
        
            AddLastVerticesSquare();
        }
        private void GenerateFrontSide(Vector3Int blockPosition)
        {
            vertices.Add(new Vector3(0, 0, 1) + blockPosition);
            vertices.Add(new Vector3(1, 0, 1) + blockPosition);
            vertices.Add(new Vector3(0, 1, 1) + blockPosition);
            vertices.Add(new Vector3(1, 1, 1) + blockPosition);
        
            AddLastVerticesSquare();
        }
        private void GenerateBackSide(Vector3Int blockPosition)
        {
            vertices.Add(new Vector3(0, 0, 0) + blockPosition);
            vertices.Add(new Vector3(0, 1, 0) + blockPosition);
            vertices.Add(new Vector3(1, 0, 0) + blockPosition);
            vertices.Add(new Vector3(1, 1, 0) + blockPosition);
        
            AddLastVerticesSquare();
        }
        private void GenerateTopSide(Vector3Int blockPosition)
        {
            vertices.Add(new Vector3(0, 1, 0) + blockPosition);
            vertices.Add(new Vector3(0, 1, 1) + blockPosition);
            vertices.Add(new Vector3(1, 1, 0) + blockPosition);
            vertices.Add(new Vector3(1, 1, 1) + blockPosition);
        
            AddLastVerticesSquare();
        }
        private void GenerateBottomSide(Vector3Int blockPosition)
        {
            vertices.Add(new Vector3(0, 0, 0) + blockPosition);
            vertices.Add(new Vector3(1, 0, 0) + blockPosition);
            vertices.Add(new Vector3(0, 0, 1) + blockPosition);
            vertices.Add(new Vector3(1, 0, 1) + blockPosition);
        
            AddLastVerticesSquare();
        }
        private void AddLastVerticesSquare()
        {
            uvs.Add(new Vector2(0.125f, 0.9375f));
            uvs.Add(new Vector2(0.125f, 1));
            uvs.Add(new Vector2(0.1875f, 0.9375f));
            uvs.Add(new Vector2(0.1875f, 1));
            
            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);
        
        
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 1);
            triangles.Add(vertices.Count - 2);
        }
    }
}
