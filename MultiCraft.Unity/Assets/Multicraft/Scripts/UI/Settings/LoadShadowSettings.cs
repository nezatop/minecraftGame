using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace MultiCraft.Scripts.Settings.UI
{
    public class LoadShadowSettings : MonoBehaviour
    {
        public bool shadows;

        private const string ShadowsKey = "ShadowToggleState";

        [Obsolete("Obsolete")]
        private void Awake()
        {
            LoadShadowToggleState();
            QualitySettings.shadows = shadows ? ShadowQuality.All : ShadowQuality.Disable;
            foreach (Light light1 in FindObjectsOfType<Light>())
            {
                light1.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            }
        }

        private void LoadShadowToggleState()
        {
            shadows = PlayerPrefs.GetInt(ShadowsKey, 0) == 1;
        }
    }
}