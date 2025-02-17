using MultiCraft.Scripts.Engine.Core.Items;

namespace MultiCraft.Scripts.Engine.Core.Inventories
{
    public class ItemInSlot
    {
        public Item Item;
        public int Amount;
        public int Durability;

        public ItemInSlot(Item item, int amount)
        {
            Item = item;
            Amount = amount;
            if(item != null)
                Durability = item.MaxDurability;
        }

        public ItemInSlot()
        {
            Item = null;
            Amount = 0;
            Durability = 0;
        }

        public void ReduceDurability()
        {
            Durability--;
        }
        
        public bool CanAdd(Item item)
        {
            if (Item == null) return false;
            return Amount < Item.MaxStackSize && Item.Name == item.Name;
        }

        public bool CanAdd()
        {
            return Amount == 0;
        }
    }
}