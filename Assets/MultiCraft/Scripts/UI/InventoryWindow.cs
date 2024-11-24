using System;
using MultiCraft.Scripts.Core.CraftingSystem;
using MultiCraft.Scripts.Core.Inventories;
using UnityEngine;
using UnityEngine.UI;

namespace MultiCraft.Scripts.UI
{
    public class InventoryWindow : MonoBehaviour
    {
        public static InventoryWindow Instance;

        public CraftController CraftController;
        public InventoryController InventoryController;

        [SerializeField] private Image currentItemImage;

        public ItemInSlot CurrentItem;
        public bool HasCurrentItem => CurrentItem != null;

        private void Awake()
        {
            Instance = this;
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }
        
        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void SetCurrentItem(ItemInSlot item)
        {
            CurrentItem = item;
            currentItemImage.gameObject.SetActive(true);
            currentItemImage.sprite = CurrentItem.Item.Icon;
        }

        public void ResetCurrentItem()
        {
            CurrentItem = null;
            currentItemImage.gameObject.SetActive(false);
        }

        public void CheckCurrentItem()
        {
            if(CurrentItem != null && CurrentItem.Amount < 1)
                ResetCurrentItem();
        }

        private void Update()
        {
            if(CurrentItem == null)
                return;

            currentItemImage.transform.position = Input.mousePosition;
        }
    }
}