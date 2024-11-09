using UnityEngine;

namespace MultiCraft.Scripts.Game.Items
{
    public class Item
    {
        public string Name = "unknown";
        public ItemType Type = ItemType.Unknown;
        public string Description = "";

        public int MaxQuantity = 64;
        public bool Stackable = true;
        
        public int MaxDurability = 0;
    }
}