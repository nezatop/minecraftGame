using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MultiCraft.Scripts.Game.Blocks;
using MultiCraft.Scripts.Game.Chunks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MultiCraft.Scripts.Game.World
{
    public class GameWorld : MonoBehaviour
    {
        [Header("Game World settings")] public int WorldSize = 1024;
        public const int ChunkWidth = 16;
        public const int ChunkHeight = 256;

        public WorldGenerator WorldGenerator;

        [Header("Server settings")] public int ViewDistance = 5;
        public int MaxViewDistance = 10;
        public int MaxPlayers = 99;

        [Header("Generating settings")] public int Seed = 0;
        public ChunkRenderer ChunkPrefab;


        public List<GameObject> Players = new List<GameObject>();
        private List<Vector3Int> _currentPlayersPosition = new List<Vector3Int>();

        public Dictionary<Vector3Int, Chunk> Chunks = new Dictionary<Vector3Int, Chunk>();

        private ConcurrentQueue<GeneratedMesh> _meshingResults = new ConcurrentQueue<GeneratedMesh>();

        private IEnumerator Generate(bool wait)
        {
            Debug.Log("Generating world...");
            int loadRadius = ViewDistance + 1;
            var players = _currentPlayersPosition;

            List<Chunk> loadingChunks = new List<Chunk>();
            foreach (var playerPosition in players)
            {
                for (var x = playerPosition.x - loadRadius; x <= playerPosition.x + loadRadius; x++)
                {
                    for (var z = playerPosition.z - loadRadius; z <= playerPosition.z + loadRadius; z++)
                    {
                        var chunkPosition = new Vector3Int(x, 0, z);

                        if (Mathf.Abs(x) > WorldSize || Mathf.Abs(z) > WorldSize)
                        {
                            continue;
                        }

                        if (Chunks.ContainsKey(chunkPosition)) continue;

                        var chunk = GenerateChunk(chunkPosition);
                        loadingChunks.Add(chunk);

                        if (wait) yield return null;
                    }
                }

                while (loadingChunks.Any(c => c.State == ChunkState.Generating))
                {
                    yield return null;
                }

                for (var x = playerPosition.x - ViewDistance; x <= playerPosition.x + ViewDistance; x++)
                {
                    for (var z = playerPosition.z - ViewDistance; z <= playerPosition.z + ViewDistance; z++)
                    {
                        var chunkPosition = new Vector3Int(x, 0, z);

                        if (Mathf.Abs(x) > WorldSize || Mathf.Abs(z) > WorldSize)
                        {
                            continue;
                        }

                        Chunk chunk = Chunks[chunkPosition];
                        if (chunk.Renderer != null) continue;
                        SpawnChunk(chunk);
                        if (wait) yield return null;
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
            Chunks.Add(chunkPosition, chunk);

            Task.Factory.StartNew(() =>
            {
                try
                {
                    chunk.Blocks = WorldGenerator.GenerateWorld(xPos, yPos, zPos, Seed);
                    chunk.State = ChunkState.Generated;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw;
                }
            });

            return chunk;
        }

        private void SpawnChunk(Chunk chunk)
        {
            Chunks.TryGetValue(chunk.Position + Vector3Int.left, out chunk.LeftChunk);
            Chunks.TryGetValue(chunk.Position + Vector3Int.right, out chunk.RightChunk);
            Chunks.TryGetValue(chunk.Position + Vector3Int.up, out chunk.UpChunk);
            Chunks.TryGetValue(chunk.Position + Vector3Int.down, out chunk.DownChunk);
            Chunks.TryGetValue(chunk.Position + Vector3Int.forward, out chunk.FrontChunk);
            Chunks.TryGetValue(chunk.Position + Vector3Int.back, out chunk.BackChunk);

            chunk.State = ChunkState.MeshBuilding;

            Task.Factory.StartNew(() =>
            {
                GeneratedMesh mesh = MeshBuilder.GenerateMesh(chunk);
                _meshingResults.Enqueue(mesh);
                chunk.State = ChunkState.Loaded;
            });
        }

        private void Start()
        {
            ChunkRenderer.InitializeTriangles();
            WorldGenerator.InitializeGenerators();
            Players.AddRange(GameObject.FindGameObjectsWithTag("Player"));
            Seed = Random.Range(int.MinValue, int.MaxValue);

            BlockDataBase.InitializeBlockDataBase();
            CheckPlayers(false);
        }

        private void Update()
        {
            CheckPlayers(true);

            if (_meshingResults.TryDequeue(out GeneratedMesh mesh))
            {
                var xPos = mesh.Chunk.Position.x * ChunkWidth;
                var yPos = mesh.Chunk.Position.y * ChunkHeight;
                var zPos = mesh.Chunk.Position.z * ChunkWidth;
                var chunkObject = Instantiate(ChunkPrefab, new Vector3(xPos, yPos, zPos), Quaternion.identity,
                    transform);
                chunkObject.Chunk = mesh.Chunk;
                chunkObject.ParentWorld = this;

                chunkObject.SetMesh(mesh);

                mesh.Chunk.Renderer = chunkObject;
                mesh.Chunk.State = ChunkState.Active;
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
                Debug.Log(_currentPlayersPosition);
                _currentPlayersPosition = playersPosition;
                StartCoroutine(Generate(wait));
            }
        }

        public bool SpawnBlock(Vector3 blockPosition, BlockType blockType)
        {
            var blockWorldPosition = Vector3Int.FloorToInt(blockPosition);
            var chunkPosition = GetChunkContainBlock(blockWorldPosition);
            if (Chunks.TryGetValue(chunkPosition, out var chunkData))
            {
                var chunkOrigin = new Vector3Int(chunkPosition.x, 0, chunkPosition.y) * ChunkWidth;
                chunkData.Renderer.SpawnBlock(chunkPosition - chunkOrigin, blockType);
            }
            else return false;

            return true;
        }

        public BlockType DestroyBlock(Vector3 blockPosition)
        {
            BlockType destroyedBlockType;

            var blockWorldPosition = Vector3Int.FloorToInt(blockPosition);
            var chunkPosition = GetChunkContainBlock(blockWorldPosition);
            if (Chunks.TryGetValue(chunkPosition, out var chunkData))
            {
                var chunkOrigin = new Vector3Int(chunkPosition.x, 0, chunkPosition.y) * ChunkWidth;
                destroyedBlockType = chunkData.Renderer.DestroyBlock(chunkPosition - chunkOrigin);
            }
            else return BlockType.Air; //TODO: Воздух заменить на BlockType.Unknown

            return destroyedBlockType;
        }

        private Vector3Int GetChunkContainBlock(Vector3Int blockWorldPosition)
        {
            var chunkPosition = new Vector3Int(
                blockWorldPosition.x / ChunkWidth,
                blockWorldPosition.y / ChunkHeight,
                blockWorldPosition.z / ChunkWidth);

            if (blockWorldPosition.x < 0) chunkPosition.x--;
            if (blockWorldPosition.y < 0) chunkPosition.y--;
            if (blockWorldPosition.z < 0) chunkPosition.z--;

            return chunkPosition;
        }
    }
}