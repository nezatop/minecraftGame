using System;
using System.Collections.Generic;
using MultiCraft.Scripts.Core.Inventories;
using MultiCraft.Scripts.Core.Players;
using UnityEngine;

namespace MultiCraft.Scripts.UI
{
    public class UiManager : MonoBehaviour
    {
        public static UiManager Instance;

        public InventoryWindow InventoryWindow;
        public GameObject LoadingScreen;
        
        public PlayerController Player;

        public bool inventoryUpdated = false;

        private void Awake()
        {
            Instance = this;
            InventoryWindow.CraftController.Init();
            InventoryWindow.InventoryController.Init();

            LoadingScreen.SetActive(true);
            
            OpenInventory();
            CloseInventory();
        }


        public void OpenCloseInventory()
        {
            if (InventoryWindow.gameObject.activeSelf)
            {
                CloseInventory();
                inventoryUpdated = true;
            }
            else
            {
                OpenInventory();
            }
        }

        private void OpenInventory()
        {
            InventoryWindow.Open();
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        private void CloseInventory()
        {
            InventoryWindow.Close();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }


        public void UpdateInventory(List<ItemInSlot> slots)
        {
            InventoryWindow.InventoryController.UpdateUI(slots);
        }

        public void CloseLoadingPanel()
        {
            if(Player._isGrounded)LoadingScreen.SetActive(false);
        }
    }
}