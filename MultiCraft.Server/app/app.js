import {WebSocketServer} from 'ws'; // Импортируем WebSocketServer

import express from 'express';
import https from 'https';
import http from 'http';
import cors from 'cors';

import {loadPlayerData, savePlayerData, playerData} from './utils/storage.js';
import {handleClientMessage, broadcast, SendEntities} from './routes/player.js';
import {PORT} from './config.js';
import {clients} from './utils/chunk.js';
import fs from "fs";
import path from "path";

import {fileURLToPath} from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const updateInterval = 10000 / 200;

const key = fs.readFileSync(path.join(__dirname, '../certs/key.pem'));
const cert = fs.readFileSync(path.join(__dirname, '../certs/cert.pem'));

console.log("key", key);
console.log("cert", cert);

const app = express();
const server = https.createServer(
    {
        key,
        cert,
        passphrase: 'test'
    }, app);

app.use(cors());

const wss = new WebSocketServer({server}); // Используем WebSocketServer вместо WebSocket.Server

setInterval(SendEntities, updateInterval);

wss.on('connection', (socket) => {

    console.log("socket", socket);

    socket.on('message', (message) => {
        try {
            const data = JSON.parse(message);
            handleClientMessage(data, socket);
        } catch (error) {
            console.error('Ошибка при обработке сообщения:', error);
        }
    });

    socket.on('close', () => {
        const clientId = clients.get(socket);

        if (clientId) {
            const playerInfo = playerData.get(clientId);
            if (playerInfo) {
                savePlayerData();
                playerData.delete(clientId);
            }
        }
        broadcast(JSON.stringify({type: 'player_disconnected', player_id: clientId}));
        clients.delete(socket);
    });
});

server.listen(PORT, () => {
    console.log(`[SERVER] Start on port ${PORT}`);
});
