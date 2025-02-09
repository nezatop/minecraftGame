using UnityEngine;
using UnityEngine.UI;

namespace MultiCraft.Scripts.Settings.UI
{
    public class ShadowSettings : MonoBehaviour
    {
        public Image Image;

        public Sprite toggleOn;
        public Sprite toggleOff;

        public bool toggle;

        private const string ToggleKey = "ShadowToggleState";

        public void Awake()
        {
            LoadToggleState();
        }

        public void Toggle()
        {
            toggle = !toggle;
            Image.sprite = toggle ? toggleOn : toggleOff;
            SaveToggleState();
        }

        private void SaveToggleState()
        {
            PlayerPrefs.SetInt(ToggleKey, toggle ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadToggleState()
        {
            toggle = PlayerPrefs.GetInt(ToggleKey, 0) == 1;
            Image.sprite = toggle ? toggleOn : toggleOff;
        }
    }
}