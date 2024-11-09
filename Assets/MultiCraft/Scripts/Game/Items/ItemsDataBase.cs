using System.Collections.Generic;
using UnityEngine;

namespace MultiCraft.Scripts.Game.Items
{
    public static class ItemsDataBase
    {
        public static Dictionary<string, Item> Items;

        public static void Initialize()
        {
            Items = new Dictionary<string, Item>();

            TextAsset[] itemFiles = Resources.LoadAll<TextAsset>("Items");
            foreach (var file in itemFiles)
            {
                Item item = JsonUtility.FromJson<Item>(file.text);
                Items.TryAdd(item.Name, item);
            }
        }

        public static Item GetItem(string name)
        {
            return Items.ContainsKey(name) ? Items[name] : null;
        }

        public static void PrintAllItems()
        {
            if (Items == null || Items.Count == 0)
            {
                Debug.Log("No items in the database.");
                return;
            }

            foreach (var item in Items.Values)
            {
                Debug.Log($"Name: {item.Name}, Type: {item.Type}, Description: {item.Description}, " +
                          $"Weight: {item.MaxQuantity}, Stackable: {item.Stackable}, " +
                          $"MaxDurability: {item.MaxDurability}");
            }
        }
    }
}