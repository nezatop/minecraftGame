using System;
using System.Collections.Generic;
using MultiCraft.Scripts.Game.Items;
using UnityEngine;

namespace MultiCraft.Scripts.Game.Inventory
{
    public class Inventory : MonoBehaviour
    {
        public List<InventorySlot> Slots;
        private Vector2Int Size;

        private void Start()
        {
            ItemsDataBase.Initialize();
            Initialize(new Vector2Int(4, 9));
        }

        private void Update()
        {
            PrintInv();
        }

        private void PrintInv()
        {
            foreach (InventorySlot slot in Slots)
            {
                if (slot.Item != null)
                {
                    Debug.Log(slot.Item.Name + " " + (slot.Quantity ));
                }
            }
        }
        public void Initialize(Vector2Int size)
        {
            Size = size;
            Slots = new List<InventorySlot>();
            for (int i = 0; i < Size.x * Size.y; i++)
            {
                Slots.Add(new InventorySlot());
            }
        }

        public bool AddItem(Item item, int amount)
        {
            foreach (var slot in Slots)
            {
                if (slot.CanAdd(item))
                {
                    int spaceInSlot = slot.Item.MaxQuantity - slot.Quantity;
                    int amountToAdd = Mathf.Min(spaceInSlot, amount);

                    slot.Quantity += amountToAdd;
                    amount -= amountToAdd;

                    if (amount == 0)
                        return true;
                }
            }

            foreach (var slot in Slots)
            {
                if (slot.CanAdd())
                {
                    int spaceInSlot = item.MaxQuantity - slot.Quantity;
                    int amountToAdd = Mathf.Min(spaceInSlot, amount);
                    slot.Item = item;

                    slot.Quantity += amountToAdd;
                    amount -= amountToAdd;

                    if (amount == 0)
                        return true;
                }
            }

            return false;
        }
    }
}