import path from 'path';
import { fileURLToPath } from 'url';
import fs from 'fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const serverRoot = path.join(__dirname, '../../');
const logsDir = path.join(serverRoot, 'logs');

export function SaveLog(data, socket) {
    if (!data || !data.playerId) {
        return;
    }

    const { playerId, ...logData } = data;

    const playerLogDir = path.join(logsDir, playerId.toString());
    if (!fs.existsSync(playerLogDir)) {
        fs.mkdirSync(playerLogDir, { recursive: true });
    }

    const now = new Date();
    const timestamp = now.toISOString().replace(/[:.]/g, '-'); // Заменяем недопустимые символы в имени файла
    const logFileName = path.join(playerLogDir, `${timestamp}.json`);

    fs.writeFileSync(logFileName, JSON.stringify(logData, null, 2), 'utf8');

    console.log(`Log saved to ${logFileName}`);
}