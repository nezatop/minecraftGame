using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Loadingscene : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(LoadMainScene());
    }

    private IEnumerator LoadMainScene()
    {
        // Задержка в 2.5 секунды
        yield return new WaitForSeconds(1.5f);

        // Здесь можно добавить логическую часть загрузки (например, анимацию)
        // Загрузка основной сцены
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("Gameplay");
        asyncOperation.allowSceneActivation = false;

        // Пока сцена загружается, вы можете показывать индикатор загрузки
        while (!asyncOperation.isDone)
        {
            // Например, обновить индикатор загрузки
            // Пример: progressBar.fillAmount = asyncOperation.progress;

            // Ожидание завершения загрузки
            if (asyncOperation.progress >= 0.9f)
            {
                // Активируем сцену, когда она загружена
                asyncOperation.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}