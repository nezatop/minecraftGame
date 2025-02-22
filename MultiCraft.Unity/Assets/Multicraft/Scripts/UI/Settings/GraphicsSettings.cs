using System;
using MultiCraft.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using YG;

namespace MultiCraft.Scripts.Settings.UI
{
    public class GraphicsSettings : MonoBehaviour
    {
        [Header("Sliders")] 
        public Slider renderDistanceSlider;
        public TMP_Text renderDistanceText;
        public LocalizationText renderDistanceLocalization;

        private const string RenderDistanceKey = "RenderDistance";

        private void OnEnable()
        {
            Load();

            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (renderDistanceSlider != null)
                renderDistanceSlider.onValueChanged.RemoveListener(SetRenderDistance);
        }

        private void Load()
        {
            if (PlayerPrefs.HasKey(RenderDistanceKey))
            {
                renderDistanceSlider.value = PlayerPrefs.GetFloat(RenderDistanceKey);
                SetRenderDistance(PlayerPrefs.GetFloat(RenderDistanceKey));
            }
            else
            {
                var renderDistance =2;
                if (YG2.envir.isMobile)
                    renderDistance = 2;
                SetRenderDistance(renderDistance);
                renderDistanceSlider.value = renderDistance;
            }
        }
        
        private void Subscribe()
        {
            if (renderDistanceSlider != null)
                renderDistanceSlider.onValueChanged.AddListener(SetRenderDistance);
        }

        private void SetRenderDistance(float volume)
        {
            renderDistanceText.text = string.Format(renderDistanceLocalization.GetText(), volume);
            PlayerPrefs.SetFloat(RenderDistanceKey, volume);
            PlayerPrefs.Save();
        }
    }
}
