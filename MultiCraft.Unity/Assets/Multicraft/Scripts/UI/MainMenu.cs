using MultiCraft.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace MultiCraft.Scripts.UI
{
    public class MainMenu : MonoBehaviour
    {
        [Header("Screens")] public GameObject loadingScreen;
        public GameObject settingsScreen;

        private void Awake()
        {
            loadingScreen.SetActive(true);

            CloseSettings();

            loadingScreen.SetActive(false);
        }


        public void SinglePlayer()
        {
            SceneManager.LoadScene("Boot");
            SceneManager.LoadScene("Gameplay");
        }

        public void Multiplayer()
        {
            SceneManager.LoadScene("Boot");
            SceneManager.LoadScene("Multiplayer");
        }

        public void OpenSettings()
        {
            settingsScreen.SetActive(true);
        }

        public void CloseSettings()
        {
            settingsScreen.SetActive(false);
        }
    }
}