import path from 'path';
import { fileURLToPath } from 'url';
import fs from 'fs';

// Получаем __dirname в ES модулях
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Указываем путь к папке MultiCraft.Server/logs
const serverRoot = path.join(__dirname, '../../'); // Поднимаемся на два уровня вверх (из app/routes)
const logsDir = path.join(serverRoot, 'logs');

export function SaveLog(data, socket) {
    // Проверяем, что data существует и содержит playerId
    if (!data || !data.playerId) {
        console.error('Invalid data: playerId is missing');
        return; // Прекращаем выполнение функции
    }

    const { playerId, ...logData } = data;

    // Создаем директорию для логов игрока, если она не существует
    const playerLogDir = path.join(logsDir, playerId.toString());
    if (!fs.existsSync(playerLogDir)) {
        fs.mkdirSync(playerLogDir, { recursive: true });
    }

    // Генерируем имя файла на основе текущей даты и времени
    const now = new Date();
    const timestamp = now.toISOString().replace(/[:.]/g, '-'); // Заменяем недопустимые символы в имени файла
    const logFileName = path.join(playerLogDir, `${timestamp}.json`);

    // Сохраняем данные в файл
    fs.writeFileSync(logFileName, JSON.stringify(logData, null, 2), 'utf8');

    console.log(`Log saved to ${logFileName}`);
}