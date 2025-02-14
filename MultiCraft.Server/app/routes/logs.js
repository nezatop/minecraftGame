import fs from 'fs';
import path from 'path';

export function SaveLog(data, socket) {
    // Предположим, что data содержит playerId и другие данные
    const { playerId, ...logData } = data;

    // Создаем директорию для логов, если она не существует
    const logDir = path.join(__dirname, 'logs', playerId.toString());
    if (!fs.existsSync(logDir)) {
        fs.mkdirSync(logDir, { recursive: true });
    }

    // Генерируем имя файла на основе текущей даты и времени
    const now = new Date();
    const timestamp = now.toISOString().replace(/[:.]/g, '-'); // Заменяем недопустимые символы в имени файла
    const logFileName = path.join(logDir, `${timestamp}.json`);

    // Сохраняем данные в файл
    fs.writeFileSync(logFileName, JSON.stringify(logData, null, 2), 'utf8');

    console.log(`Log saved to ${logFileName}`);
}