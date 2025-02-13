using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace MultiCraft.Scripts.Settings.UI
{
    public class AudioSettings : MonoBehaviour
    {
        [Header("Sliders")] 
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider soundVolumeSlider;
        public Slider environmentVolumeSlider;

        [Header("AudioMixer")]
        public AudioMixer audioMixer;
        
        [Header("Default Audio Settings")]
        [Range(0f,1f)]public float defaultMasterVolume;
        [Range(0f,1f)]public float defaultMusicVolume;
        [Range(0f,1f)]public float defaultSoundVolume;
        [Range(0f,1f)]public float defaultEnvironmentVolume;

        private const string MasterVolumeParam = "Master";
        private const string MusicVolumeParam = "Music";
        private const string SoundVolumeParam = "Sounds";
        private const string EnvironmentVolumeParam = "Enviroment";

        private const string MasterVolumeKey = "MasterVolume";
        private const string MusicVolumeKey = "MusicVolume";
        private const string SoundVolumeKey = "SoundVolume";
        private const string EnvironmentVolumeKey = "EnvironmentVolume";

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
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);

            if (soundVolumeSlider != null)
                soundVolumeSlider.onValueChanged.RemoveListener(SetSoundVolume);

            if (environmentVolumeSlider != null)
                environmentVolumeSlider.onValueChanged.RemoveListener(SetEnvironmentVolume);
        }

        private void Subscribe()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);

            if (soundVolumeSlider != null)
                soundVolumeSlider.onValueChanged.AddListener(SetSoundVolume);

            if (environmentVolumeSlider != null)
                environmentVolumeSlider.onValueChanged.AddListener(SetEnvironmentVolume);
        }

        private void Load()
        {
            masterVolumeSlider.value = PlayerPrefs.HasKey(MasterVolumeKey) ? PlayerPrefs.GetFloat(MasterVolumeKey) : defaultMasterVolume;
            musicVolumeSlider.value = PlayerPrefs.HasKey(MusicVolumeKey) ? PlayerPrefs.GetFloat(MusicVolumeKey) : defaultMusicVolume;
            soundVolumeSlider.value = PlayerPrefs.HasKey(SoundVolumeKey) ? PlayerPrefs.GetFloat(SoundVolumeKey) : defaultSoundVolume;
            environmentVolumeSlider.value = PlayerPrefs.HasKey(EnvironmentVolumeKey) ? PlayerPrefs.GetFloat(EnvironmentVolumeKey) : defaultEnvironmentVolume;

            SetMasterVolume(masterVolumeSlider.value);
            SetMusicVolume(musicVolumeSlider.value);
            SetSoundVolume(soundVolumeSlider.value);
            SetEnvironmentVolume(environmentVolumeSlider.value);
        }
        
        private void SetMasterVolume(float volume)
        {
            SetMixerVolume(MasterVolumeParam, volume);
            PlayerPrefs.SetFloat(MasterVolumeKey, volume);
            PlayerPrefs.Save();
        }

        private void SetMusicVolume(float volume)
        {
            SetMixerVolume(MusicVolumeParam, volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, volume);
            PlayerPrefs.Save();
        }

        private void SetSoundVolume(float volume)
        {
            SetMixerVolume(SoundVolumeParam, volume);
            PlayerPrefs.SetFloat(SoundVolumeKey, volume);
            PlayerPrefs.Save();
        }

        private void SetEnvironmentVolume(float volume)
        {
            SetMixerVolume(EnvironmentVolumeParam, volume);
            PlayerPrefs.SetFloat(EnvironmentVolumeKey, volume);
            PlayerPrefs.Save();
        }

        private void SetMixerVolume(string parameter, float sliderValue)
        {
            var dB = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20;
            audioMixer.SetFloat(parameter, dB);
        }
    }
}