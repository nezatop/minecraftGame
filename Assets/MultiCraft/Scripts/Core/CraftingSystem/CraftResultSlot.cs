using MultiCraft.Scripts.Core.Inventories;
using MultiCraft.Scripts.UI;
using UnityEngine;

namespace MultiCraft.Scripts.Core.CraftingSystem
{
    public class CraftResultSlot : Slot
    {
        public override void LeftClick()
        {
            if(InventoryWindow.Instance.HasCurrentItem || !InventoryWindow.Instance.CraftController.HasrResultItem)
                return;
            
            InventoryWindow.Instance.SetCurrentItem(Item);
            ResetItem();
            
            InventoryWindow.Instance.CraftController.CraftItem();
        }
    }
}