import fs from 'fs';
import path from 'path';
import { PLAYERS_DIR_PATH } from '../config.js'; // Обновите путь в config.js
import { PlayerData } from '../models/player.js';

export const playerData = new Map();

// Создаем директорию для игроков при необходимости
function ensurePlayersDir() {
    if (!fs.existsSync(PLAYERS_DIR_PATH)) {
        fs.mkdirSync(PLAYERS_DIR_PATH, { recursive: true });
    }
}

export function loadPlayerByLogin(login) {
    ensurePlayersDir();

    const filePath = path.join(PLAYERS_DIR_PATH, `${login}.json`);

    if (!fs.existsSync(filePath)) {
        return null;
    }

    try {
        const data = fs.readFileSync(filePath);
        const playerJson = JSON.parse(data);

        const player = new PlayerData(
            playerJson.login,
            playerJson.password,
            playerJson.position,
            playerJson.rotation,
            playerJson.inventory
        );

        playerData.set(login, player);
        savePlayer(login)
        return player;

    } catch (error) {
        console.error(`Error loading player ${login}:`, error);
        return null;
    }
}

export function loadPlayerData() {
    ensurePlayersDir();

    const files = fs.readdirSync(PLAYERS_DIR_PATH);

    files.forEach(file => {
        if (path.extname(file) === '.json') {
            const filePath = path.join(PLAYERS_DIR_PATH, file);
            const data = fs.readFileSync(filePath);
            const player = JSON.parse(data);

            playerData.set(player.login, new PlayerData(
                player.login,
                player.password,
                player.position,
                player.rotation,
                player.inventory
            ));
        }
    });
}

export function savePlayerData() {
    ensurePlayersDir();

    playerData.forEach(player => {
        const filePath = path.join(PLAYERS_DIR_PATH, `${player.login}.json`);
        const playerDataToSave = {
            login: player.login,
            password: player.password,
            position: player.position,
            rotation: player.rotation,
            inventory: player.inventory
        };

        fs.writeFileSync(filePath, JSON.stringify(playerDataToSave, null, 2));
    });
}

// Дополнительная функция для сохранения конкретного игрока
export function savePlayer(login) {
    const player = playerData.get(login);
    if (!player) return;

    ensurePlayersDir();

    const filePath = path.join(PLAYERS_DIR_PATH, `${login}.json`);
    const playerDataToSave = {
        login: player.login,
        password: player.password,
        position: player.position,
        rotation: player.rotation,
        inventory: player.inventory
    };

    fs.writeFileSync(filePath, JSON.stringify(playerDataToSave, null, 2));
}