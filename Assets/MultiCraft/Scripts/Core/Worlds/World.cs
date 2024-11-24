using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MultiCraft.Scripts.Core.Blocks;
using MultiCraft.Scripts.Core.Chunks;
using MultiCraft.Scripts.Core.Worlds.Generators;
using MultiCraft.Scripts.UI;
using MultiCraft.Scripts.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MultiCraft.Scripts.Core.Worlds
{
    public class World : MonoBehaviour
    {
        public static World Instance { get; private set; }

        public string WorldName;
        public const int WorldSize = 1024;
        public const int WorldHeight = 1;

        public const int ChunkWidth = 16;
        public const int ChunkHeight = 256;
        
        public List<GameObject> Players = new List<GameObject>();
        private List<Vector3Int> _currentPlayersPosition = new List<Vector3Int>();

        public WorldGenerator worldGenerator;
        public ChunkRenderer chunkPrefab;

        public int Seed = 0;

        private Dictionary<Vector3Int, Chunk> _chunks = new Dictionary<Vector3Int, Chunk>();
        private readonly ConcurrentQueue<GeneratedMesh> _meshingResults = new ConcurrentQueue<GeneratedMesh>();

        private int _chunksInWorld = 1;

        private void Start()
        {
            Instance = this;

            if (Seed == 0)
                Seed = Random.Range(int.MinValue, int.MaxValue);
            worldGenerator.InitializeGenerators(Seed);
            ChunkRenderer.InitializeTriangles();
            Players.AddRange(GameObject.FindGameObjectsWithTag("Player"));
        }

        private void Update()
        {
            if (_meshingResults.Count == 0)
            {
                UiManager.Instance.CloseLoadingPanel();
            }
            
            CheckPlayers(true);
            
            if (_meshingResults.TryDequeue(out var mesh))
            {
                var xPos = mesh.Chunk.Position.x * ChunkWidth;
                var yPos = mesh.Chunk.Position.y * ChunkHeight;
                var zPos = mesh.Chunk.Position.z * ChunkWidth;
                var chunkObject = Instantiate(chunkPrefab, new Vector3(xPos, yPos, zPos), Quaternion.identity,
                    transform);
                chunkObject.Chunk = mesh.Chunk;
                chunkObject.name = "chunk " + _chunksInWorld.ToString();

                chunkObject.SetMesh(mesh);

                mesh.Chunk.Renderer = chunkObject;
                mesh.Chunk.State = ChunkState.Active;
                _chunksInWorld++;
            }
        }
        
        private void CheckPlayers(bool wait = true)
        {
            bool playerMoved = false;

            List<Vector3Int> playersPosition = new List<Vector3Int>();
            for (var i = 0; i < Players.Count; i++)
            {
                Vector3Int playerPosition = GetChunkContainBlock(Vector3Int.FloorToInt(Players[i].transform.position));
                playersPosition.Add(playerPosition);
            }

            if (_currentPlayersPosition.Count == 0)
            {
                _currentPlayersPosition = playersPosition;
                playerMoved = true;
            }

            for (var i = 0; i < Players.Count; i++)
            {
                var prevPplayer = _currentPlayersPosition[i];
                var player = playersPosition[i];
                if (prevPplayer.x != player.x || prevPplayer.z != player.z)
                    playerMoved = true;
            }

            if (playerMoved)
            {
                _currentPlayersPosition = playersPosition;
                foreach (var pos in _currentPlayersPosition)
                {
                    
                    StartCoroutine(Generate(new Vector2Int(pos.x, pos.z), 4));
                }
            }
        }


        public void SpawnBlock(Vector3 blockPosition, int blockType)
        {
            var blockWorldPosition = Vector3Int.FloorToInt(blockPosition);
            var chunkPosition = GetChunkContainBlock(Vector3Int.FloorToInt(blockWorldPosition));

            if (_chunks.TryGetValue(chunkPosition, out var chunk))
            {
                var chunkOrigin = new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z) * ChunkWidth;
                var blockChunkPosition = blockWorldPosition - chunkOrigin;
                chunk.Renderer.SpawnBlock(blockChunkPosition, blockType);
            }
        }

        public int DestroyBlock(Vector3 blockPosition)
        {
            var blockWorldPosition = Vector3Int.FloorToInt(blockPosition);
            var chunkPosition = GetChunkContainBlock(Vector3Int.FloorToInt(blockWorldPosition));

            if (_chunks.TryGetValue(chunkPosition, out var chunk))
            {
                var chunkOrigin = new Vector3Int(chunkPosition.x, chunkPosition.y, chunkPosition.z) * ChunkWidth;
                var blockChunkPosition = blockWorldPosition - chunkOrigin;

                return chunk.Renderer.DestroyBlock(blockChunkPosition);
            }

            return -1;
        }

        private Vector3Int GetChunkContainBlock(Vector3Int blockWorldPosition)
        {
            var chunkPosition = new Vector3Int(
                blockWorldPosition.x / ChunkWidth,
                blockWorldPosition.y / ChunkHeight,
                blockWorldPosition.z / ChunkWidth);


            if (blockWorldPosition.x < 0)
                if (blockWorldPosition.x % ChunkWidth != 0)
                    chunkPosition.x--;
            if (blockWorldPosition.z < 0)
                if (blockWorldPosition.z % ChunkWidth != 0)
                    chunkPosition.z--;

            return chunkPosition;
        }

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

            return BlockManager.GetBlock(blockId);
        }

        private IEnumerator Generate(Vector2Int center, int radius)
        {
            int loadRadius = radius + 1;

            List<Chunk> loadingChunks = new List<Chunk>();

            for (var x = center.x - loadRadius; x <= center.x + loadRadius; x++)
            {
                for (var z = center.y - loadRadius; z <= center.y + loadRadius; z++)
                {
                    for (var y = 0; y < WorldHeight; y++)
                    {
                        var chunkPosition = new Vector3Int(x, y, z);
                        if (Mathf.Abs(x) > WorldSize || Mathf.Abs(z) > WorldSize)
                            continue;

                        if (_chunks.ContainsKey(chunkPosition)) continue;

                        var chunk = GenerateChunk(chunkPosition);
                        loadingChunks.Add(chunk);

                        yield return null;
                    }
                }
            }

            while (loadingChunks.Any(c => c.State == ChunkState.Generating))
                yield return null;

            for (var x = center.x - radius;
                 x <= center.x + radius;
                 x++)
            {
                for (var z = center.y - radius;
                     z <= center.y + radius;
                     z++)
                {
                    for (var y = 0; y < WorldHeight; y++)
                    {
                        var chunkPosition = new Vector3Int(x, y, z);

                        if (Mathf.Abs(x) > WorldSize || Mathf.Abs(z) > WorldSize)
                            continue;

                        var chunk = _chunks[chunkPosition];
                        if (chunk.Renderer != null) continue;

                        SpawnChunk(chunk);

                        yield return null;
                    }
                }
            }
        }

        private Chunk GenerateChunk(Vector3Int chunkPosition)
        {
            var xPos = chunkPosition.x * ChunkWidth;
            var yPos = chunkPosition.y * ChunkHeight;
            var zPos = chunkPosition.z * ChunkWidth;

            var chunk = new Chunk
            {
                Position = chunkPosition,
                State = ChunkState.Generating
            };
            
            _chunks.Add(chunkPosition, chunk);
            chunk.Blocks = worldGenerator.Generate(xPos, yPos, zPos);
            chunk.State = ChunkState.Generated;

            return chunk;
        }

        private void SpawnChunk(Chunk chunk)
        {
            _chunks.TryGetValue(chunk.Position + Vector3Int.left, out chunk.LeftChunk);
            _chunks.TryGetValue(chunk.Position + Vector3Int.right, out chunk.RightChunk);
            _chunks.TryGetValue(chunk.Position + Vector3Int.up, out chunk.UpChunk);
            _chunks.TryGetValue(chunk.Position + Vector3Int.down, out chunk.DownChunk);
            _chunks.TryGetValue(chunk.Position + Vector3Int.forward, out chunk.FrontChunk);
            _chunks.TryGetValue(chunk.Position + Vector3Int.back, out chunk.BackChunk);

            chunk.State = ChunkState.MeshBuilding;
            var mesh = MeshBuilder.GenerateMesh(chunk);
            _meshingResults.Enqueue(mesh);
            chunk.State = ChunkState.Loaded;
        }
    }
}