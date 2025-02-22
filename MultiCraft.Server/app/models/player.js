import {createInventory} from "../utils/inventory.js";

class PlayerData {
    constructor(login, password, position = { x: 0, y: 0, z: 0 }, rotation = { x: 0, y: 0 }, inventory) {
        this.login = login;
        this.password = password;
        this.position = position;
        this.rotation = rotation;
        if(inventory === undefined)
            this.inventory = createInventory();
        else
            this.inventory = inventory;
        this.health = 10;
        this.hunger = 100;
    }
}

export { PlayerData };
