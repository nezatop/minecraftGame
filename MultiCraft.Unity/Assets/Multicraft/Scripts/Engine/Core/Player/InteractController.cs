using System;
using System.Collections.Generic;
using MultiCraft.Scripts.Engine.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiCraft.Scripts.Engine.Core.Player
{
    public class InteractController : MonoBehaviour
    {
        public List<MonoBehaviour> ScriptsToDisable = null;
        public Camera mainCamera;

        private InputSystem_Actions _inputSystem;

        private bool _activeInventory = false;
        private bool _activeChat = false;

        private void Awake()
        {
            _inputSystem = new InputSystem_Actions();
            _inputSystem.Enable();
        }

        private void OnEnable()
        {
            _inputSystem.Player.OpenChat.performed += OpenChat;
            _inputSystem.Player.OpenInventory.performed += OpenInventory;
            _inputSystem.Player.exit.performed += OpenPouseMenu;
        }

        private void OnDisable()
        {
            _inputSystem.Player.OpenChat.performed -= OpenChat;
            _inputSystem.Player.OpenInventory.performed -= OpenInventory;
            _inputSystem.Player.exit.performed -= OpenPouseMenu;
        }

        private void OnDestroy()
        {
            _inputSystem.Player.Disable();
            _inputSystem.UI.Disable();

            _inputSystem.Disable();
        }

        public void EnableScripts()
        {
            foreach (var script in ScriptsToDisable)
            {
                script.enabled = true;
            }

            mainCamera.GetComponent<CameraController>().enabled = true;
            mainCamera.GetComponent<DestroyAndPlaceBlockController>().enabled = true;
            //mainCamera.GetComponent<HighLightController>().enabled = true;
        }

        public void DisableScripts()
        {
            foreach (var script in ScriptsToDisable)
            {
                script.enabled = false;
            }

            mainCamera.GetComponent<CameraController>().enabled = false;
            mainCamera.GetComponent<DestroyAndPlaceBlockController>().enabled = false;
            //mainCamera.GetComponent<HighLightController>().enabled = false;
        }

        private void OpenInventory(InputAction.CallbackContext obj)
        {
            if (_activeChat) return;
            if (UiManager.Instance.OpenCloseInventory())
                DisableScripts();
            else
                EnableScripts();
            _activeInventory = !_activeInventory;
        }

        private void OpenPouseMenu(InputAction.CallbackContext obj)
        {
            if (UiManager.Instance.OpenClosePause())
                DisableScripts();
            else
                EnableScripts();
            if (_activeChat) return;
            _activeInventory = !_activeInventory;
        }

        private void OpenChat(InputAction.CallbackContext obj)
        {
            if (_activeInventory) return;
            _activeChat = !_activeChat;
            if (UiManager.Instance.OpenCloseChat())
                DisableScripts();
            else
                EnableScripts();
        }

        public void OpenChat()
        {
            if (_activeInventory) return;
            _activeChat = !_activeChat;
            if (UiManager.Instance.OpenCloseChat())
                DisableScripts();
            else
                EnableScripts();
        }
    }
}