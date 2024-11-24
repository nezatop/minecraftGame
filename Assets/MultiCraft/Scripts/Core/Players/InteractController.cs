using System.Collections.Generic;
using MultiCraft.Scripts.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiCraft.Scripts.Core.Players
{
    public class InteractController: MonoBehaviour
    {
        public List<MonoBehaviour> ScriptsToDisable = null;
        public Camera mainCamera;

        private InputSystem_Actions _inputSystem;

        private void Awake()
        {
            _inputSystem = new InputSystem_Actions();
            _inputSystem.Enable();
        }

        private void OnEnable()
        {
            _inputSystem.Player.OpenInventory.performed += OpenInventory;
        }

        private void OnDisable()
        {
            _inputSystem.Player.OpenInventory.performed -= OpenInventory;
        }

        private void OpenInventory(InputAction.CallbackContext obj)
        {
            
            foreach (var script in ScriptsToDisable)
            {
                script.enabled = !script.enabled;
            }

            mainCamera.GetComponent<CameraController>().enabled = !mainCamera.GetComponent<CameraController>().enabled;
            mainCamera.GetComponent<DestroyAndPlaceBlockController>().enabled =
                !mainCamera.GetComponent<DestroyAndPlaceBlockController>().enabled;
            mainCamera.GetComponent<HighLightController>().enabled =
                !mainCamera.GetComponent<HighLightController>().enabled;

            UiManager.Instance.OpenCloseInventory();
        }
    }
}