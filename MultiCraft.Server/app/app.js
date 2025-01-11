import { WebSocketServer } from 'ws';
import express from 'express';
import https from 'https';
import cors from 'cors';
import fs from 'fs';
import path from 'path';

import { loadPlayerData, savePlayerData, playerData } from './utils/storage.js';
import { handleClientMessage, broadcast, SendEntities } from './routes/player.js';
import { PORT } from './config.js';
import { clients } from './utils/chunk.js';

const updateInterval = 10000 / 200;

import { fileURLToPath } from 'url';

// Определяем __filename и __dirname
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Чтение сертификата и ключа
const key = fs.readFileSync(path.join(__dirname, '../certs/key.pem'));
const cert = fs.readFileSync(path.join(__dirname, '../certs/cert.pem'));

const app = express();
const server = https.createServer({ key, cert }, app); // Создаём HTTPS сервер

app.use(cors());

const wss = new WebSocketServer({ server }); // Используем WebSocketServer с HTTPS сервером

setInterval(SendEntities, updateInterval);

wss.on('connection', (socket) => {
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
        broadcast(JSON.stringify({ type: 'player_disconnected', player_id: clientId }));
        clients.delete(socket);
    });
});

server.listen(PORT, () => {
    console.log(`[SERVER] Start on port ${PORT} with WSS`);
});
