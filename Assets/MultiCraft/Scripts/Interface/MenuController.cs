using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void OnSingleplayButtonClicked()
    {
        SceneManager.LoadScene("Loading"); // Переход на сцену загрузки
    }
}