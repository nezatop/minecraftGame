using MultiCraft.Scripts.Core.Inventories;
using MultiCraft.Scripts.UI;
using UnityEngine;

namespace MultiCraft.Scripts.Core.CraftingSystem
{
    public class CraftSlot : Slot
    {
        public override void LeftClick()
        {
            base.LeftClick();
            InventoryWindow.Instance.CraftController.CheckCraft();
        }

        public override void RightClick()
        {
            base.RightClick();
            InventoryWindow.Instance.CraftController.CheckCraft();
        }

        public void DecreaseItemAmount(int amount)
        {
            Item.Amount -= amount;

            if (Item.Amount < 1)
            {
                ResetItem();
            }
            else
            {
                RefreshUI();
            }
        }
    }
}