using UnityEngine;
using UnityEngine.UI;
using Lean.Localization;

public class LanguageSelector : MonoBehaviour
{
    public GameObject languagePanel; // Ссылка на панель выбора языка
    public Button englishButton; // Кнопка для выбора английского
    public Button russianButton; // Кнопка для выбора русского

    private void Start()
    {
        // Скрыть панель выбора языка в начале
        languagePanel.SetActive(false);

        // Привязать методы к кнопкам
        englishButton.onClick.AddListener(() => ChangeLanguage("en"));
        russianButton.onClick.AddListener(() => ChangeLanguage("ru"));
    }

    public void ToggleLanguagePanel()
    {
        // Переключить видимость панели
        languagePanel.SetActive(!languagePanel.activeSelf);
    }

    private void ChangeLanguage(string languageCode)
    {
        languagePanel.SetActive(false); // Скрыть панель после выбора
    }
}