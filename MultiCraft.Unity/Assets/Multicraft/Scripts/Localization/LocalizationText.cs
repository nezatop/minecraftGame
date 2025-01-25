using TMPro;
using UnityEngine;
using YG;

namespace MultiCraft.Scripts.Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizationText : MonoBehaviour
    {
        public string ru, en;
       
        private TMP_Text _text;
        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            SwitchLanguage(YG2.lang);
        }
        private void OnEnable()
        {
            YG2.onSwitchLang += SwitchLanguage;
            SwitchLanguage(YG2.lang);
        }
        private void OnDisable()
        {
            YG2.onSwitchLang -= SwitchLanguage;
        }

        public string GetText()
        {
            var lang = YG2.lang;
            return lang switch
            {
                "ru" => ru,
                _ => en
            };
        }
        
        private void SwitchLanguage(string lang)
        {
            _text.text = lang switch
            {
                "ru" => ru,
                _ => en
            };
        }
    }
}