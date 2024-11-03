using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MultiCraft.Scripts.Game.World
{
    public class GameWorld : MonoBehaviour
    {
        [FormerlySerializedAs("LoadRadius")] public int loadRadius = 5;

        public Dictionary<Vector3Int, Chunk> Chunks = new Dictionary<Vector3Int, Chunk>();
        [FormerlySerializedAs("ChunkPrefab")] public ChunkRenderer chunkPrefab;
        [FormerlySerializedAs("Generator")] public TerrainGenerator generator;

        [FormerlySerializedAs("BlockDataBase")]
        public BlockDataBase blockDataBase;

        private Camera _mainCamera;
        private Vector3Int _currentPlayerChunk;

        void Start()
        {
            _mainCamera = Camera.main;
            generator.InitializeNoise();

            StartCoroutine(Generate(false));
        }

        private IEnumerator Generate(bool wait)
        {
            for (var x = _currentPlayerChunk.x - loadRadius; x <= _currentPlayerChunk.x + loadRadius; x++)
            {
                for (var z = _currentPlayerChunk.z - loadRadius; z <= _currentPlayerChunk.z + loadRadius; z++)
                {
                    var chunkPosition = new Vector3Int(x, 0, z);
                    if (Chunks.ContainsKey(chunkPosition)) continue;

                    LoadChunkAt(chunkPosition);
                    if (wait) yield return new WaitForSeconds(0.2f);
                }
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void LoadChunkAt(Vector3Int chunkPosition)
        {
            var xPos = chunkPosition.x * ChunkRenderer.ChunkWidth;
            var yPos = chunkPosition.y * ChunkRenderer.ChunkHeight;
            var zPos = chunkPosition.z * ChunkRenderer.ChunkWidth;

            var chunk = new Chunk
            {
                Position = chunkPosition,
                Blocks = generator.GenerateTerrain(xPos, yPos, zPos),
            };

            Chunks.Add(chunkPosition, chunk);

            var chunkObject = Instantiate(chunkPrefab, new Vector3(xPos, 0, zPos), Quaternion.identity, transform);
            chunkObject.Chunk = chunk;
            chunkObject.parentWorld = this;
            
            
            chunk.Renderer = chunkObject;
            /*
            ChunkData chunkData = new ChunkData();
            chunkData.ChunkPosition = chunkPosition;
            chunkData.Blocks =
                generator.GenerateTerrain(xPos, yPos);
            ChunkDatas.Add(chunkPosition, chunkData);

            var chunk = Instantiate(ChunkPrefab, new Vector3(xPos, 0, yPos), Quaternion.identity, transform);
            chunk.ChunkData = chunkData;
            chunk.ParentWorld = this;

            chunkData.Renderer = chunk;*/
        }

        public void Update()
        {
            Vector3Int playerWorldPosition = Vector3Int.FloorToInt(_mainCamera.transform.position);
            Vector3Int playerChunk = GetChunkContainBlock(playerWorldPosition);
            if (playerChunk != _currentPlayerChunk)
            {
                _currentPlayerChunk = playerChunk;
                StartCoroutine(Generate(true));
            }

            CheckInput();
        }
        
        [ContextMenu("Regenerate World")]
        public void Regenerate()
        {
            generator.InitializeNoise();
            foreach (var chunk in Chunks.Values)
            {
                Destroy(chunk.Renderer.gameObject);
            }
            Chunks.Clear();
            StartCoroutine(Generate(false));
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void CheckInput()
        {
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(0))
            {
                bool isDestoying = Input.GetMouseButtonDown(0);
                Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

                if (Physics.Raycast(ray, out var hitInfo))
                {
                    Vector3 blockCenter;
                    if (isDestoying)
                    {
                        blockCenter = hitInfo.point - hitInfo.normal * 0.5f;
                    }
                    else
                    {
                        blockCenter = hitInfo.point + hitInfo.normal * 0.5f;
                    }

                    Vector3Int blockWorldPosition = Vector3Int.FloorToInt(blockCenter);
                    Vector3Int chunkPosition = GetChunkContainBlock(blockWorldPosition);
                    if (Chunks.TryGetValue(chunkPosition, out Chunk chunk))
                    {
                        Vector3Int chunkOrigin = new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z) *
                                                 ChunkRenderer.ChunkWidth;
                        if (isDestoying)
                        {
                            chunk.Renderer.DestroyBlock(blockWorldPosition - chunkOrigin);
                        }
                        else
                        {
                            chunk.Renderer.SpawnBlock(blockWorldPosition - chunkOrigin, BlockType.Grass);
                        }
                    }
                }
            }
        }

        private Vector3Int GetChunkContainBlock(Vector3Int blockWorldPosition)
        {
            var chunkPosition = new Vector3Int(
                blockWorldPosition.x / ChunkRenderer.ChunkWidth,
                blockWorldPosition.y / ChunkRenderer.ChunkHeight,
                blockWorldPosition.z / ChunkRenderer.ChunkWidth);

            if (blockWorldPosition.x < 0) chunkPosition.x--;
            if (blockWorldPosition.y < 0) chunkPosition.y--;
            if (blockWorldPosition.z < 0) chunkPosition.z--;
            
            return chunkPosition;
        }
    }
}