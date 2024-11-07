using MultiCraft.Scripts.Game.Blocks;
using MultiCraft.Scripts.Game.World;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace MultiCraft.Scripts.Game.Chunks
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class ChunkRenderer : MonoBehaviour
    {
        private ProfilerMarker Meshing = new ProfilerMarker(ProfilerCategory.Loading, "Meshing");

        public Chunk Chunk = new Chunk();

        public GameWorld ParentWorld;
        public Mesh ChunkMesh;

        private static int[] _triangles;

        private void Awake()
        {
            ChunkMesh = new Mesh();

            GetComponent<MeshFilter>().sharedMesh = ChunkMesh;
        }

        public void SpawnBlock(Vector3Int position, BlockType blockType)
        {
            Chunk.Blocks[position.x, position.y, position.z] = blockType;
            RegenerateMesh();
        }

        public BlockType DestroyBlock(Vector3Int position)
        {
            var destroyedBlockType = Chunk.Blocks[position.x, position.y, position.z];
            Chunk.Blocks[position.x, position.y, position.z] = BlockType.Air;
            RegenerateMesh();
            
            return destroyedBlockType;
        }

        public void RegenerateMesh()
        {
            SetMesh(MeshBuilder.GenerateMesh(Chunk));
        }

        public static void InitializeTriangles()
        {
            _triangles = new int[GameWorld.ChunkWidth * GameWorld.ChunkWidth * GameWorld.ChunkHeight * 6 / 4];

            var vertexNumber = 4;
            for (int i = 0; i < _triangles.Length; i += 6)
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

        public void SetMesh(GeneratedMesh mesh)
        {
            Meshing.Begin();
            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.SNorm8, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };

            ChunkMesh.SetVertexBufferParams(mesh.Vertices.Length, layout);
            ChunkMesh.SetVertexBufferData(mesh.Vertices, 0, 0, mesh.Vertices.Length, 0,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);

            int trianglesCount = mesh.Vertices.Length / 4 * 6;
            ChunkMesh.SetIndexBufferParams(trianglesCount, IndexFormat.UInt32);
            ChunkMesh.SetIndexBufferData(_triangles, 0, 0, trianglesCount,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontRecalculateBounds);

            ChunkMesh.subMeshCount = 1;
            ChunkMesh.SetSubMesh(0, new SubMeshDescriptor(0, trianglesCount));

            ChunkMesh.bounds = mesh.Bounds;

            GetComponent<MeshCollider>().sharedMesh = ChunkMesh;
            Meshing.End();
        }
    }
}