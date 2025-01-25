import {PORT} from "../config.js";

const inventories = new Map();

export function getInventory(position) {
    console.log(`[SERVER] send inventory on position(${JSON.stringify(position)}) ${inventories.get(JSON.stringify(position))}`);
    return inventories.get(JSON.stringify(position));
}

export function setInventory(position, inventory) {
    console.log(`[SERVER] set inventory on position(${JSON.stringify(position)}) ${JSON.parse(inventory)}`);
    inventories.set(JSON.stringify(position), JSON.parse(inventory));
}

export function addInventory(position) {
    console.log(`[SERVER] add inventory on position(${JSON.stringify(position)})`);
    inventories.set(JSON.stringify(position), createInventory());
   }

export function createInventory() {
    return new Array(9 * 4).fill(null).map(() => ({
        type: "null",
        count: 0,
        durability: 0
    }));
}
