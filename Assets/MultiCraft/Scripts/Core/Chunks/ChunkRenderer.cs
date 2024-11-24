using MultiCraft.Scripts.Core.Worlds;
using MultiCraft.Scripts.Game.Core.Worlds;
using UnityEngine;
using UnityEngine.Rendering;

namespace MultiCraft.Scripts.Core.Chunks
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class ChunkRenderer : MonoBehaviour
    {
        public Chunk Chunk = new Chunk();

        public MeshFilter meshFilter;
        public MeshCollider meshCollider;
        public MeshRenderer meshRenderer;

        private Mesh _mesh;


        private static int[] _triangles;

        private void Awake()
        {
            _mesh = new Mesh();

            meshFilter.sharedMesh = _mesh;
        }

        public static void InitializeTriangles()
        {
            _triangles = new int[World.ChunkWidth * World.ChunkWidth * World.ChunkHeight * 6 * 2];

            var vertexNumber = 4;
            for (var i = 0; i < _triangles.Length; i += 6)
            {
                _triangles[i] = vertexNumber - 4;
                _triangles[i + 1] = vertexNumber - 3;
                _triangles[i + 2] = vertexNumber - 2;
                _triangles[i + 3] = vertexNumber - 3;
                _triangles[i + 4] = vertexNumber - 1;
                _triangles[i + 5] = vertexNumber - 2;

                vertexNumber += 4;
            }
        }

        private void RegenerateMesh()
        {
            SetMesh(MeshBuilder.GenerateMesh(Chunk));
        }

        public void SetMesh(GeneratedMesh mesh)
        {
            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.SNorm8, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };

            _mesh.SetVertexBufferParams(mesh.Vertices.Length, layout);
            _mesh.SetVertexBufferData(mesh.Vertices, 0, 0, mesh.Vertices.Length, 0,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);

            var trianglesCount = mesh.Vertices.Length / 4 * 6;
            _mesh.SetIndexBufferParams(trianglesCount, IndexFormat.UInt32);
            _mesh.SetIndexBufferData(_triangles, 0, 0, trianglesCount,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontRecalculateBounds);

            _mesh.subMeshCount = 1;
            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, trianglesCount));

            _mesh.bounds = mesh.Bounds;

            meshCollider.sharedMesh = _mesh;
        }

        public void SpawnBlock(Vector3Int position, int blockType)
        {
            Chunk.Blocks[position.x, position.y, position.z] = blockType;
            RegenerateMesh();

            if (position.x == 0)
                Chunk.LeftChunk.Renderer.RegenerateMesh();
            if (position.x == World.ChunkWidth - 1)
                Chunk.RightChunk.Renderer.RegenerateMesh();
            if (position.z == 0)
                Chunk.BackChunk.Renderer.RegenerateMesh();
            if (position.z == World.ChunkWidth - 1)
                Chunk.FrontChunk.Renderer.RegenerateMesh();
        }

        public int DestroyBlock(Vector3Int position)
        {
            var destroyedBlockType = Chunk.Blocks[position.x, position.y, position.z];
            Chunk.Blocks[position.x, position.y, position.z] = 0;
            RegenerateMesh();

            if (position.x == 0)
                Chunk.LeftChunk.Renderer.RegenerateMesh();
            if (position.x == World.ChunkWidth - 1)
                Chunk.RightChunk.Renderer.RegenerateMesh();
            if (position.z == 0)
                Chunk.BackChunk.Renderer.RegenerateMesh();
            if (position.z == World.ChunkWidth - 1)
                Chunk.FrontChunk.Renderer.RegenerateMesh();

            return destroyedBlockType;
        }
    }
}