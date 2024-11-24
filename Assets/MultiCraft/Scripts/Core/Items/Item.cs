using MultiCraft.Scripts.Core.Blocks;
using MultiCraft.Scripts.Core.CraftingSystem;
using UnityEngine;

namespace MultiCraft.Scripts.Core.Items
{
    public class Item
    {
        public string Name;
        public Sprite Icon;

        public CraftRecipe CraftRecipe;
        public bool HasRecipe => CraftRecipe.Items.Count > 0;
        
        public ItemType Type;
        public int MaxStackSize;

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