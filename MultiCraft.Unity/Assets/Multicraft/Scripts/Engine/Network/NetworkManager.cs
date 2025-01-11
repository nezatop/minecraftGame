using UnityEngine;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Multicraft.Scripts.Engine.Core.Hunger;
using MultiCraft.Scripts.Engine.Core.Inventories;
using MultiCraft.Scripts.Engine.Core.Player;
using MultiCraft.Scripts.Engine.Network.Player;
using MultiCraft.Scripts.Engine.Network.Worlds;
using MultiCraft.Scripts.Engine.UI;
using MultiCraft.Scripts.Engine.Utils;
using MultiCraft.Scripts.UI.Authorize;
using UnityEngine.SceneManagement;
using UnityWebSocket;

namespace MultiCraft.Scripts.Engine.Network
{
    public class NetworkManager : MonoBehaviour
    {
        private static readonly int VelocityX = Animator.StringToHash("VelocityX");
        private static readonly int VelocityY = Animator.StringToHash("VelocityY");
        private static readonly int VelocityZ = Animator.StringToHash("VelocityZ");

        #region Parametrs

        public static NetworkManager Instance { get; private set; }

        [Header("Debug Settings")] public bool enableLogging = true;

        public GameObject playerPrefab;
        public OtherNetPlayer otherPlayerPrefab;
        public GameObject animalPrefab;
        private Dictionary<string, OtherNetPlayer> _otherPlayers;
        private Dictionary<string, GameObject> _animals;

        public string serverAddress = "wss://ms-mult.onrender.com";

        private WebSocket _webSocket;

        public string playerName;
        private string _playerPassword;

        private GameObject _player;
        private PlayerController _playerController;
        private Vector3 _playerPosition;

        public ConcurrentQueue<Vector3Int> ChunksToRender;
        public ConcurrentQueue<Vector3Int> ChunksToGet;

        public bool canSpawnPlayer;

        public VariableJoystick moveJoystick;
        public VariableJoystick cameraJoystick;

        private static bool IsMobile => SystemInfo.deviceType == DeviceType.Handheld;

        private Vector3 _startPosition;
        
        #endregion

        #region Initialization

        private void Start()
        {
            Instance = this;

            ChunksToRender = new ConcurrentQueue<Vector3Int>();
            ChunksToGet = new ConcurrentQueue<Vector3Int>();

            _otherPlayers = new Dictionary<string, OtherNetPlayer>();
            _animals = new Dictionary<string, GameObject>();

            if (PlayerPrefs.HasKey("UserData"))
            {
                string jsonData = PlayerPrefs.GetString("UserData");
                var userData = JsonUtility.FromJson<UserData>(jsonData);
                playerName = userData.username;
                _playerPassword = userData.password;
            }
            else
            {
                playerName = Guid.NewGuid().ToString();
                _playerPassword = Guid.NewGuid().ToString();
            }

            _webSocket = new WebSocket(serverAddress);

            _webSocket.OnOpen += OnOpen;
            _webSocket.OnClose += OnClose;
            _webSocket.OnMessage += OnMessage;
            _webSocket.OnError += OnError;

            _webSocket.ConnectAsync();
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            LogError($"[Client] Error: {e.Message}");
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.RawData);
            HandleServerMessage(message);
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            SceneManager.LoadScene("MainMenu");
            LogDebug($"[Client] Connection closed. Reason: {e}");
        }

        private void OnOpen(object sender, OpenEventArgs e)
        {
            LogDebug("[Client] WebSocket connection opened.");
            SendMessageToServer(new { type = "connect", login = playerName, password = _playerPassword });
        }

        private void OnApplicationQuit()
        {
            _webSocket?.CloseAsync();
        }

        #endregion

        private void Update()
        {
            if (ChunksToGet.TryDequeue(out Vector3Int chunkPosition))
            {
                RequestChunk(chunkPosition);
            }

            if (ChunksToGet.Count > 0) return;
            if (NetworkWorld.instance.ChunksLoaded < (NetworkWorld.instance.settings.viewDistanceInChunks * 2 + 1) *
                (NetworkWorld.instance.settings.viewDistanceInChunks * 2 + 1))
                return;

            if (ChunksToRender.TryDequeue(out chunkPosition))
            {
                NetworkWorld.instance.RenderChunks(chunkPosition);
                NetworkWorld.instance.RenderWaterChunks(chunkPosition);
                NetworkWorld.instance.RenderFloraChunks(chunkPosition);
            }

            if (!_player && canSpawnPlayer && ChunksToRender.Count <= 0)
            {
                OnAllChunksLoaded();
            }
        }

        private void OnDestroy()
        {
            _playerController.health.OnDeath -= RespawnPlayer;
        }

        #region HandleServerMessage

        private void HandleServerMessage(string data)
        {
            try
            {
                var message = JsonDocument.Parse(data);
                var type = message.RootElement.GetProperty("type").GetString();
               
                LogDebug($"[MassageFromServer] {message.RootElement.ToString()}");
                switch (type)
                {
                    case "connected":
                        OnConnected(message.RootElement);
                        SendMessageToServer(new { type = "get_players" });
                        break;

                    case "damage":
                        HandleDamage(message.RootElement);
                        break;

                    case "player_connected":
                        OnPlayerConnected(message.RootElement);
                        break;

                    case "player_moved":
                        OnPlayerMoved(message.RootElement);
                        break;

                    case "player_disconnected":
                        OnPlayerDisconnected(message.RootElement);
                        break;

                    case "players_list":
                        OnPlayersListReceived(message.RootElement);
                        break;

                    case "chunk_data":
                        HandleChunkData(message.RootElement);
                        break;

                    case "player_update":
                        HandlePlayerUpdate(message.RootElement);
                        break;

                    case "block_update":
                        HandleBlockUpdate(message.RootElement);
                        break;

                    case "inventory":
                        HandleInventoryGet(message.RootElement);
                        break;

                    case "chat":
                        HandleChat(message.RootElement);
                        break;

                    case "entities":
                        HandleGetEntities(message.RootElement);
                        break;

                    default:
                        LogWarning($"[Client] Unknown message type: {type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError($"[Client] Exception while handling server message: {ex}");
            }
        }

        #endregion

        #region Handleplayers

        private void OnConnected(JsonElement data)
        {
            Vector3 position = JsonToVector3Safe(data, "position");
            _playerPosition = position;
            NetworkWorld.instance.currentPosition = Vector3Int.FloorToInt(_playerPosition);
            RequestChunksAtStart(position);
        }

        private void OnPlayerConnected(JsonElement data)
        {
            string playerId = data.GetProperty("player_id").GetString();
            Vector3 position = JsonToVector3(data.GetProperty("position"));

            UiManager.Instance.ChatWindow.commandReader.PrintLog($"{playerId}: Зашел на сервер");

            if (playerId != null && !_otherPlayers.ContainsKey(playerId) && playerId != playerName)
            {
                var playerObject = Instantiate(otherPlayerPrefab, position, Quaternion.identity);
                playerObject.playerName = playerId;
                playerObject.Init();
                _otherPlayers[playerId] = playerObject;
            }
        }

        private void OnPlayerMoved(JsonElement data)
        {
            var playerId = data.GetProperty("player_id").GetString();
            var targetPosition = JsonToVector3(data.GetProperty("position"));
            var targetRotation = JsonToVector3(data.GetProperty("rotation"));
            var velocity = JsonToVector3(data.GetProperty("velocity"));

            if (playerId == null || playerId == playerName ||
                !_otherPlayers.TryGetValue(playerId, out var player)) return;
            var playerAnimator = player.animator;
            var smoothSpeed = 0.1f;
            player.transform.position = targetPosition;

            var targetQuaternion = Quaternion.Euler(targetRotation);
            player.transform.rotation = Quaternion.Lerp(player.transform.rotation, targetQuaternion, smoothSpeed);

            playerAnimator.SetFloat(VelocityX, velocity.x);
            playerAnimator.SetFloat(VelocityY, velocity.y);
            playerAnimator.SetFloat(VelocityZ, velocity.z);
        }


        private void OnPlayerDisconnected(JsonElement data)
        {
            var playerId = data.GetProperty("player_id").GetString();

            UiManager.Instance.ChatWindow.commandReader.PrintLog($"{playerId}: Вышел с сервера");
            if (playerId == null || !_otherPlayers.TryGetValue(playerId, out var player)) return;
            Destroy(player);
            _otherPlayers.Remove(playerId);
        }

        private void HandlePlayerUpdate(JsonElement data)
        {
            string playerId = data.GetProperty("player_id").GetString();
            Vector3 position = JsonToVector3(data.GetProperty("position"));

            if (playerId != null && _otherPlayers.TryGetValue(playerId, out OtherNetPlayer player))
            {
                player.transform.position = position;
            }
            else
            {
                var playerObject = Instantiate(otherPlayerPrefab, position, Quaternion.identity);
                playerObject.playerName = playerId;
                playerObject.Init();
                if (playerId != null) _otherPlayers[playerId] = playerObject;
            }
        }

        private void OnPlayersListReceived(JsonElement data)
        {
            var players = data.GetProperty("players");

            foreach (JsonElement player in players.EnumerateArray())
            {
                string playerId = player.GetProperty("player_id").GetString();
                Vector3 position = JsonToVector3(player.GetProperty("position"));

                if (playerId != null && !_otherPlayers.ContainsKey(playerId) && playerId != playerName)
                {
                    var playerObject = Instantiate(otherPlayerPrefab, position, Quaternion.identity);
                    playerObject.playerName = playerId;
                    playerObject.Init();
                    _otherPlayers[playerId] = playerObject;
                }
            }
        }

        private void HandleBlockUpdate(JsonElement data)
        {
            Vector3Int position = JsonToVector3Int(data.GetProperty("position"));
            int newBlockType = data.GetProperty("block_type").GetInt32();

            var type = data.GetProperty("chunk").ToString();

            if (type == "Flora")
                NetworkWorld.instance.UpdateFloraBlock(position, newBlockType);
            else
                NetworkWorld.instance.UpdateBlock(position, newBlockType);
        }

        private void HandleDamage(JsonElement data)
        {
            var damage = data.GetProperty("damage").GetInt32();
            if(_playerController) _playerController.health.TakeDamage(damage);
        }

        private IEnumerator SendPlayerPositionRepeatedly()
        {
            while (true)
            {
                ServerMassageMove(_playerController);
                yield return null;
            }
            // ReSharper disable once IteratorNeverReturns
        }

        #endregion

        #region Chat

        private void HandleChat(JsonElement data)
        {
            string playerId = data.GetProperty("player_id").GetString();
            string massage = data.GetProperty("chat_massage").GetString();

            if (playerId != null && playerId != playerName)
            {
                UiManager.Instance.ChatWindow.commandReader.PrintLog($"{playerId}: {massage}");
            }
        }

        #endregion

        #region Entities

        private void HandleGetEntities(JsonElement data)
        {
            if (!_player || !canSpawnPlayer || ChunksToRender.Count > 0) return;
            if (data.TryGetProperty("entities_list", out JsonElement entitiesElement) &&
                entitiesElement.ValueKind == JsonValueKind.Array)
            {
                var entities = new List<Entity>();

                foreach (var entityElement in entitiesElement.EnumerateArray())
                {
                    try
                    {
                        Entity entity = new Entity
                        {
                            ID = entityElement.GetProperty("id").GetString(),
                            Position = JsonToVector3(entityElement.GetProperty("position"))
                        };
                        entities.Add(entity);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Ошибка десериализации сущности: {ex.Message}");
                    }
                }

                foreach (var entity in entities)
                {
                    if (_animals.ContainsKey(entity.ID)) continue;
                    if (!(_playerPosition.x -
                          NetworkWorld.instance.settings.viewDistanceInChunks * NetworkWorld.ChunkWidth <=
                          entity.Position.x &&
                          _playerPosition.x + NetworkWorld.instance.settings.viewDistanceInChunks *
                          NetworkWorld.ChunkWidth >= entity.Position.x &&
                          _playerPosition.z - NetworkWorld.instance.settings.viewDistanceInChunks *
                          NetworkWorld.ChunkWidth <= entity.Position.z &&
                          _playerPosition.z + NetworkWorld.instance.settings.viewDistanceInChunks *
                          NetworkWorld.ChunkWidth >= entity.Position.z)) continue;
                    var animal = Instantiate(animalPrefab,
                        entity.Position + new Vector3(
                            entity.Position.x >= 0 ? 0.5f : -0.5f,
                            0.5f,
                            entity.Position.z >= 0 ? 0.5f : -0.5f),
                        Quaternion.Euler(-90, 0, 0));
                    _animals.Add(entity.ID, animal);
                }
            }
            else
            {
                LogDebug("Неверный формат данных о сущностях.");
            }
        }

        #endregion

        #region HandleChunks

        private void HandleChunkData(JsonElement data)
        {
            Vector3Int chunkCoord = JsonToVector3Int(data.GetProperty("position"));

            JsonElement blocksJson = data.GetProperty("blocks");
            JsonElement waterBlocksJson = data.GetProperty("waterChunk");
            JsonElement floraBlocksJson = data.GetProperty("floraChunk");

            NetworkWorld.instance.SpawnChunk(chunkCoord, Blocks(blocksJson));
            NetworkWorld.instance.SpawnWaterChunk(chunkCoord, Blocks(waterBlocksJson));
            NetworkWorld.instance.SpawnFloraChunk(chunkCoord, Blocks(floraBlocksJson));

            if (_player)
                _playerPosition = _player.transform.position;

            var playerChunkPosition =
                NetworkWorld.instance.GetChunkContainBlock(Vector3Int.FloorToInt(_playerPosition));
            if (playerChunkPosition.x - NetworkWorld.instance.settings.viewDistanceInChunks <= chunkCoord.x &&
                playerChunkPosition.x + NetworkWorld.instance.settings.viewDistanceInChunks >= chunkCoord.x &&
                playerChunkPosition.z - NetworkWorld.instance.settings.viewDistanceInChunks <= chunkCoord.z &&
                playerChunkPosition.z + NetworkWorld.instance.settings.viewDistanceInChunks >= chunkCoord.z)
                ChunksToRender.Enqueue(chunkCoord);
        }

        private int[,,] Blocks(JsonElement blocksJson)
        {
            var flatBlocks = new int[16 * 16 * 256];
            var blocks = new int[16, 256, 16];

            var index = 0;
            foreach (var blockValue in blocksJson.EnumerateArray())
            {
                flatBlocks[index] = blockValue.GetInt32();
                index++;
            }

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        int flatIndex = x + z * 16 + y * 16 * 16;
                        blocks[x, y, z] = flatBlocks[flatIndex];
                    }
                }
            }

            return blocks;
        }

        private void RequestChunksAtStart(Vector3 position)
        {
            Vector3Int playerChunkPosition = new Vector3Int(
                Mathf.FloorToInt(position.x / 16),
                0,
                Mathf.FloorToInt(position.z / 16)
            );

            int loadDistance = NetworkWorld.instance.settings.loadDistance;

            for (int x = -loadDistance; x <= loadDistance; x++)
            {
                for (int z = -loadDistance; z <= loadDistance; z++)
                {
                    ChunksToGet.Enqueue(new Vector3Int(
                        playerChunkPosition.x + x,
                        0,
                        playerChunkPosition.z + z
                    ));
                }
            }

            canSpawnPlayer = true;
        }

        private void RequestChunk(Vector3Int chunkPosition)
        {
            SendMessageToServer(new
            {
                type = "get_chunk",
                position = new { chunkPosition.x, chunkPosition.y, chunkPosition.z }
            });
        }

        private void OnAllChunksLoaded()
        {
            if (_player) return;

            _startPosition = _playerPosition;
            
            _player = Instantiate(playerPrefab, _playerPosition + Vector3.up * 2, Quaternion.identity);
            _playerController = _player.GetComponent<PlayerController>();
            
            _playerController.variableJoystick = moveJoystick;
            _playerController.isMobile = IsMobile;

            _playerController.cameraController.variableJoystick = cameraJoystick;
            _playerController.cameraController.isMobile = IsMobile;

            moveJoystick.gameObject.SetActive(IsMobile);
            cameraJoystick.gameObject.SetActive(IsMobile);

            NetworkWorld.instance.player = _player;

            _playerController.health.OnDeath += RespawnPlayer;

            UiManager.Instance.PlayerController = _playerController;
            UiManager.Instance.Initialize();
            UiManager.Instance.CloseLoadingScreen();

            foreach (var otherPlayers in _otherPlayers.Values)
            {
                otherPlayers.cameraTransform = _player.GetComponentInChildren<Camera>().transform;
            }
            
            StartCoroutine(SendPlayerPositionRepeatedly());
        }

        private void RespawnPlayer()
        {
            _player.transform.position = _startPosition;
            _playerController.health.health = _playerController.health.maxHealth;
            _playerController.GetComponent<HungerSystem>().hunger = _playerController.GetComponent<HungerSystem>().maxHunger;
        }
        
        #endregion

        #region Inventory

        private void HandleInventoryGet(JsonElement data)
        {
            var slots = JsonToInventory(data.GetProperty("inventory"));

            UiManager.Instance.OpenCloseChest(slots,
                Vector3Int.FloorToInt(JsonToVector3(data.GetProperty("position"))));
        }

        public void GetInventory(Vector3 chestPosition)
        {
            SendMessageToServer(new
            {
                type = "get_inventory",
                position = new { chestPosition.x, chestPosition.y, chestPosition.z }
            });
        }

        public void SetInventory(Vector3Int chestPosition, List<ItemInSlot> slots)
        {
            var slotsJson = JsonToInventory(slots);

            SendMessageToServer(new
            {
                type = "set_inventory",
                position = new { chestPosition.x, chestPosition.y, chestPosition.z },
                inventory = slotsJson
            });
        }

        #endregion

        #region Utils

        private Vector3Int JsonToVector3Int(JsonElement json)
        {
            return new Vector3Int(
                json.GetProperty("x").GetInt32(),
                json.GetProperty("y").GetInt32(),
                json.GetProperty("z").GetInt32()
            );
        }

        private string JsonToInventory(List<ItemInSlot> slots)
        {
            List<ItemJson> slotsJson = new List<ItemJson>();

            foreach (var slot in slots)
            {
                var item = new ItemJson();
                if (slot == null)
                {
                    item.Type = "null";
                    item.Count = 0;
                    item.Durability = 0;
                }
                else
                {
                    item = new ItemJson()
                    {
                        Type = slot.Item.Name,
                        Count = slot.Amount,
                        Durability = slot.Durability,
                    };
                }

                slotsJson.Add(item);
            }

            return JsonSerializer.Serialize(slotsJson);
        }


        private List<ItemInSlot> JsonToInventory(JsonElement json)
        {
            List<ItemInSlot> inventory = new List<ItemInSlot>();

            List<ItemJson> items = json.Deserialize<List<ItemJson>>();

            foreach (var itemJson in items)
            {
                var item = new ItemInSlot
                {
                    Amount = itemJson.Count,
                    Durability = itemJson.Durability,
                    Item = itemJson.Type != "null" ? ResourceLoader.Instance.GetItem(itemJson.Type) : null
                };

                inventory.Add(item);
            }

            return inventory;
        }

        private Vector3 JsonToVector3(JsonElement json)
        {
            float x = json.GetProperty("x").GetSingle();
            float y = json.GetProperty("y").GetSingle();
            float z = json.GetProperty("z").GetSingle();
            return new Vector3(x, y, z);
        }

        private Vector3 JsonToVector3Safe(JsonElement data, string propertyName)
        {
            if (data.TryGetProperty(propertyName, out JsonElement positionElement))
            {
                return JsonToVector3(positionElement);
            }

            Debug.LogWarning($"[Client] Property '{propertyName}' not found. Defaulting position to (0, 0, 0).");
            return Vector3.zero;
        }

        #endregion

        #region SendToServer

        public void SendMessageToServerChat(string massage)
        {
            SendMessageToServer(new
            {
                type = "chat",
                player = playerName,
                chat_massage = massage
            });
        }

        public void SendBlockPlaced(Vector3 position, int blockType)
        {
            SendMessageToServer(new
            {
                type = "place_block",
                position = new
                {
                    position.x,
                    position.y,
                    position.z
                },
                block_type = blockType
            });
        }

        public void ServerMassageAttack(int damage, string attackTarget)
        {
            SendMessageToServer(new
            {
                type = "Attack",
                player = playerName,
                attack_target = attackTarget,
                damage,
            });
        }

        public void SendBlockDestroyed(Vector3 position)
        {
            SendMessageToServer(new
            {
                type = "destroy_block",
                position = new
                {
                    position.x,
                    position.y,
                    position.z
                },
            });
        }

        private void SendMessageToServer(object message)
        {
            string jsonMessage = JsonSerializer.Serialize(message);
            _webSocket.SendAsync(Encoding.UTF8.GetBytes(jsonMessage));
        }

        private void ServerMassageMove(PlayerController player)
        {
            if(!player)return;
            SendMessageToServer(new
            {
                type = "move",
                player = playerName,
                position = new
                {
                    player.transform.position.x,
                    player.transform.position.y,
                    player.transform.position.z
                },
                rotation = new
                {
                    player.transform.rotation.eulerAngles.x,
                    player.transform.rotation.eulerAngles.y,
                    player.transform.rotation.eulerAngles.z
                },
                velocity = new
                {
                    player.controller.velocity.x,
                    player.controller.velocity.y,
                    player.controller.velocity.z,
                }
            });
        }

        #endregion

        #region Logs

        private void LogDebug(string message)
        {
            if (enableLogging)
            {
                Debug.Log(message);
            }
        }

        private void LogWarning(string message)
        {
            if (enableLogging)
            {
                Debug.LogWarning(message);
            }
        }

        private void LogError(string message)
        {
            if (enableLogging)
            {
                Debug.LogError(message);
            }
        }

        #endregion
    }

    public class ItemJson
    {
        public string Type { get; set; }
        public int Count { get; set; }
        public int Durability { get; set; }
    }

    public class Entity
    {
        public string ID { get; set; }
        public Vector3 Position { get; set; }
    }
}