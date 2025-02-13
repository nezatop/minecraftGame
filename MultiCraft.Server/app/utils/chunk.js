import {WorldGenerator, Config} from './WorldGenerator.js';
import fs from "fs";

export const clients = new Map();
export const chunkMap = new Map();
export const waterChunkMap = new Map();
export const floraChunkMap = new Map();

export let entities = loadAnimalsFromFile("entities.json");

const ANIMAL_TYPES = ['sheep', 'cow', 'pig'];
const NUM_ANIMALS = 20;
const MOVE_INTERVAL = 3000;


const worldGenerator = new WorldGenerator(Config);

export function getChunkIndex(position) {
    const x = position.x % 16;
    const y = position.y % 256;
    const z = position.z % 16;
    return x + y * 16 * 16 + z * 16;
}

export function getRandomSurfacePosition() {
    const chunkWidth = 16;
    const chunkDepth = 16;

    const randomX = Math.floor(Math.random() * chunkWidth);
    const randomZ = Math.floor(Math.random() * chunkDepth);

    const chunkKey = `${Math.floor(randomX / chunkWidth)},0,${Math.floor(randomZ / chunkDepth)}`;

    let chunk;

    if (chunkMap.has(chunkKey)) {
        chunk = chunkMap.get(chunkKey);
    } else {
        chunk = createChunk(Math.floor(randomX / 16), 0, Math.floor(randomZ / 16)); // Ваша функция создания чанка
        chunkMap.set(chunkKey, chunk); // Сохраняем новый чанк в карте
    }

    const surfaceY = findSurfaceHeight(chunk, randomX % chunkWidth, randomZ % chunkDepth);

    return {x: randomX, y: surfaceY + 2, z: randomZ}; // Возвращаем поверхность
}

export function createChunk(offsetX, offsetY, offsetZ) {
    const blocks = worldGenerator.generate(offsetX, offsetZ);
    if (Math.random() < 0.2) {
        generateAnimals(blocks, offsetX, offsetY, offsetZ);
        saveAnimalsToFile("entities.json", entities)
    }
    return blocks;
}

export function createWaterChunk(offsetX, offsetY, offsetZ) {
    return worldGenerator.generateWater(offsetX, offsetZ);
}

export function createFloraChunk(offsetX, offsetY, offsetZ) {
    return worldGenerator.generateFlora(offsetX, offsetZ);
}

export function generateAnimals(blocks, offsetX, offsetY, offsetZ) {
    for (let i = 0; i < NUM_ANIMALS; i++) {
        if (Math.random() < 0.1) {
            const randomX = Math.floor(Math.random() * 16);
            const randomZ = Math.floor(Math.random() * 16);
            const Y = findSurfaceHeight(blocks, randomX, randomZ);
            const position = {x: randomX + offsetX, y: Y + 5, z: randomZ + offsetZ};
            const type = ANIMAL_TYPES[Math.floor(Math.random() * ANIMAL_TYPES.length)];
            const id = `animal_${entities.size}`;

            if(!entities.has(id))
                entities.set(id, {id, type, position});
        }
    }
}

export function loadAnimalsFromFile(filename) {
    if (!fs.existsSync(filename)) {
        console.log(`File ${filename} does not exist.`);
        return new Map();
    }

    // Читаем данные из файла
    const data = fs.readFileSync(filename, 'utf-8');
    const animalsArray = JSON.parse(data);

    // Преобразуем массив обратно в Map
    const entities = new Map();
    animalsArray.forEach(animal => {
        entities.set(animal.id, animal);
    });

    console.log(`Animals loaded from ${filename}`);
    return entities;
}

export function saveAnimalsToFile(filename, entities) {
    // Преобразуем Map в массив объектов для удобства сохранения
    const animalsArray = Array.from(entities.values());

    // Сохраняем данные в файл
    fs.writeFileSync(filename, JSON.stringify(animalsArray, null, 2), 'utf-8');
    console.log(`Animals saved to ${filename}`);
}

function findSurfaceHeight(chunk, x, z) {
    const height = 256; // Максимальная высота
    for (let y = height - 1; y >= 0; y--) { // Начинаем с самой верхней высоты и идем вниз
        const index = getChunkIndex({x: x, y: y, z: z});
        if (chunk[index] !== 0) { // Предполагаем, что 0 - это воздух
            return y; // Возвращаем Y-координату найденной поверхности
        }
    }
    return 0;
}
