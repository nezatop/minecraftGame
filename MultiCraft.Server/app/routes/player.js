import {loadPlayerByLogin, playerData, savePlayer} from '../utils/storage.js';
import {
    clients
    , createChunk
    , createFloraChunk
    , createWaterChunk
    , entities
    , getRandomSurfacePosition, loadAnimalsFromFile
} from '../utils/chunk.js';
import { PlayerData } from "../models/player.js";
import { addInventory, createInventory, getInventory, setInventory } from "../utils/inventory.js";
import fs from "fs";
import path from "path";
import msgpack from '@msgpack/msgpack';
import {SaveLog} from "./logs.js";
import {PORT} from "../config.js";

const __dirname = "./"

const chunkStorageDir = path.join(__dirname, 'chunks');

if (!fs.existsSync(chunkStorageDir)) {
    fs.mkdirSync(chunkStorageDir, { recursive: true });
}

const DayDuration = 300;
let startTime = Date.now();

function getTimeFraction() {
    let elapsed = (Date.now() - startTime) / 1000;
    return (elapsed % DayDuration) / DayDuration;
}

export async function broadcastTime() {
    await broadcast(JSON.stringify({
        type: 'time',
        time: getTimeFraction(),
    }));
}

export async function handleClientMessage(data, socket) {
    switch (data.type) {
        case 'connect':
            handleConnect(data, socket);
            console.log(`[SERVER] ${data.login} connected to server`);
            break;
        case 'loaded':
            handleLoaded(data, socket);
            break;
        case 'drop_inventory':
            handleDropInventory(data, socket);
            break;
        case 'get_chunk':
            handleGetChunk(data.position, socket);
            break;
        case 'logs':
            SaveLog(data, socket);
            break;
        case 'move':
            handleMove(data.position, data.rotation, data.velocity, socket);
            break;
        case 'get_players':
            handlePlayers(socket);
            break;
        case 'place_block':
            handlePlaceBlock(data.position, data.block_type, socket);
            break;
        case 'PlayerDeath':
            handlePlayerDead(data.playerName, socket);
            console.log(`[SERVER] ${data.playerName} dead`);
            break;
        case 'destroy_block':
            handleDestroyBlock(data.position, socket);
            break;
        case 'get_inventory':
            handleGetInventory(data.position, socket);
            break;
        case 'set_inventory':
            handleSetInventory(data.position, data.inventory, socket);
            break;
        case 'pickup_item':
            handleSetInventory(data.item, socket);
            break;
        case 'disconnect':
            handleDisconnect(data, socket);
            console.log(`[SERVER] ${data.player} disconnected`);
            break;
        case 'chat':
            handleChat(data.player, data.chat_massage, socket);
            console.log(`[SERVER] ${data.player}: ${data.chat_massage}`);
            break;
        case 'get_entities':
            handleGetEntities(data, socket);
            break;
        case 'Attack':
            handleAttack(data, socket);
            break;
        case 'PlayerRespawn':
            handleRespawn(data.playerName, socket);
            break;
        default:
    }
}

function handleLoaded(data, socket) {
    const { login} = data;
    broadcast(JSON.stringify({ type: 'player_connected', player_id: login, position: playerData.get(login).position }));
}

function handlePlayerDead(playerName, socket) {
    broadcast(JSON.stringify({ type: 'player_dead', player_id: playerName }));
}

function handleRespawn(playerName, socket) {
    broadcast(JSON.stringify({ type: 'Player_respawn', player_id: playerName }));
}

function handleDisconnect(data, socket) {
    try {
        console.log('Disconnect data:', data);

        // Парсим инвентарь из строки в массив
        const inventory = JSON.parse(data.inventory);

        // Обновляем данные игрока
        if(playerData.has(data.player)) {
            const player = playerData.get(data.player);
            player.position = data.position;
            player.rotation = data.rotation;
            player.inventory = inventory;


            savePlayer(data.player);
        }

        sendMessage(socket, {
            type: 'disconnected'
        });

        broadcast(JSON.stringify({
            type: 'player_disconnected',
            player_id: data.player
        }));

    } catch (error) {
        console.error('Disconnect handling error:', error);
        // Можно добавить повторную попытку сохранения
    }
}

function handleDropInventory(data, socket) {
    broadcast(JSON.stringify({
        type: 'drop_inventory'
        , position: data.position
        , inventory: data.inventory
    }));
}

function handleAttack(data, socket) {
    const target = data.attack_target;
    let foundSocket = null;
    clients.forEach((value, socket) => {
        if (value === target) {
            foundSocket = socket;
        }
    });
    sendMessage(foundSocket, {
        type: 'damage'
        , damage: data.damage
    })
}

function handleChat(player, chat_massage, socket) {
    broadcast(JSON.stringify({ type: 'chat', player_id: player, chat_massage: chat_massage }));
}

function handleGetEntities(data, socket) {
    sendMessage(socket, {
        type: 'entities'
        , entities_list: entities
    })
}

export async function SendEntities() {
    if (entities.size > 0) { // Проверяем, есть ли сущности
        const entitiesArray = Array.from(entities.values()); // Преобразуем Map в массив
        await broadcast(JSON.stringify({
            type: 'entities'
            , entities_list: entitiesArray
        }));
    }
}


function handleGetInventory(position, socket) {
    const clientId = clients.get(socket);
    if (clientId) {
        const inventory = getInventory(position);
        sendMessage(socket, { type: 'inventory', position: position, inventory: inventory })
    }
}

function handleSetInventory(position, inventory, socket) {
    setInventory(position, inventory);
}

function handleConnect(data, socket) {
    try {
        const { login, password } = data;

        let playerInfo = playerData.get(login);

        if (!playerInfo)
        {
            playerInfo = loadPlayerByLogin(login);
        }

        if (playerInfo) {
            if(playerInfo.position.y < -1)
                playerInfo.position = getRandomSurfacePosition();
            sendMessage(socket, {
                type: 'connected',
                position: playerInfo.position,
                rotation: playerInfo.rotation,
                inventory: playerInfo.inventory
            });
        } else {
            const startPosition = getRandomSurfacePosition();
            playerInfo = new PlayerData(
                login,
                "",
                startPosition,
                { x: 0, y: 0, z: 0 }
            );

            playerData.set(login, playerInfo);
            savePlayer(login);

            sendMessage(socket, {
                type: 'connected',
                position: startPosition,
                rotation: playerInfo.rotation,
                inventory: playerInfo.inventory
            });
        }

        clients.set(socket, login);
        broadcast(JSON.stringify({
            type: 'player_connected',
            player_id: login,
            position: playerData.get(login).position
        }));

    } catch (error) {
        console.error(`Connection error: ${error.message}`);
        sendMessage(socket, {
            type: 'connection_error',
            message: error.message
        });
        socket.close();
    }
}

function handlePlayers(socket) {
    const playersArray = Array.from(playerData.values()).map(data => ({
        player_id: data.login
        , position: data.position
    }));

    sendMessage(socket, {
        type: 'players_list'
        , players: playersArray
    })
}

function saveChunkToFile(chunkKey, chunk, waterChunk, floraChunk) {
    const filePath = path.join(chunkStorageDir, `${chunkKey}.mp`);

    const chunkData = { chunk, waterChunk, floraChunk };
    const packedData = msgpack.encode(chunkData);

    fs.writeFileSync(filePath, packedData);
}

function loadChunkFromFile(chunkKey) {
    const filePath = path.join(chunkStorageDir, `${chunkKey}.mp`);

    if (fs.existsSync(filePath)) {
        const packedData = fs.readFileSync(filePath);
        return msgpack.decode(packedData);
    }
    return null;
}

function handleGetChunk(position, socket) {
    const chunkKey = `${position.x},${position.y},${position.z}`;
    let chunk;
    let waterChunk;
    let floraChunk;
    const savedChunkData = loadChunkFromFile(chunkKey);

    if (savedChunkData) {
        chunk = savedChunkData.chunk;
        waterChunk = savedChunkData.waterChunk;
        floraChunk = savedChunkData.floraChunk;
    } else {
        chunk = createChunk(position.x * 16, position.y * 256, position.z * 16);
        waterChunk = createWaterChunk(position.x * 16, position.y * 256, position.z * 16);
        floraChunk = createFloraChunk(position.x * 16, position.y * 256, position.z * 16);

        saveChunkToFile(chunkKey, chunk, waterChunk, floraChunk);
    }

    sendMessage(socket, {
        type: 'chunk_data'
        , position: position
        , blocks: chunk
        , waterChunk: waterChunk
        , floraChunk: floraChunk
        })
}

async function handleMove(position, rotation, velocity, socket) {
    const clientId = clients.get(socket);
    if (clientId && playerData.has(clientId)) {
        playerData.get(clientId).position = position;
        playerData.get(clientId).rotation = rotation;
    }
    broadcast(JSON.stringify({
        type: 'player_moved'
        , player_id: clientId
        , position: position
        , rotation: rotation
        , velocity: velocity
    }));
}

// Обновление блока в чанк
async function updateBlock(position, blockType, socket) {
    const chunkPosition = getChunkContainingBlock(position);
    const chunkKey = `${chunkPosition.x},${chunkPosition.y},${chunkPosition.z}`;

    // Загружаем чанк из файла
    let chunk = loadChunkFromFile(chunkKey);

    if (chunk) {
        const chunkOrigin = {
            x: chunkPosition.x * 16
            , y: chunkPosition.y * 16
            , z: chunkPosition.z * 16
        };

        const indexV3 = {
            x: position.x - chunkOrigin.x
            , y: position.y - chunkOrigin.y
            , z: position.z - chunkOrigin.z};

        const index = indexV3.x + indexV3.z * 16 + indexV3.y * 16 * 16;
        chunk.chunk[index] = blockType; // Обновляем блок в чанке

        if (blockType === 54) {
            addInventory(position); // Если блок определенный, добавляем в инвентарь
        }

        // Сохраняем чанк обратно в файл
        saveChunkToFile(chunkKey, chunk.chunk, chunk.waterChunk, chunk.floraChunk);

        // Отправляем обновление всем клиентам
        broadcast(JSON.stringify({
            type: 'block_update'
            , chunk: "Block"
            , position: position
            , block_type: blockType
        }));
    }
}

// Обновление флоры в чанк
function updateFloraBlock(position, blockType, socket) {
    const chunkPosition = getChunkContainingBlock(position);
    const chunkKey = `${chunkPosition.x},${chunkPosition.y},${chunkPosition.z}`;

    // Загружаем флору чанка из файла
    let floraChunk = loadChunkFromFile(chunkKey);

    if (floraChunk) {
        const chunkOrigin = {
            x: chunkPosition.x * 16
            , y: chunkPosition.y * 16
            , z: chunkPosition.z * 16
        };

        const indexV3 = {
            x: position.x - chunkOrigin.x
            , y: position.y - chunkOrigin.y
            , z: position.z - chunkOrigin.z};

        const index = indexV3.x + indexV3.z * 16 + indexV3.y * 16 * 16;
        floraChunk.floraChunk[index] = blockType; // Обновляем флору в чанке

        // Сохраняем флору обратно в файл
        saveChunkToFile(chunkKey, floraChunk.chunk, floraChunk.waterChunk, floraChunk.floraChunk);

        // Отправляем обновление всем клиентам
        broadcast(JSON.stringify({
            type: 'block_update'
            , chunk: "Flora"
            , position: position
            , block_type: blockType
        }));
    }
}

function handlePlaceBlock(position, blockType, socket) {
    updateBlock(position, blockType, socket);
}

function handleDestroyBlock(position, socket) {
    updateBlock(position, 0, socket);
    updateFloraBlock(position, 0, socket);
    position.y++;
    updateFloraBlock(position, 0, socket);
}

function getChunkContainingBlock(blockWorldPosition) {
    let chunkPosition = {
        x: Math.trunc(blockWorldPosition.x / 16)
        , y: Math.trunc(blockWorldPosition.y / 256)
        , z: Math.trunc(blockWorldPosition.z / 16)
    };

    if (blockWorldPosition.x < 0) {
        if (blockWorldPosition.x % 16 !== 0) {
            chunkPosition.x--;
        }
    }
    if (blockWorldPosition.z < 0) {
        if (blockWorldPosition.z % 16 !== 0) {
            chunkPosition.z--;
        }
    }

    return chunkPosition;
}

export function sendMessage(socket, data) {
    socket.send(JSON.stringify(data));
}

export function broadcast(data) {
    for(const [client] of clients){
        if (client.readyState === WebSocket.OPEN) {
            client.send(data);
        }
    }
/*
    clients.forEach((_, client) => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(data);
        }
    });*/
}
