using System;
using RageRunGames.KayakController;
using UnityEngine;
using UnityEngine.SceneManagement;
using Button = UnityEngine.UI.Button;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button pauseBtn;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private CanvasGroup levelLostUI;
    [SerializeField] private GameObject lostPanel;

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
    public void GameOver()
    {
        levelLostUI.gameObject.SetActive(true);
        pauseBtn.gameObject.SetActive(false);
        levelLostUI.LeanAlpha(1, 0.5f);
        lostPanel.LeanMoveLocalY(0, 0.5f).setEaseOutExpo().delay = 0.1f;
        Invoke(nameof(DisableKayak),0.5f);
    }

    void DisableKayak()
    {
        Time.timeScale = 0f;
    }
}
