using UnityEngine;
using UnityEngine.Serialization;

namespace MultiCraft.Scripts.Settings.UI
{
    public class Settings : MonoBehaviour
    {
        [Header("Settings screens")] 
        public GameObject graphicsScreen;
        public GameObject audioScreen;
        public GameObject controlsScreen;
        public GameObject languageScreen;
        public GameObject creditScreen;

        private void OnEnable()
        {
            CloseAllScreens();
            OpenAudio();
        }

        public void OpenGraphics()
        {
            CloseAllScreens();
            graphicsScreen.SetActive(true);
        }

        public void OpenAudio()
        {
            CloseAllScreens();
            audioScreen.SetActive(true);
        }

        public void OpenControls()
        {
            CloseAllScreens();
            controlsScreen.SetActive(true);
        }

        public void OpenLanguage()
        {
            CloseAllScreens();
            languageScreen.SetActive(true);
        }
        
        public void OpenCredit()
        {
            CloseAllScreens();
            creditScreen.SetActive(true);
        }

        private void CloseAllScreens()
        {
            graphicsScreen.SetActive(false);
            audioScreen.SetActive(false);
            controlsScreen.SetActive(false);
            languageScreen.SetActive(false);
            creditScreen.SetActive(false);
        }
    }
}