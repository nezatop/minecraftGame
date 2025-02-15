using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MultiCraft.Scripts.Engine.Core.Blocks;
using MultiCraft.Scripts.Engine.Core.Chunks;
using MultiCraft.Scripts.Engine.Core.Entities;
using MultiCraft.Scripts.Engine.Core.Inventories;
using MultiCraft.Scripts.Engine.Core.Items;
using MultiCraft.Scripts.Engine.Core.MeshBuilders;
using MultiCraft.Scripts.Engine.Core.Player;
using MultiCraft.Scripts.Engine.Utils;
using UnityEngine;

namespace MultiCraft.Scripts.Engine.Network.Worlds
{
    public class NetworkWorld : MonoBehaviour
    {
        #region Singleton Instance

        public static NetworkWorld Instance { get; private set; }

        #endregion

        #region User and Chunk Settings

        [Header("User settings")] public ServerSettings settings;

        [Header("Chunk Settings")] public const int ChunkWidth = 16;
        private const int ChunkHeight = 256;

        #endregion

        #region Chunk Dictionaries

        [Header("Chunks Dictionary")] public int ChunksLoaded => Chunks.Count;
        public Dictionary<Vector3Int, Chunk> Chunks;
        public Dictionary<Vector3Int, Chunk> WaterChunks;
        public Dictionary<Vector3Int, Chunk> FloraChunks;

        #endregion

        #region Prefabs

        [Header("Prefabs")] public ChunkRenderer chunkPrefab;
        public ChunkRenderer waterChunkPrefab;
        public ChunkRenderer floraChunkPrefab;
        public DroppedItem droppedItemPrefab;

        #endregion

        #region Concurrent Queues for Meshing Results

        private readonly ConcurrentQueue<GeneratedMesh> _meshingResults = new ConcurrentQueue<GeneratedMesh>();
        private readonly ConcurrentQueue<GeneratedMesh> _meshingWaterResults = new ConcurrentQueue<GeneratedMesh>();
        private readonly ConcurrentQueue<GeneratedMesh> _meshingFloraResults = new ConcurrentQueue<GeneratedMesh>();

        #endregion

        #region Resource Loader

        [Header("Resource Loader")] public ResourceLoader resourceLoader;

        #endregion

        #region Player Data

        public GameObject player;
        public Vector3Int currentPosition;

        #endregion

        #region Unity Lifecycle Methods

        private void Awake()
        {
            Instance = this;
            StartCoroutine(InitializeWorld());
        }

        private IEnumerator InitializeWorld()
        {
            Chunks = new Dictionary<Vector3Int, Chunk>();
            WaterChunks = new Dictionary<Vector3Int, Chunk>();
            FloraChunks = new Dictionary<Vector3Int, Chunk>();

            ChunkRenderer.InitializeTriangles();
            DropItemRenderer.InitializeTriangles();
            HandRenderer.InitializeTriangles();
            
            if (PlayerPrefs.HasKey("RenderDistance"))
            {
                var renderDistance = PlayerPrefs.GetFloat("RenderDistance");
                settings.viewDistanceInChunks = Mathf.FloorToInt(renderDistance);
                settings.loadDistance = settings.viewDistanceInChunks + 1;
            }

            yield return StartCoroutine(resourceLoader.Initialize());
        }

        public void Update()
        {
            if (player)
                StartCoroutine(CheckPlayer());

            StartCoroutine(SpawnChunks());
            StartCoroutine(DispawnChunks());
        }

        #endregion

        #region Chunk Spawning Methods

        private IEnumerator DispawnChunks()
        {
            List<Vector3Int> chunksToRemove = new List<Vector3Int>();

            var positions = GetChunkContainBlock(currentPosition);
            
            foreach (var chunkEntry in Chunks)
            {
                Vector3Int chunkPosition = chunkEntry.Key;

                if (chunkPosition.x > positions.x + settings.loadDistance ||
                    chunkPosition.x < positions.x - settings.loadDistance ||
                    chunkPosition.y > positions.y + settings.loadDistance ||
                    chunkPosition.y < positions.y - settings.loadDistance ||
                    chunkPosition.z > positions.z + settings.loadDistance ||
                    chunkPosition.z < positions.z - settings.loadDistance)
                {
                    chunksToRemove.Add(chunkPosition);
                }
            }

            // Удаляем чанки, отмеченные для удаления
            foreach (var chunkPosition in chunksToRemove)
            {
                StartCoroutine(NetworkManager.Instance.DestroyChunk(chunkPosition));
                if(Chunks[chunkPosition].Renderer)
                {
                    Destroy(Chunks[chunkPosition].Renderer.gameObject);
                    Destroy(FloraChunks[chunkPosition].Renderer.gameObject);
                    Destroy(WaterChunks[chunkPosition].Renderer.gameObject);
                    Debug.Log($"Chunk {chunkPosition} has been destroyed");
                } // Если чанк использует ресурсы, освободите их
                Chunks.Remove(chunkPosition); // Удаляем из словаря
                FloraChunks.Remove(chunkPosition); // Удаляем из словаря
                WaterChunks.Remove(chunkPosition); // Удаляем из словаря
            }
            
            yield return null;
        }

        private IEnumerator SpawnChunks()
        {
            if (_meshingResults.TryDequeue(out var mesh))
                yield return SpawnChunkRenderer(mesh, chunkPrefab);

            if (_meshingWaterResults.TryDequeue(out mesh))
                yield return SpawnChunkRenderer(mesh, waterChunkPrefab);

            if (_meshingFloraResults.TryDequeue(out mesh))
                yield return SpawnChunkRenderer(mesh, floraChunkPrefab);
        }

        private IEnumerator SpawnChunkRenderer(GeneratedMesh mesh, ChunkRenderer prefab)
        {
            var xPos = mesh.Chunk.Position.x * ChunkWidth;
            var yPos = mesh.Chunk.Position.y * ChunkHeight;
            var zPos = mesh.Chunk.Position.z * ChunkWidth;

            var chunkObject = Instantiate(prefab, new Vector3(xPos, yPos, zPos), Quaternion.identity, transform);
            mesh.Chunk.Renderer = chunkObject;
            mesh.Chunk.State = ChunkState.Active;
            
            chunkObject.Chunk = mesh.Chunk;
            chunkObject.SetMesh(mesh);

            yield return null;
        }

        public IEnumerator SpawnChunk(Vector3Int position, int[,,] blocks)
        {
            if (Chunks.ContainsKey(position))
                yield break;

            Chunk chunk = new Chunk()
            {
                Position = position,
                Blocks = blocks,
                State = ChunkState.Generated
            };

            yield return null;

            Chunks.Add(position, chunk);

            yield return null;
        }

        public IEnumerator SpawnWaterChunk(Vector3Int position, int[,,] blocks)
        {
            if (WaterChunks.ContainsKey(position))
                yield break;
            Chunk chunk = new Chunk()
            {
                Position = position,
                Blocks = blocks,
                State = ChunkState.Generated
            };

            yield return null;

            WaterChunks.Add(position, chunk);

            yield return null;
        }

        public IEnumerator SpawnFloraChunk(Vector3Int position, int[,,] blocks)
        {
            if (FloraChunks.ContainsKey(position))
                yield break;
            Chunk chunk = new Chunk()
            {
                Position = position,
                Blocks = blocks,
                State = ChunkState.Generated
            };

            yield return null;

            FloraChunks.Add(position, chunk);

            yield return null;
        }

        #endregion

        #region Player Handling Methods

        private IEnumerator CheckPlayer()
        {
            var playersPosition = Vector3Int.FloorToInt(player.transform.position);
            if (currentPosition != playersPosition)
            {
                currentPosition = playersPosition;
                var playerChunkPosition = GetChunkContainBlock(currentPosition);
                for (int x = playerChunkPosition.x - settings.loadDistance;
                     x <= playerChunkPosition.x + settings.loadDistance;
                     x++)
                {
                    for (int z = playerChunkPosition.z - settings.loadDistance;
                         z <= playerChunkPosition.z + settings.loadDistance;
                         z++)
                    {
                        var chunkPos = new Vector3Int(x, 0, z);
                        if (Chunks.TryGetValue(chunkPos, out var chunk))
                        {
                            if (!chunk.Renderer && !NetworkManager.Instance.ChunksToRender.Contains(chunkPos))
                                NetworkManager.Instance.ChunksToRender.Enqueue(chunkPos);
                            continue;
                        }

                        if (!NetworkManager.Instance.ChunksToGet.Contains(chunkPos))
                            NetworkManager.Instance.ChunksToGet.Enqueue(chunkPos);

                        yield return null;
                    }
                }
            }
        }

        #endregion

        #region Chunk Rendering Methods

        public IEnumerator RenderChunks(Vector3Int position)
        {
            yield return RenderChunk(position, Chunks, _meshingResults);
        }

        public IEnumerator RenderWaterChunks(Vector3Int position)
        {
            yield return RenderChunk(position, WaterChunks, _meshingWaterResults);
        }

        public IEnumerator RenderFloraChunks(Vector3Int position)
        {
            yield return RenderChunk(position, FloraChunks, _meshingFloraResults);
        }

        private IEnumerator RenderChunk(Vector3Int position, Dictionary<Vector3Int, Chunk> chunkDict,
            ConcurrentQueue<GeneratedMesh> meshingQueue)
        {
            var playerChunkPosition = GetChunkContainBlock(currentPosition);
            if (chunkDict.TryGetValue(position, out var chunk))
            {
                SetChunkNeighbors(chunk, chunkDict);
                if (IsWithinViewDistance(playerChunkPosition, chunk.Position))
                {
                    chunk.State = ChunkState.MeshBuilding;
                    var mesh = MeshBuilder.GenerateMesh(chunk);
                    chunk.State = ChunkState.Loaded;

                    meshingQueue.Enqueue(mesh);
                }
            }

            yield return null;
        }

        private bool IsWithinViewDistance(Vector3Int playerChunkPosition, Vector3Int chunkPosition)
        {
            return playerChunkPosition.x - settings.viewDistanceInChunks <= chunkPosition.x &&
                   playerChunkPosition.x + settings.viewDistanceInChunks >= chunkPosition.x &&
                   playerChunkPosition.z - settings.viewDistanceInChunks <= chunkPosition.z &&
                   playerChunkPosition.z + settings.viewDistanceInChunks >= chunkPosition.z;
        }

        private void SetChunkNeighbors(Chunk chunk, Dictionary<Vector3Int, Chunk> chunkDict)
        {
            chunkDict.TryGetValue(chunk.Position + Vector3Int.left, out chunk.LeftChunk);
            chunkDict.TryGetValue(chunk.Position + Vector3Int.right, out chunk.RightChunk);
            chunkDict.TryGetValue(chunk.Position + Vector3Int.up, out chunk.UpChunk);
            chunkDict.TryGetValue(chunk.Position + Vector3Int.down, out chunk.DownChunk);
            chunkDict.TryGetValue(chunk.Position + Vector3Int.forward, out chunk.FrontChunk);
            chunkDict.TryGetValue(chunk.Position + Vector3Int.back, out chunk.BackChunk);
        }

        #endregion

        #region Block and Inventory Management

        public Block GetBlockAtPosition(Vector3 blockPosition)
        {
            var blockWorldPosition = Vector3Int.FloorToInt(blockPosition);
            var chunkPosition = GetChunkContainBlock(Vector3Int.FloorToInt(blockPosition));

            int blockId = 0;
            if (Chunks.TryGetValue(chunkPosition, out var chunk))
            {
                var chunkOrigin = new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z) * ChunkWidth;
                var blockChunkPosition = blockWorldPosition - chunkOrigin;
                blockId = chunk.Blocks[blockChunkPosition.x, blockChunkPosition.y, blockChunkPosition.z];
            }

            if (blockId == 0)
            {
                if (FloraChunks.TryGetValue(chunkPosition, out chunk))
                {
                    var chunkOrigin = new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z) * ChunkWidth;
                    var blockChunkPosition = blockWorldPosition - chunkOrigin;
                    blockId = chunk.Blocks[blockChunkPosition.x, blockChunkPosition.y, blockChunkPosition.z];
                }
            }

            return ResourceLoader.Instance.GetBlock(blockId);
        }

        public Vector3Int GetChunkContainBlock(Vector3Int blockWorldPosition)
        {
            var chunkPosition = new Vector3Int(
                blockWorldPosition.x / ChunkWidth,
                blockWorldPosition.y / ChunkHeight,
                blockWorldPosition.z / ChunkWidth);

            if (blockWorldPosition.x < 0 && blockWorldPosition.x % ChunkWidth != 0)
                chunkPosition.x--;
            if (blockWorldPosition.z < 0 && blockWorldPosition.z % ChunkWidth != 0)
                chunkPosition.z--;

            return chunkPosition;
        }

        public void DestroyBlock(Vector3 blockPosition)
        {
            var blockWorldPosition = Vector3Int.FloorToInt(blockPosition);
            NetworkManager.Instance.SendBlockDestroyed(blockWorldPosition);
        }

        public void SpawnBlock(Vector3 blockPosition, int blockType)
        {
            if (blockType == 0) return;
            var blockWorldPosition = Vector3Int.FloorToInt(blockPosition);
            NetworkManager.Instance.SendBlockPlaced(blockWorldPosition, blockType);
        }

        public void UpdateBlock(Vector3Int position, int newBlockType)
        {
            var blockWorldPosition = Vector3Int.FloorToInt(position);
            var chunkPosition = GetChunkContainBlock(blockWorldPosition);
            if (Chunks.TryGetValue(chunkPosition, out var chunk))
            {
                var chunkOrigin = chunkPosition * ChunkWidth;
                var blockChunkPosition = blockWorldPosition - chunkOrigin;
                var prevBlock = chunk.Renderer.SpawnBlock(blockChunkPosition, newBlockType);
                if (prevBlock != 0)
                {
                    DropItem(blockWorldPosition,
                        ResourceLoader.Instance.GetItem(ResourceLoader.Instance.GetBlock(prevBlock).DropItem), 1);
                }
            }
        }

        public void UpdateFloraBlock(Vector3Int position, int newBlockType)
        {
            var blockWorldPosition = Vector3Int.FloorToInt(position);
            var chunkPosition = GetChunkContainBlock(blockWorldPosition);
            if (FloraChunks.TryGetValue(chunkPosition, out var chunk))
            {
                var chunkOrigin = chunkPosition * ChunkWidth;
                var blockChunkPosition = blockWorldPosition - chunkOrigin;
                var prevBlock = chunk.Renderer.SpawnBlock(blockChunkPosition, newBlockType);
                if (prevBlock != 0)
                {
                    DropItem(blockWorldPosition,
                        ResourceLoader.Instance.GetItem(ResourceLoader.Instance.GetBlock(prevBlock).DropItem), 1);
                }
            }
        }

        public void DropItem(Vector3 position, Item item, int amount)
        {
            var droppedItem = Instantiate(droppedItemPrefab, position + new Vector3(0.5f, 0.5f, 0.5f),
                Quaternion.identity);
            droppedItem.Item = new ItemInSlot(item, amount);
            droppedItem.Init();
        }

        public void GetInventory(Vector3 blockPosition)
        {
            NetworkManager.Instance.GetInventory(blockPosition);
        }

        public void UpdateChest(Vector3Int chestPosition, List<ItemInSlot> chestSlots)
        {
            NetworkManager.Instance.SetInventory(chestPosition, chestSlots);
        }

        #endregion
    }

    [Serializable]
    public class ServerSettings
    {
        [Header("Performance")] [Range(1, 64)] public int viewDistanceInChunks = 8;
        [Range(1, 64)] public int loadDistance = 16;
    }
}