using System;
using System.Collections.Generic;
using MultiCraft.Scripts.Core.Items;
using UnityEngine;

namespace MultiCraft.Scripts.Utils
{
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance;

        public ItemScriptableObject[] ItemsToLoad;

        public Dictionary<string, Item> Items;
        private bool _isInit = false;
        
        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
                return;
            }
        }

        private void LoadItems()
        {
            Items = new Dictionary<string, Item>();
            foreach (var item in ItemsToLoad)
            {
                var itemToLoad = new Item();
                itemToLoad.Name = item.Name;
                itemToLoad.Icon = item.Icon;
                itemToLoad.CraftRecipe = item.CraftRecipe;
                itemToLoad.Type = item.Type;
                itemToLoad.MaxStackSize = item.MaxStackSize;
                itemToLoad.IsPlaceable = item.IsPlaceable;
                itemToLoad.BlockType = item.BlockType;
                itemToLoad.MaxDurability = item.MaxDurability;
                itemToLoad.Damage = item.Damage;
                itemToLoad.ToolType = item.ToolType;
                itemToLoad.MiningLevel = item.MiningLevel;
                itemToLoad.MiningMultiplier = item.MiningMultiplier;
                itemToLoad.Defense = item.Defense;
                itemToLoad.ArmorSlot = item.ArmorSlot;
                itemToLoad.HungerRestoration = item.HungerRestoration;
                itemToLoad.Saturation = item.Saturation;
                
                Items.Add(itemToLoad.Name, itemToLoad);
            }

            _isInit = true;
        }

        public Item GetItem(string id)
        {
            if(_isInit == false) LoadItems();
            return Items.GetValueOrDefault(id);
        }
        public Item GetItem(ItemScriptableObject itemScriptableObject)
        {
            if(_isInit == false) LoadItems();
            if (itemScriptableObject == null) return null;
            return GetItem(itemScriptableObject.Name);
        }
    }
}