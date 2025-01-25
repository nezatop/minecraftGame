using MultiCraft.Scripts.Engine.UI;

namespace MultiCraft.Scripts.Engine.Core.Inventories
{
    public class HotbarSlot : Slot
    {
        public override void LeftClick()
        {
            UiManager.Instance.SetSelectedItem(slotId);
        }

        public override void RightClick()
        {
            UiManager.Instance.SetSelectedItem(slotId);
        }
    }
}