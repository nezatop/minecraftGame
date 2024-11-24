using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageReveal : MonoBehaviour
{
    public Image blackScreen; // Перетащите сюда черный фон
    public Image revealImage; // Перетащите сюда изображение, которое нужно показать
    public float revealDuration = 1f; // Время анимации

    void Start()
    {
        StartCoroutine(RevealImage());
    }

    private IEnumerator RevealImage()
    {
        // Убедимся, что изображение полностью скрыто
        Color color = blackScreen.color;
        color.a = 1; // Полностью черный
        blackScreen.color = color;

        // Постепенно уменьшаем альфа-канал черного фона
        float elapsedTime = 0f;
        while (elapsedTime < revealDuration)
        {
            float alpha = Mathf.Lerp(1, 0, elapsedTime / revealDuration);
            color.a = alpha;
            blackScreen.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Убедитесь, что альфа-канал установлен в 0 после окончания анимации
        color.a = 0;
        blackScreen.color = color;
    }
}