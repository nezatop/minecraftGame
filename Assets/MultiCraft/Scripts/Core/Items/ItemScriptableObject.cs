using MultiCraft.Scripts.Core.Blocks;
using MultiCraft.Scripts.Core.CraftingSystem;
using UnityEngine;

namespace MultiCraft.Scripts.Core.Items
{
    [CreateAssetMenu(fileName = "Item", menuName = "MultiCraft/Item")]
    public class ItemScriptableObject : ScriptableObject
    {
        public string Name;
        public Sprite Icon;
        public ItemType Type;
        public int MaxStackSize;
        
        public CraftRecipe CraftRecipe;
        public bool HasRecipe => CraftRecipe.Items.Count > 0;

        public bool IsPlaceable;
        public BlockScriptableObject BlockType;

        public int MaxDurability;
        public int Damage;
        public ToolType ToolType;
        public int MiningLevel;
        public float MiningMultiplier;

        public float Defense;
        public ArmorSlot ArmorSlot;

        public float HungerRestoration;
        public float Saturation;
    }
}