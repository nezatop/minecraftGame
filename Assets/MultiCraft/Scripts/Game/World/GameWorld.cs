using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiCraft.Scripts.Game.World
{
    public class GameWorld: MonoBehaviour
    {
        public Dictionary<Vector2Int, ChunkData> ChunkDatas = new Dictionary<Vector2Int, ChunkData>();
        public ChunkRenderer ChunkPrefab;
        
        private Camera _mainCamera;
        
        void Start()
        {
            _mainCamera = Camera.main;
            
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    int xPos = x * ChunkRenderer.ChunkWidth;
                    int yPos = y * ChunkRenderer.ChunkWidth;
                    
                    ChunkData chunkData = new ChunkData();
                    chunkData.ChunkPosition = new Vector2Int(x, y);
                    chunkData.Blocks =
                        TerrainGenerator.GenerateTerrain(xPos, yPos);
                    ChunkDatas.Add(new Vector2Int(x, y), chunkData);
                    
                    var chunk = Instantiate(ChunkPrefab, new Vector3(xPos, 0, yPos), Quaternion.identity, transform);
                    chunk.ChunkData = chunkData;
                    chunk.ParentWorld = this;

                    chunkData.Renderer = chunk;
                }
            }
        }
        
        public void Update()
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
                    Vector3Int blockWorldPosisiton = Vector3Int.FloorToInt(blockCenter);
                    Vector2Int ChunkPosition = GetChunkContainBlock(blockWorldPosisiton);
                    if (ChunkDatas.TryGetValue(ChunkPosition, out ChunkData chunkData))
                    {
                        Vector3Int ChunkOrigin = new Vector3Int(ChunkPosition.x, 0, ChunkPosition.y) * ChunkRenderer.ChunkWidth;
                        if (isDestoying)
                        {
                            chunkData.Renderer.DestroyBlock(blockWorldPosisiton - ChunkOrigin);
                        }
                        else
                        {
                            
                            chunkData.Renderer.SpawnBlock(blockWorldPosisiton - ChunkOrigin, BlockType.Grass);
                        }
                    }
                }
            }
        }

        public Vector2Int GetChunkContainBlock(Vector3Int blockWorldPosisiton)
        {
            return new Vector2Int(blockWorldPosisiton.x/ChunkRenderer.ChunkWidth, blockWorldPosisiton.z/ChunkRenderer.ChunkWidth);
        }
    }
}