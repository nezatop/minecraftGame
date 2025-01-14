using System;
using System.Collections.Generic;
using System.Linq;
using MultiCraft.Scripts.Engine.Core.HealthSystem;
using Multicraft.Scripts.Engine.Core.Hunger;
using MultiCraft.Scripts.Engine.Core.Inventories;
using MultiCraft.Scripts.Engine.Core.Player;
using MultiCraft.Scripts.Engine.Core.Worlds;
using MultiCraft.Scripts.Engine.Utils.Commands;
using MultiCraft.Scripts.Engine.Utils.MulticraftDebug;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiCraft.Scripts.Engine.UI
{
    public class UiManager : MonoBehaviour
    {
        public static UiManager Instance;

        public InventoryWindow InventoryWindow;
        public ChatWindow ChatWindow;
        public GameObject LoadingScreen;
        
        public GameObject jumpButton;

        public HealthView HealthView;
        public HungerUI hungerUI;

        public GameObject GameOverScreen;
        public GameObject PauseScreen;

        public bool inventoryUpdated = false;

        public PlayerController PlayerController;
        private MultiCraft.Scripts.Engine.Network.Player.DestroyAndPlaceBlockController NetDestroyAndPlaceBlockController;
        private DestroyAndPlaceBlockController DestroyAndPlaceBlockController;

        private Vector3Int _chestPosition;

        public CordUI cordUI;

        public bool chatWindowOpen = false;

        private bool isMobile => SystemInfo.deviceType != DeviceType.Desktop;
        private void Awake()
        {
            LoadingScreen.SetActive(true);
            Instance = this;
        }

        public void Initialize()
        {
            InventoryWindow.CraftController.Init();
            InventoryWindow.InventoryController.Init();
            InventoryWindow.ChestController.Init();

            OpenInventory();
            CloseInventory();
            OpenChat();
            CloseChat();
            HealthView.InitializeHealth(PlayerController.gameObject.GetComponent<Health>());
            hungerUI.InitializeHealth(PlayerController.gameObject.GetComponent<HungerSystem>());

            cordUI.gameObject.SetActive(false);
            jumpButton.SetActive(isMobile);
        }

        #region MobileInput

        public void Jump()
        {
            if(PlayerController != null)
                PlayerController.HandleJump();
        }

        #endregion

        #region Inventorty

        public void OpenCloseInventory()
        {
            if (InventoryWindow.gameObject.activeSelf)
            {
                CloseInventory();
                CloseChest();
                inventoryUpdated = true;
            }
            else
            {
                OpenInventory();
            }
        }

        private void OpenInventory()
        {
            if(!isMobile)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
            InventoryWindow.Open();
        }

        private void CloseInventory()
        {
            if(!isMobile)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            InventoryWindow.Close();
        }

        #endregion

        #region Chat

        public void OpenCloseChat()
        {
            if (ChatWindow.gameObject.activeSelf)
            {
                CloseChat();
                inventoryUpdated = true;
            }
            else
            {
                OpenChat();
            }
        }

        private void OpenChat()
        {
            if (!isMobile)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
            chatWindowOpen = true;
            ChatWindow.Open();
        }

        private void CloseChat()
        {
            if(!isMobile)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            chatWindowOpen = false;
            ChatWindow.Close();
        }

        #endregion

        #region Chest

        public void OpenCloseChest(List<ItemInSlot> slots, Vector3Int position)
        {
            InventoryWindow.gameObject.SetActive(true);
            OpenChest(slots, position);
            inventoryUpdated = true;
        }

        public void OpenChest(List<ItemInSlot> slots, Vector3Int position)
        {
            if (!isMobile)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }

            InventoryWindow.OpenChest(slots, position);
        }

        public void CloseChest()
        {
            if(!isMobile)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            InventoryWindow.CloseChest();
        }

        public void UpdateInventory(List<ItemInSlot> slots)
        {
            InventoryWindow.InventoryController.UpdateUI(slots);
        }

        #endregion

        #region Pause

        public void OpenClosePause()
        {
            CloseInventory();
            CloseChest();
            CloseChat();
            if (PauseScreen.activeSelf)
            {
                ClosePause();
            }
            else
            {
                OpenPause();
            }
        }

        private void OpenPause()
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            PauseScreen.SetActive(true);
        }

        private void ClosePause()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            PauseScreen.SetActive(false);
        }

        #endregion

        public void MainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void CloseLoadingScreen()
        {
            LoadingScreen.SetActive(false);
        }
        
    }
}