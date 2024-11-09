using MultiCraft.Scripts.Game.Items;

namespace MultiCraft.Scripts.Game.Inventory
{
    public class InventorySlot
    {
        public Item Item = null;
        public int Quantity = 0;

        public bool CanAdd(Item item)
        {
            if (Item == null) return false;
            if (Quantity < Item.MaxQuantity && Item.Name == item.Name) return true;
            return false;
        }

        public bool CanAdd()
        {
            if (Quantity == 0) return true;
            return false;
        }
    }
}