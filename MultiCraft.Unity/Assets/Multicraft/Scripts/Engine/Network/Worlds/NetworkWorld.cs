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

        public static NetworkWorld instance { get; private set; }

        #endregion

        #region User and Chunk Settings

        [Header("User settings")] public ServerSettings settings;

        [Header("Chunk Settings")] public const int ChunkWidth = 16;
        private const int ChunkHeight = 256;

        #endregion

        #region Chunk Dictionaries

        [Header("Chunks Dictionary")] private Dictionary<Vector3Int, Chunk> _chunks;
        private Dictionary<Vector3Int, Chunk> _waterChunks;
        private Dictionary<Vector3Int, Chunk> _floraChunks;

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
            instance = this;
            StartCoroutine(InitializeWorld());
        }

        private IEnumerator InitializeWorld()
        {
            _chunks = new Dictionary<Vector3Int, Chunk>();
            _waterChunks = new Dictionary<Vector3Int, Chunk>();
            _floraChunks = new Dictionary<Vector3Int, Chunk>();

            ChunkRenderer.InitializeTriangles();
            DropItemRenderer.InitializeTriangles();
            HandRenderer.InitializeTriangles();

            yield return StartCoroutine(resourceLoader.Initialize());
        }

        public void Update()
        {
            if (player)
                StartCoroutine(CheckPlayer());

            SpawnChunks();
        }

        #endregion

        #region Chunk Spawning Methods

        private void SpawnChunks()
        {
            if (_meshingResults.TryDequeue(out var mesh))
                SpawnChunkRenderer(mesh, chunkPrefab);

            if (_meshingWaterResults.TryDequeue(out mesh))
                SpawnChunkRenderer(mesh, waterChunkPrefab);

            if (_meshingFloraResults.TryDequeue(out mesh))
                SpawnChunkRenderer(mesh, floraChunkPrefab);
        }

        private void SpawnChunkRenderer(GeneratedMesh mesh, ChunkRenderer prefab)
        {
            var xPos = mesh.Chunk.Position.x * ChunkWidth;
            var yPos = mesh.Chunk.Position.y * ChunkHeight;
            var zPos = mesh.Chunk.Position.z * ChunkWidth;

            var chunkObject = Instantiate(prefab, new Vector3(xPos, yPos, zPos), Quaternion.identity, transform);
            chunkObject.Chunk = mesh.Chunk;
            chunkObject.SetMesh(mesh);

            mesh.Chunk.Renderer = chunkObject;
            mesh.Chunk.State = ChunkState.Active;
        }

        public bool SpawnChunk(Vector3Int position, int[,,] blocks)
        {
            if (_chunks.ContainsKey(position)) return false;
            Chunk chunk = new Chunk()
            {
                Position = position,
                Blocks = blocks,
                State = ChunkState.Generated
            };

            _chunks.Add(position, chunk);

            return true;
        }

        public bool SpawnWaterChunk(Vector3Int position, int[,,] blocks)
        {
            if (_waterChunks.ContainsKey(position)) return false;
            Chunk chunk = new Chunk()
            {
                Position = position,
                Blocks = blocks,
                State = ChunkState.Generated
            };

            _waterChunks.Add(position, chunk);

            return true;
        }

        public bool SpawnFloraChunk(Vector3Int position, int[,,] blocks)
        {
            if (_floraChunks.ContainsKey(position)) return false;
            Chunk chunk = new Chunk()
            {
                Position = position,
                Blocks = blocks,
                State = ChunkState.Generated
            };

            _floraChunks.Add(position, chunk);

            return true;
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
                        if (_chunks.TryGetValue(chunkPos, out var chunk))
                        {
                            if (!chunk.Renderer && !NetworkManager.instance.ChunksToRender.Contains(chunkPos))
                                NetworkManager.instance.ChunksToRender.Enqueue(chunkPos);
                            continue;
                        }

                        if (!NetworkManager.instance.ChunksToGet.Contains(chunkPos))
                            NetworkManager.instance.ChunksToGet.Enqueue(chunkPos);
                    }
                }
            }

            yield return null;
        }

        #endregion

        #region Chunk Rendering Methods

        public void RenderChunks(Vector3Int position)
        {
            RenderChunk(position, _chunks, _meshingResults);
        }

        public void RenderWaterChunks(Vector3Int position)
        {
            RenderChunk(position, _waterChunks, _meshingWaterResults);
        }

        public void RenderFloraChunks(Vector3Int position)
        {
            RenderChunk(position, _floraChunks, _meshingFloraResults);
        }

        private void RenderChunk(Vector3Int position, Dictionary<Vector3Int, Chunk> chunkDict,
            ConcurrentQueue<GeneratedMesh> meshingQueue)
        {
            var playerChunkPosition = GetChunkContainBlock(currentPosition);
            if (chunkDict.TryGetValue(position, out var chunk))
            {
                if (IsWithinViewDistance(playerChunkPosition, chunk.Position))
                {
                    SetChunkNeighbors(chunk, chunkDict);

                    chunk.State = ChunkState.MeshBuilding;
                    var mesh = MeshBuilder.GenerateMesh(chunk);
                    chunk.State = ChunkState.Loaded;

                    meshingQueue.Enqueue(mesh);
                }
            }
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
            if (_chunks.TryGetValue(chunkPosition, out var chunk))
            {
                var chunkOrigin = new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z) * ChunkWidth;
                var blockChunkPosition = blockWorldPosition - chunkOrigin;
                blockId = chunk.Blocks[blockChunkPosition.x, blockChunkPosition.y, blockChunkPosition.z];
            }

            if (blockId == 0)
            {
                if (_floraChunks.TryGetValue(chunkPosition, out chunk))
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
            NetworkManager.instance.SendBlockDestroyed(blockWorldPosition);
        }

        public void SpawnBlock(Vector3 blockPosition, int blockType)
        {
            if (blockType == 0) return;
            var blockWorldPosition = Vector3Int.FloorToInt(blockPosition);
            NetworkManager.instance.SendBlockPlaced(blockWorldPosition, blockType);
        }

        public void UpdateBlock(Vector3Int position, int newBlockType)
        {
            var blockWorldPosition = Vector3Int.FloorToInt(position);
            var chunkPosition = GetChunkContainBlock(blockWorldPosition);
            if (_chunks.TryGetValue(chunkPosition, out var chunk))
            {
                var chunkOrigin = chunkPosition * ChunkWidth;
                var blockChunkPosition = blockWorldPosition - chunkOrigin;
                var prevBlock = chunk.Renderer.SpawnBlock(blockChunkPosition, newBlockType);
                if (prevBlock != 0)
                {
                    DropItem(blockWorldPosition,ResourceLoader.Instance.GetItem(ResourceLoader.Instance.GetBlock(prevBlock).DropItem), 1);
                }
            }
        }
        
        public void UpdateFloraBlock(Vector3Int position, int newBlockType)
        {
            var blockWorldPosition = Vector3Int.FloorToInt(position);
            var chunkPosition = GetChunkContainBlock(blockWorldPosition);
            if (_floraChunks.TryGetValue(chunkPosition, out var chunk))
            {
                var chunkOrigin = chunkPosition * ChunkWidth;
                var blockChunkPosition = blockWorldPosition - chunkOrigin;
                var prevBlock = chunk.Renderer.SpawnBlock(blockChunkPosition, newBlockType);
                if (prevBlock != 0)
                {
                    DropItem(blockWorldPosition,ResourceLoader.Instance.GetItem(ResourceLoader.Instance.GetBlock(prevBlock).DropItem), 1);
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
            NetworkManager.instance.GetInventory(blockPosition);
        }

        public void UpdateChest(Vector3Int chestPosition, List<ItemInSlot> chestSlots)
        {
            NetworkManager.instance.SetInventory(chestPosition, chestSlots);
        }

        #endregion
    }

    [Serializable]
    public class ServerSettings
    {
        [Header("Performance")] [Range(1, 16)] public int viewDistanceInChunks = 8;
        [Range(1, 32)] public int loadDistance = 16;
    }
}