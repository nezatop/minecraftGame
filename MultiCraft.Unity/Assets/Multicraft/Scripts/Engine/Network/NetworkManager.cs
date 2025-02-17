using UnityEngine;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
using YG;
using InteractController = MultiCraft.Scripts.Engine.Network.Player.InteractController;

namespace MultiCraft.Scripts.Engine.Network
{
    public class NetworkManager : MonoBehaviour
    {
        #region Parametrs

        private static readonly int VelocityX = Animator.StringToHash("VelocityX");
        private static readonly int VelocityY = Animator.StringToHash("VelocityY");
        private static readonly int VelocityZ = Animator.StringToHash("VelocityZ");

        public static NetworkManager Instance { get; private set; }

        [Header("Debug Settings")] public bool enableLogging = true;

        public GameObject playerPrefab;
        public OtherNetPlayer otherPlayerPrefab;
        public GameObject animalPrefab;
        private Dictionary<string, OtherNetPlayer> _otherPlayers;
        private Dictionary<string, GameObject> _animals;

        public string serverAddress = "ws://localhost:8080";

        private WebSocket _webSocket;

        public string playerName;
        private string _playerPassword;

        private GameObject _player;
        private PlayerController _playerController;
        private Vector3 _playerPosition;

        public ConcurrentQueue<Vector3Int> ChunksToRender;
        public ConcurrentQueue<Vector3Int> ChunksToGet;
        public HashSet<Vector3Int> RenderedChunks;
        public HashSet<Vector3Int> RequestedChunks;
        public HashSet<Vector3Int> SpawnedChunks;
        public HashSet<Vector3Int> GettedChunks;
        private int _requestedChunks = 0;

        public bool StartChunksLoaded = false;

        public bool canSpawnPlayer;

        public VariableJoystick moveJoystick;
        public VariableJoystick cameraJoystick;

        private static bool IsMobile => YG2.envir.isMobile;

        private Vector3 _startPosition;

        private float _targetTime = 0f;
        private const float SmoothSpeed = 2f;

        #endregion

        #region Initialization

        private void Start()
        {
            Instance = this;

            ChunksToRender = new ConcurrentQueue<Vector3Int>();
            RenderedChunks = new HashSet<Vector3Int>();
            ChunksToGet = new ConcurrentQueue<Vector3Int>();
            RequestedChunks = new HashSet<Vector3Int>();
            SpawnedChunks = new HashSet<Vector3Int>();
            GettedChunks = new HashSet<Vector3Int>();

            _otherPlayers = new Dictionary<string, OtherNetPlayer>();
            _animals = new Dictionary<string, GameObject>();

            if (YG2.player.auth == false)
            {
                playerName = Guid.NewGuid().ToString();
            }
            else
            {
                playerName = YG2.player.name;
            }

            _playerPassword = Guid.NewGuid().ToString();

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
            DisconnectPlayer();
            SceneManager.LoadScene("MainMenu");
            LogDebug($"[Client] Connection closed. Reason: {e.Reason}");
        }

        private void OnOpen(object sender, OpenEventArgs e)
        {
            LogDebug("[Client] WebSocket connection opened.");
            SendMessageToServer(new { type = "connect", login = playerName, password = _playerPassword });
        }

        private void OnApplicationQuit()
        {
            DisconnectPlayer();
            _webSocket?.CloseAsync();
        }

        void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        #endregion

        private void Update()
        {
            if (DayCycleManager.Instance != null)
            {
                if (DayCycleManager.Instance.TimeOfDay > _targetTime)
                    DayCycleManager.Instance.TimeOfDay -= 1;
                if (DayCycleManager.Instance.DayNight)
                    DayCycleManager.Instance.TimeOfDay = Mathf.Lerp(DayCycleManager.Instance.TimeOfDay, _targetTime,
                        Time.deltaTime * SmoothSpeed);
            }

            if (ChunksToGet.TryDequeue(out Vector3Int chunkPosition))
            {
                if (!RequestedChunks.Contains(chunkPosition))
                {
                    RequestChunk(chunkPosition);
                    RequestedChunks.Add(chunkPosition);
                }
            }

            if (ChunksToGet.Count > 0 && !_player) return;
            if (_requestedChunks > 0 && !_player) return;

            if (ChunksToRender.TryDequeue(out chunkPosition))
            {
                if (!RenderedChunks.Contains(chunkPosition))
                    if (SpawnedChunks.Contains(chunkPosition + Vector3Int.left) &&
                        SpawnedChunks.Contains(chunkPosition + Vector3Int.right) &&
                        SpawnedChunks.Contains(chunkPosition + Vector3Int.forward) &&
                        SpawnedChunks.Contains(chunkPosition + Vector3Int.back))
                    {
                        RenderedChunks.Add(chunkPosition);
                        StartCoroutine(NetworkWorld.Instance.RenderChunks(chunkPosition));
                        StartCoroutine(NetworkWorld.Instance.RenderWaterChunks(chunkPosition));
                        StartCoroutine(NetworkWorld.Instance.RenderFloraChunks(chunkPosition));
                    }
                    else
                    {
                        ChunksToRender.Enqueue(chunkPosition);
                    }
            }

            if (!_player)
                if (NetworkWorld.Instance.ChunksLoaded < (NetworkWorld.Instance.settings.viewDistanceInChunks * 2 + 1) *
                    (NetworkWorld.Instance.settings.viewDistanceInChunks * 2 + 1))
                    return;

            if (!_player && canSpawnPlayer && ChunksToRender.Count <= 0)
            {
                OnAllChunksLoaded();
            }
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
            if (_playerController)
                _playerController.health.OnDeath -= OpenDeadMenu;
            _webSocket.CloseAsync();
        }

        public IEnumerator DestroyChunk(Vector3Int chunkPosition)
        {
            RequestedChunks.Remove(chunkPosition);
            SpawnedChunks.Remove(chunkPosition);
            GettedChunks.Remove(chunkPosition);
            RenderedChunks.Remove(chunkPosition);
            yield return null;
        }

        #region HandleServerMessage

        private void HandleServerMessage(string data)
        {
            try
            {
                var message = JsonDocument.Parse(data);
                var type = message.RootElement.GetProperty("type").GetString();

                switch (type)
                {
                    case "connected":
                        OnConnected(message.RootElement);
                        SendMessageToServer(new { type = "get_players" });
                        break;

                    case "time":
                        HandleTime(message.RootElement);
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

                    case "player_dead":
                        OnPlayerDead(message.RootElement);
                        break;

                    case "Player_respawn":
                        OnPlayerRespawn(message.RootElement);
                        break;

                    case "chunk_data":
                        StartCoroutine(HandleChunkData(message.RootElement));
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

                    case "drop_inventory":
                        HandleDropInventory(message.RootElement);
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

        public void DisconnectPlayer()
        {
            SendMessageToServer(new { type = "disconnect", player = playerName });
        }

        private void HandleTime(JsonElement data)
        {
            _targetTime = data.GetProperty("time").GetSingle(); // Получаем новое время
        }

        private void OnConnected(JsonElement data)
        {
            Vector3 position = JsonToVector3Safe(data, "position");
            _playerPosition = position;
            NetworkWorld.Instance.currentPosition = Vector3Int.FloorToInt(_playerPosition);
            RequestChunksAtStart(position);
        }

        private void OnPlayerConnected(JsonElement data)
        {
            string playerId = data.GetProperty("player_id").GetString();
            Vector3 position = JsonToVector3(data.GetProperty("position"));

            LogDebug($"[MassageFromServer] Player Connected {playerId}");

            UiManager.Instance.ChatWindow.commandReader.PrintLog($"{playerId}: Зашел на сервер",
                new Color(0, 255 / 255f, 0));

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
            var previousPosition = player.transform.position;
            player.transform.position = targetPosition;

            var targetQuaternion = Quaternion.Euler(targetRotation);
            player.transform.rotation = Quaternion.Lerp(player.transform.rotation, targetQuaternion, SmoothSpeed);

            playerAnimator.SetFloat(VelocityX, velocity.x * 4);
            playerAnimator.SetFloat(VelocityY, velocity.y);
            playerAnimator.SetFloat(VelocityZ, velocity.z * 4);
        }


        private void OnPlayerDisconnected(JsonElement data)
        {
            var playerId = data.GetProperty("player_id").GetString();

            LogDebug($"[MassageFromServer] Player Disconnected {playerId}");
            UiManager.Instance.ChatWindow.commandReader.PrintLog($"{playerId}: Вышел с сервера",
                new Color(231 / 255f, 76 / 255f, 60 / 255f));
            if (playerId == null || !_otherPlayers.Remove(playerId, out var player)) return;
            Destroy(player.gameObject);
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

        private void OnPlayerDead(JsonElement data)
        {
            string playerId = data.GetProperty("player_id").GetString();

            if (playerId != null && _otherPlayers.TryGetValue(playerId, out OtherNetPlayer player))
            {
                player.gameObject.SetActive(false);
            }
        }

        private void OnPlayerRespawn(JsonElement data)
        {
            string playerId = data.GetProperty("player_id").GetString();

            if (playerId != null && _otherPlayers.TryGetValue(playerId, out OtherNetPlayer player))
            {
                player.gameObject.SetActive(true);
                player.health.TakeDamage(0);
                player.health.ResetHealth();
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
                NetworkWorld.Instance.UpdateFloraBlock(position, newBlockType);
            else
                NetworkWorld.Instance.UpdateBlock(position, newBlockType);
        }

        private void HandleDamage(JsonElement data)
        {
            var damage = data.GetProperty("damage").GetInt32();
            if (_playerController.health.health <= 0) return;
            if (_playerController) _playerController.health.TakeDamage(damage);
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
            if (!_player || !canSpawnPlayer) return;
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
                    if (_animals.TryGetValue(entity.ID, out var animalFromList))
                    {
                        if (!animalFromList)
                            continue;
                        _animals.Remove(entity.ID);
                    }
                    
                    if (!ChunkSpawned(entity.Position)) continue;
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

        public bool ChunkSpawned(Vector3 position)
        {
            NetworkWorld.Instance.Chunks.TryGetValue(
                NetworkWorld.Instance.GetChunkContainBlock(Vector3Int.FloorToInt(position)), out var chunk);
            return chunk == null ? false : chunk.Renderer;
        }

        #endregion

        #region HandleChunks

        private IEnumerator HandleChunkData(JsonElement data)
        {
            Vector3Int chunkCoord = JsonToVector3Int(data.GetProperty("position"));

            LogDebug($"[Client] Get Chunk at position {chunkCoord} from server.");

            int[,,] blocks = null;
            int[,,] waterBlocks = null;
            int[,,] floraBlocks = null;

            JsonElement blocksJson = data.GetProperty("blocks");
            JsonElement waterBlocksJson = data.GetProperty("waterChunk");
            JsonElement floraBlocksJson = data.GetProperty("floraChunk");

            yield return StartCoroutine(Blocks(blocksJson, result => blocks = result));
            yield return StartCoroutine(Blocks(waterBlocksJson, result => waterBlocks = result));
            yield return StartCoroutine(Blocks(floraBlocksJson, result => floraBlocks = result));

            yield return NetworkWorld.Instance.SpawnChunk(chunkCoord, blocks);
            yield return NetworkWorld.Instance.SpawnWaterChunk(chunkCoord, waterBlocks);
            yield return NetworkWorld.Instance.SpawnFloraChunk(chunkCoord, floraBlocks);

            SpawnedChunks.Add(chunkCoord);

            if (_player)
                _playerPosition = _player.transform.position;

            var playerChunkPosition =
                NetworkWorld.Instance.GetChunkContainBlock(Vector3Int.FloorToInt(_playerPosition));
            if (playerChunkPosition.x - NetworkWorld.Instance.settings.viewDistanceInChunks <= chunkCoord.x &&
                playerChunkPosition.x + NetworkWorld.Instance.settings.viewDistanceInChunks >= chunkCoord.x &&
                playerChunkPosition.z - NetworkWorld.Instance.settings.viewDistanceInChunks <= chunkCoord.z &&
                playerChunkPosition.z + NetworkWorld.Instance.settings.viewDistanceInChunks >= chunkCoord.z)
                ChunksToRender.Enqueue(chunkCoord);
            _requestedChunks--;
            yield return null;
        }

        private IEnumerator Blocks(JsonElement blocksJson, Action<int[,,]> onComplete)
        {
            var flatBlocks = new int[16 * 16 * 256];
            var blocks = new int[16, 256, 16];

            var index = 0;

            // Чтение значений из JSON и заполнение flatBlocks
            foreach (var blockValue in blocksJson.EnumerateArray())
            {
                flatBlocks[index] = blockValue.GetInt32();
                index++;

                // Асинхронное выполнение: каждые 1000 блоков отдаем управление движку
                if (index % 1000 == 0)
                {
                    yield return null;
                }
            }

            // Преобразование flatBlocks в трехмерный массив blocks
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

                // Асинхронное выполнение: каждые 16 строк отдаем управление движку
                yield return null;
            }

            // Вызываем callback с результатом
            onComplete?.Invoke(blocks);
        }


        private void RequestChunksAtStart(Vector3 position)
        {
            Vector3Int playerChunkPosition = new Vector3Int(
                Mathf.FloorToInt(position.x / 16),
                0,
                Mathf.FloorToInt(position.z / 16)
            );

            int loadDistance = NetworkWorld.Instance.settings.loadDistance;

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
            _requestedChunks++;
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

            _player = Instantiate(playerPrefab, _playerPosition + Vector3.up, Quaternion.identity);
            _playerController = _player.GetComponent<PlayerController>();

            _playerController.variableJoystick = moveJoystick;
            _playerController.isMobile = IsMobile;

            _playerController.cameraController.variableJoystick = cameraJoystick;
            _playerController.cameraController.isMobile = IsMobile;

            moveJoystick.gameObject.SetActive(IsMobile);
            cameraJoystick.gameObject.SetActive(IsMobile);

            NetworkWorld.Instance.player = _player;

            _playerController.health.OnDeath += OpenDeadMenu;

            UiManager.Instance.PlayerController = _playerController;
            UiManager.Instance.Initialize();
            UiManager.Instance.CloseLoadingScreen();

            foreach (var otherPlayers in _otherPlayers.Values)
            {
                otherPlayers.cameraTransform = _player.GetComponentInChildren<Camera>().transform;
            }

            _player.GetComponent<Network.Player.InteractController>().OpenChat();

            SendMessageToServer(new
            {
                type = "loaded",
                login = playerName,
            });


            StartChunksLoaded = true;

            StartCoroutine(SendPlayerPositionRepeatedly());
        }


        private void OpenDeadMenu(GameObject deadPlayer)
        {
            if (deadPlayer != _player) return;
            SendMessageToServer(new
            {
                type = "PlayerDeath",
                playerName = playerName,
            });
            SendDropInventory();
            UiManager.Instance.OpenCloseDead();
            _player.GetComponent<Inventory>().Clear();
            _playerController.GetComponent<InteractController>().DisableScripts();
        }

        public void RespawnPlayer()
        {
            UiManager.Instance.CloseDead();
            SendMessageToServer(new
            {
                type = "PlayerRespawn",
                playerName = playerName,
            });
            _playerController.GetComponent<InteractController>().EnableScripts();
            _player.transform.position = _startPosition;
            _playerController.health.ResetHealth();
            _playerController.GetComponent<HungerSystem>().ResetHunger();
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


        private void HandleDropInventory(JsonElement data)
        {
            var slots = JsonToInventory(data.GetProperty("inventory"));
            var pos = Vector3Int.FloorToInt(JsonToVector3(data.GetProperty("position")));
            pos += Vector3Int.up * 2;

            foreach (var slot in slots.Where(itemInSlot => itemInSlot != null))
            {
                if (slot.Item != null)
                {
                    NetworkWorld.Instance.DropItem(pos, slot.Item, slot.Amount);
                }
            }
        }

        private void SendDropInventory()
        {
            var inventory = _playerController.GetComponent<Inventory>().Slots;
            var json = JsonToInventory(inventory);
            SendMessageToServer(new
            {
                type = "drop_inventory",
                position = new
                {
                    x = _player.transform.position.x,
                    y = _player.transform.position.y,
                    z = _player.transform.position.z
                },
                inventory = json
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
                    if (slot.Item != null)
                        item = new ItemJson()
                        {
                            Type = slot.Item.Name,
                            Count = slot.Amount,
                            Durability = slot.Durability,
                        };
                    else
                    {
                        item.Type = "null";
                        item.Count = 0;
                        item.Durability = 0;
                    }
                }

                slotsJson.Add(item);
            }

            return JsonSerializer.Serialize(slotsJson);
        }


        private List<ItemInSlot> JsonToInventory(JsonElement json)
        {
            var inventory = new List<ItemInSlot>();

            try
            {
                if (json.ValueKind == JsonValueKind.String)
                {
                    string jsonString = json.GetString();
                    var items = JsonSerializer.Deserialize<List<ItemJson>>(jsonString);

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
                }
                else
                {
                    Debug.LogError($"Unexpected JSON ValueKind: {json.ValueKind}");
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to deserialize JSON: {ex.Message}");
                Debug.LogError($"JSON content: {json}");
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


        private List<string> logMessages = new List<string>();

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            string message = $"[{type}] {logString}\n{stackTrace}\n";
            logMessages.Add(message);
        }

        public void SendBugReport(string text)
        {
            try
            {
                // Собираем информацию о всех объектах на сцене
                List<object> sceneObjects = new List<object>();
                GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

                foreach (GameObject obj in allObjects)
                {
                    sceneObjects.Add(new
                    {
                        name = obj.name,
                        position = new
                        {
                            x = obj.transform.position.x,
                            y = obj.transform.position.y,
                            z = obj.transform.position.z
                        }
                    });
                }

                // Собираем информацию о других игроках
                List<object> otherPlayersData = new List<object>();
                foreach (var kvp in _otherPlayers)
                {
                    if (kvp.Value != null)
                        otherPlayersData.Add(new
                        {
                            playerId = kvp.Key,
                            position = new
                            {
                                x = kvp.Value.transform.position.x,
                                y = kvp.Value.transform.position.y,
                                z = kvp.Value.transform.position.z
                            }
                        });
                }

                // Собираем информацию о животных
                List<object> animalsData = new List<object>();
                foreach (var kvp in _animals)
                {
                    if (kvp.Value != null)
                        animalsData.Add(new
                        {
                            animalId = kvp.Key,
                            position = new
                            {
                                x = kvp.Value.transform.position.x,
                                y = kvp.Value.transform.position.y,
                                z = kvp.Value.transform.position.z
                            }
                        });
                }

                // Собираем информацию о чанках из NetworkWorld.Instance.Chunks
                List<object> networkChunksData = new List<object>();
                foreach (var kvp in NetworkWorld.Instance.Chunks)
                {
                    networkChunksData.Add(new
                    {
                        chunkPosition = kvp.Key,
                    });
                }

                // Собираем информацию о чанках из очередей и множеств
                var chunkQueuesData = new
                {
                    chunksToRender = ChunksToRender.ToArray(),
                    chunksToGet = ChunksToGet.ToArray(),
                    requestedChunks = RequestedChunks.ToArray(),
                    spawnedChunks = SpawnedChunks.ToArray(),
                    gettedChunks = GettedChunks.ToArray()
                };

                // Формируем итоговое сообщение
                var message = new
                {
                    type = "logs",
                    playerId = playerName,
                    BugReportText = text,
                    sceneObjects = sceneObjects,
                    otherPlayers = otherPlayersData,
                    animals = animalsData,
                    networkChunks = networkChunksData,
                    chunkQueues = chunkQueuesData,
                    logs = logMessages // Добавляем логи в сообщение
                };

                // Отправляем сообщение на сервер
                SendMessageToServer(message);
            }
            catch (Exception e)
            {
                // Если произошла ошибка, отправляем её на сервер
                var message = new
                {
                    type = "logs",
                    BugReportText = text,
                    playerId = playerName,
                    Exception = e,
                    ExceptionMessage = e.Message,
                    InnerException = e.InnerException?.Message,
                    logs = logMessages // Добавляем логи в сообщение об ошибке
                };

                SendMessageToServer(message);
            }
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
            if (!player) return;
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
                    x = player.horizontalInput,
                    player.controller.velocity.y,
                    z = player.verticalInput,
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