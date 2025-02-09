import { WebSocketServer } from 'ws'; // Импортируем WebSocketServer
import express from 'express';
import https from 'https';
import http from 'http';
import cors from 'cors';

import { loadPlayerData, savePlayerData, playerData } from './utils/storage.js';
import {handleClientMessage, broadcast, SendEntities, broadcastTime} from './routes/player.js';
import { PORT } from './config.js';
import { clients } from './utils/chunk.js';
import fs from "fs";
import path from "path";

import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const updateInterval = 10000 / 200;

// Путь к сертификатам
const keyPath = '/etc/letsencrypt/live/bloxter.fun/privkey.pem';
const certPath = '/etc/letsencrypt/live/bloxter.fun/cert.pem';

const app = express();
app.use(cors());

// Проверяем наличие сертификатов
let server;
if (fs.existsSync(keyPath) && fs.existsSync(certPath)) {
    console.log('[SERVER] HTTPS mode enabled');
    const key = fs.readFileSync(keyPath);
    const cert = fs.readFileSync(certPath);

    server = https.createServer(
        {
            key,
            cert,
            passphrase: 'test', // Убедитесь, что passphrase соответствует настройкам сертификата
        },
        app
    );
} else {
    console.log('[SERVER] HTTPS certificates not found, starting HTTP server');
    server = http.createServer(app);
}

// WebSocket сервер
const wss = new WebSocketServer({ server });

// Интервальный вызов функции SendEntities
setInterval(SendEntities, updateInterval);
setInterval(broadcastTime, 50);
// Обработка подключения WebSocket
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

// Запуск сервера
server.listen(PORT, () => {
    console.log(`[SERVER] Started on port ${PORT}`);
});
