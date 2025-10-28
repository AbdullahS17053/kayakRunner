using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Button = UnityEngine.UI.Button;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button pauseBtn;
    [SerializeField] private GameObject pauseUI;

    public void Pause()
    {
        pauseUI.SetActive(true);
        pauseBtn.gameObject.SetActive(false);
        Time.timeScale = 0f;
    }
    public void Resume()
    {
        Time.timeScale = 1f;
        pauseUI.SetActive(false);
        pauseBtn.gameObject.SetActive(true);
    }
    public void Quit()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    public void MenuBtn()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
    public void PlayBtn()
    {
        SceneManager.LoadSceneAsync(1);
    }
    public void Restart()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
