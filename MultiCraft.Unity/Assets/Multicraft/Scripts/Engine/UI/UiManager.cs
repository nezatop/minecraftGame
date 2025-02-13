using System;
using System.Collections.Generic;
using System.Linq;
using MultiCraft.Scripts.Engine.Core.HealthSystem;
using Multicraft.Scripts.Engine.Core.Hunger;
using MultiCraft.Scripts.Engine.Core.Inventories;
using MultiCraft.Scripts.Engine.Core.Player;
using MultiCraft.Scripts.Engine.Core.Worlds;
using MultiCraft.Scripts.Engine.Network;
using MultiCraft.Scripts.Engine.Utils.Commands;
using MultiCraft.Scripts.Engine.Utils.MulticraftDebug;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

namespace MultiCraft.Scripts.Engine.UI
{
    public class UiManager : MonoBehaviour
    {
        public static UiManager Instance;

        public InventoryWindow InventoryWindow;
        public ChatWindow ChatWindow;
        public GameObject LoadingScreen;

        public List<GameObject> MobileInputObjects;

        public HealthView HealthView;
        public HungerUI hungerUI;

        public GameObject GameOverScreen;
        public GameObject PauseScreen;

        public bool inventoryUpdated = false;

        public PlayerController PlayerController;

        private MultiCraft.Scripts.Engine.Network.Player.DestroyAndPlaceBlockController
            NetDestroyAndPlaceBlockController;

        private DestroyAndPlaceBlockController DestroyAndPlaceBlockController;

        private Vector3Int _chestPosition;

        public CordUI cordUI;

        public bool chatWindowOpen = false;

        private bool isMobile => YG2.envir.isMobile;

        private void Awake()
        {
            LoadingScreen.SetActive(true);
            Instance = this;
        }
        
        public void Initialize()
        {
            LockCursor();
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
            foreach (GameObject obj in MobileInputObjects)
                obj.SetActive(isMobile);
        }

        private void Update()
        {
            if (LeftHold)
                if (PlayerController)
                    PlayerController.LeftClickHandle();
        }

        public bool LeftHold { get; set; }

        public void LockCursor()
        {
            if (!isMobile)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        
        #region MobileInput

        public void Jump()
        {
            if (PlayerController != null)
                PlayerController.HandleJump();
        }

        public void LeftClickDown()
        {
            LeftHold = true;
        }

        public void LeftClickUp()
        {
            LeftHold = false;
            
            if (PlayerController != null)
                PlayerController.StopBreaking();
        }

        public void RightClick()
        {
            if (PlayerController != null)
                PlayerController.RightClickHandle();
        }

        public void InventoryOpen()
        {
            if (OpenCloseInventory())
                PlayerController.Disable();
            else
                PlayerController.Enable();
        }

        public void MenuOpen()
        {
            if (OpenClosePause())
                PlayerController.Disable();
            else
                PlayerController.Enable();
        }

        public void SetSelectedItem(int slotId)
        {
            PlayerController.inventory.UpdateHotBarSelectedSlot(slotId);
                
        }

        #endregion

        #region Inventorty

        public bool OpenCloseInventory()
        {
            CloseDead();
            CloseChat();
            ClosePause();
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

            return InventoryWindow.gameObject.activeSelf;
        }

        private void OpenInventory()
        {
            UnlockCursor();
            InventoryWindow.Open();
        }

        private void CloseInventory()
        {
            LockCursor();
            InventoryWindow.Close();
        }

        #endregion

        #region Chat

        public bool OpenCloseChat()
        {
            CloseChest();
            CloseDead();
            CloseInventory();
            ClosePause();
            if (ChatWindow.gameObject.activeSelf)
            {
                CloseChat();
                inventoryUpdated = true;
            }
            else
            {
                OpenChat();
            }

            return ChatWindow.gameObject.activeSelf;
        }

        private void OpenChat()
        {
            UnlockCursor();
            chatWindowOpen = true;
            ChatWindow.Open();
        }

        private void CloseChat()
        {
            LockCursor();
            chatWindowOpen = false;
            ChatWindow.Close();
        }

        #endregion

        #region Chest

        public bool OpenCloseChest(List<ItemInSlot> slots, Vector3Int position)
        {
            InventoryWindow.gameObject.SetActive(true);
            OpenChest(slots, position);
            inventoryUpdated = true;

            return InventoryWindow.gameObject.activeSelf;
        }

        public void OpenChest(List<ItemInSlot> slots, Vector3Int position)
        {
            UnlockCursor();
            InventoryWindow.OpenChest(slots, position);
        }

        public void CloseChest()
        {
            LockCursor();
            InventoryWindow.CloseChest();
        }

        public void UpdateInventory(List<ItemInSlot> slots)
        {
            InventoryWindow.InventoryController.UpdateUI(slots);
        }

        #endregion

        #region Pause

        public bool OpenClosePause()
        {
            CloseInventory();
            CloseChest();
            CloseChat();
            CloseDead();
            if (PauseScreen.activeSelf)
            {
                ClosePause();
            }
            else
            {
                OpenPause();
            }

            return PauseScreen.gameObject.activeSelf;
        }

        private void OpenPause()
        {
            UnlockCursor();
            PauseScreen.SetActive(true);
        }

        private void ClosePause()
        {
            LockCursor();
            PauseScreen.SetActive(false);
        }

        #endregion

        #region GameOverMenu

        public bool OpenCloseDead()
        {
            CloseInventory();
            CloseChest();
            CloseChat();
            ClosePause();
            if (GameOverScreen.activeSelf)
            {
                CloseDead();
            }
            else
            {
                OpenDead();
            }

            return GameOverScreen.gameObject.activeSelf;
        }

        private void OpenDead()
        {
            UnlockCursor();
            GameOverScreen.SetActive(true);
        }

        public void CloseDead()
        {
            LockCursor();
            GameOverScreen.SetActive(false);
        }

        #endregion

        #region Buttons

        public void MainMenu()
        {
            if(NetworkManager.Instance)
                NetworkManager.Instance.DisconnectPlayer();
            SceneManager.LoadScene("MainMenu");
        }

        public void Restart()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.RespawnPlayer();
            }

            if (World.Instance != null)
            {
                World.Instance.RespawnPlayers();
            }
        }

        #endregion

        public void CloseLoadingScreen()
        {
            LoadingScreen.SetActive(false);
        }
    }
}