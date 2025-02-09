using UnityEngine;

namespace MultiCraft.Scripts.Settings.UI
{
    public class LoadShadowSettings : MonoBehaviour
    {
        public bool Shadows;

        private const string ShadowsKey = "ShadowToggleState";

        private void Awake()
        {
            LoadShadowToggleState();
            QualitySettings.shadows = Shadows ? ShadowQuality.All : ShadowQuality.Disable;
            foreach (Light light in FindObjectsOfType<Light>())
            {
                light.shadows = Shadows ? LightShadows.Soft : LightShadows.None;
            }
        }

        private void LoadShadowToggleState()
        {
            Shadows = PlayerPrefs.GetInt(ShadowsKey, 0) == 1;
        }
    }
}