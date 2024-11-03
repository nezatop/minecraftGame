using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MultiCraft.Scripts.Game.World
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class ChunkRenderer : MonoBehaviour
    {
        public const int ChunkWidth = 16;
        public const int ChunkHeight = 256;

        public Chunk Chunk;
        [FormerlySerializedAs("ParentWorld")] public GameWorld parentWorld;

        public Mesh chunkMesh;

        private Chunk _topChunk;
        private Chunk _bottomChunk;
        private Chunk _leftChunk;
        private Chunk _rightChunk;
        private Chunk _backChunk;
        private Chunk _frontChunk;

        private List<Vector3> _vertices = new List<Vector3>();
        private List<Vector2> _uvs = new List<Vector2>();
        private List<int> _triangles = new List<int>();

        private void Start()
        {
            parentWorld.Chunks.TryGetValue(Chunk.Position + Vector3Int.left, out _leftChunk);
            parentWorld.Chunks.TryGetValue(Chunk.Position + Vector3Int.right, out _rightChunk);
            parentWorld.Chunks.TryGetValue(Chunk.Position + Vector3Int.forward, out _frontChunk);
            parentWorld.Chunks.TryGetValue(Chunk.Position + Vector3Int.back, out _backChunk);
            parentWorld.Chunks.TryGetValue(Chunk.Position + Vector3Int.up, out _topChunk);
            parentWorld.Chunks.TryGetValue(Chunk.Position + Vector3Int.down, out _bottomChunk);
            chunkMesh = new Mesh();

            RegenerateMesh();
        }

        private void RegenerateMesh()
        {
            _vertices.Clear();
            _uvs.Clear();
            _triangles.Clear();

            for (var y = 0; y < ChunkHeight; y++)
            {
                for (var x = 0; x < ChunkWidth; x++)
                {
                    for (var z = 0; z < ChunkWidth; z++)
                    {
                        GenerateBlock(x, y, z);
                    }
                }
            }

            chunkMesh.triangles = Array.Empty<int>();
            chunkMesh.vertices = _vertices.ToArray();
            chunkMesh.uv = _uvs.ToArray();
            chunkMesh.triangles = _triangles.ToArray();

            chunkMesh.Optimize();

            chunkMesh.RecalculateBounds();
            chunkMesh.RecalculateNormals();

            GetComponent<MeshFilter>().mesh = chunkMesh;
            GetComponent<MeshCollider>().sharedMesh = chunkMesh;
        }

        public void SpawnBlock(Vector3Int blockPosition, BlockType block)
        {
            Chunk.Blocks[blockPosition.x, blockPosition.y, blockPosition.z] = block;
            RegenerateMesh();
        }

        public void DestroyBlock(Vector3Int blockPosition)
        {
            Chunk.Blocks[blockPosition.x, blockPosition.y, blockPosition.z] = BlockType.Air;
            
            if (blockPosition.x == 0 && _leftChunk != null) _leftChunk.Renderer.RegenerateMesh();
            if (blockPosition.x == ChunkWidth - 1 && _rightChunk != null) _rightChunk.Renderer.RegenerateMesh();
            if (blockPosition.z == 0 && _backChunk != null) _backChunk.Renderer.RegenerateMesh();
            if (blockPosition.z == ChunkWidth - 1 && _frontChunk != null) _frontChunk.Renderer.RegenerateMesh();
            
            RegenerateMesh();
        }

        private void GenerateBlock(int x, int y, int z)
        {
            Vector3Int blockPosition = new Vector3Int(x, y, z);

            var blockType = GetBlockPosition(blockPosition);
            if (blockType == BlockType.Air) return;

            if (GetBlockPosition(blockPosition + Vector3Int.right) == BlockType.Air)
            {
                GenerateRightSide(blockPosition);
                AddUvs(blockType, Vector3Int.right);
            }

            if (GetBlockPosition(blockPosition + Vector3Int.left) == BlockType.Air)
            {
                GenerateLeftSide(blockPosition);
                AddUvs(blockType, Vector3Int.left);
            }

            if (GetBlockPosition(blockPosition + Vector3Int.up) == BlockType.Air)
            {
                GenerateTopSide(blockPosition);
                AddUvs(blockType, Vector3Int.up);
            }

            if (GetBlockPosition(blockPosition + Vector3Int.down) == BlockType.Air)
            {
                GenerateBottomSide(blockPosition);
                AddUvs(blockType, Vector3Int.down);
            }

            if (GetBlockPosition(blockPosition + Vector3Int.forward) == BlockType.Air)
            {
                GenerateFrontSide(blockPosition);
                AddUvs(blockType, Vector3Int.forward);
            }

            if (GetBlockPosition(blockPosition + Vector3Int.back) == BlockType.Air)
            {
                GenerateBackSide(blockPosition);
                AddUvs(blockType, Vector3Int.forward);
            }
        }

        private BlockType GetBlockPosition(Vector3Int blockPosition)
        {
            if (blockPosition.x is >= 0 and < ChunkWidth &&
                blockPosition.y is >= 0 and < ChunkHeight &&
                blockPosition.z is >= 0 and < ChunkWidth)
                return Chunk.Blocks[blockPosition.x, blockPosition.y, blockPosition.z];

            if (blockPosition.y is < 0 or >= ChunkHeight) return BlockType.Air;

            if (blockPosition.x < 0)
            {
                if (_leftChunk == null) return BlockType.Air;
                return _leftChunk.Blocks[blockPosition.x + ChunkWidth, blockPosition.y, blockPosition.z];
            }

            if (blockPosition.x >= ChunkWidth)
            {
                if (_rightChunk == null) return BlockType.Air;
                return _rightChunk.Blocks[blockPosition.x - ChunkWidth, blockPosition.y, blockPosition.z];
            }

            if (blockPosition.z < 0)
            {
                if (_backChunk == null) return BlockType.Air;
                return _backChunk.Blocks[blockPosition.x, blockPosition.y, blockPosition.z + ChunkWidth];
            }

            if (blockPosition.z >= ChunkWidth)
            {
                if (_frontChunk == null) return BlockType.Air;
                return _frontChunk.Blocks[blockPosition.x, blockPosition.y, blockPosition.z - ChunkWidth];
            }


            return BlockType.Air;
        }

        private void GenerateTopSide(Vector3Int blockPosition)
        {
            _vertices.Add(new Vector3(0, 1, 0) + blockPosition);
            _vertices.Add(new Vector3(0, 1, 1) + blockPosition);
            _vertices.Add(new Vector3(1, 1, 0) + blockPosition);
            _vertices.Add(new Vector3(1, 1, 1) + blockPosition);

            AddLastVerticesSquare();
        }

        private void GenerateBottomSide(Vector3Int blockPosition)
        {
            _vertices.Add(new Vector3(0, 0, 0) + blockPosition);
            _vertices.Add(new Vector3(1, 0, 0) + blockPosition);
            _vertices.Add(new Vector3(0, 0, 1) + blockPosition);
            _vertices.Add(new Vector3(1, 0, 1) + blockPosition);

            AddLastVerticesSquare();
        }
        
        private void GenerateFrontSide(Vector3Int blockPosition)
        {
            _vertices.Add(new Vector3(1, 0, 1) + blockPosition);
            _vertices.Add(new Vector3(1, 1, 1) + blockPosition);
            _vertices.Add(new Vector3(0, 0, 1) + blockPosition);
            _vertices.Add(new Vector3(0, 1, 1) + blockPosition);

            AddLastVerticesSquare();
        }

        private void GenerateBackSide(Vector3Int blockPosition)
        {
            _vertices.Add(new Vector3(0, 0, 0) + blockPosition);
            _vertices.Add(new Vector3(0, 1, 0) + blockPosition);
            _vertices.Add(new Vector3(1, 0, 0) + blockPosition);
            _vertices.Add(new Vector3(1, 1, 0) + blockPosition);
            
            AddLastVerticesSquare();
        }
        
        private void GenerateRightSide(Vector3Int blockPosition)
        {
            _vertices.Add(new Vector3(1, 0, 0) + blockPosition);
            _vertices.Add(new Vector3(1, 1, 0) + blockPosition);
            _vertices.Add(new Vector3(1, 0, 1) + blockPosition);
            _vertices.Add(new Vector3(1, 1, 1) + blockPosition);
            
            AddLastVerticesSquare();
        }

        private void GenerateLeftSide(Vector3Int blockPosition)
        {
            _vertices.Add(new Vector3(0, 0, 1) + blockPosition);
            _vertices.Add(new Vector3(0, 1, 1) + blockPosition);
            _vertices.Add(new Vector3(0, 0, 0) + blockPosition);
            _vertices.Add(new Vector3(0, 1, 0) + blockPosition);
            
            AddLastVerticesSquare();
        }

        private void AddLastVerticesSquare()
        {
            _triangles.Add(_vertices.Count - 4);
            _triangles.Add(_vertices.Count - 3);
            _triangles.Add(_vertices.Count - 2);

            _triangles.Add(_vertices.Count - 3);
            _triangles.Add(_vertices.Count - 1);
            _triangles.Add(_vertices.Count - 2);    
        }

        private void AddUvs(BlockType blockType, Vector3Int normal)
        {
            var block = parentWorld.blockDataBase.GetBlock(blockType);
            
            var textureAtlasSize =  parentWorld.blockDataBase.textureAtlasSize;
            var textureResolution = parentWorld.blockDataBase.textureResolution;
            var pixelOffset = block.GetPixelsOffset(normal);
            
            _uvs.Add(new Vector2(
                (pixelOffset.x * textureResolution) / textureAtlasSize.x,
                (pixelOffset.y * textureResolution) / textureAtlasSize.y));
            _uvs.Add(new Vector2(
                (pixelOffset.x * textureResolution) / textureAtlasSize.x,
                ((pixelOffset.y + 1) * textureResolution) / textureAtlasSize.y));
            _uvs.Add(new Vector2(
                ((pixelOffset.x + 1) * textureResolution) / textureAtlasSize.x,
                (pixelOffset.y * textureResolution) / textureAtlasSize.y));
            _uvs.Add(new Vector2(
                ((pixelOffset.x + 1) * textureResolution) / textureAtlasSize.x,
                ((pixelOffset.y + 1) * textureResolution) / textureAtlasSize.y));
        }
    }
}