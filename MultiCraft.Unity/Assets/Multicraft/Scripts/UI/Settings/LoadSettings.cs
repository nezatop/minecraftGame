using UnityEngine;

namespace MultiCraft.Scripts.Settings.UI
{
    public class LoadSettings : MonoBehaviour
    {
        public bool Shadows;
        public bool DayNight;
        
        private const string ShadowsKey = "DayAndNightToggleState";
        private const string DayNightKey = "ShadowToggleState";
        
        private void LoadShadowToggleState()
        {
            Shadows = PlayerPrefs.GetInt(ShadowsKey, 0) == 1;
            Shadows = PlayerPrefs.GetInt(DayNightKey, 0) == 1;
        }
    }
}