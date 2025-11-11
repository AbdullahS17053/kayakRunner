using System;
using RageRunGames.KayakController;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button pauseBtn;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private CanvasGroup levelLostUI;
    [SerializeField] private GameObject lostPanel;
    [SerializeField] private CanvasGroup quitUI;
    [SerializeField] private GameObject quitPanel;

    [Header("References")] [SerializeField]
    private Slider healthBar;

    [SerializeField] private KayakController kayakController;
    
    [SerializeField] private bool isGame;
    
    

    private void Awake()
    {
        if (isGame)
        {
            levelLostUI.alpha = 0f;
            lostPanel.transform.localPosition = new Vector2(0, +Screen.height); 
        }
        else
        {
            quitUI.alpha = 0f;
            quitPanel.transform.localPosition = new Vector2(0, +Screen.height); 
        }
    }
    private void Start()
    {
        // Initialize the health bar value
        if (healthBar != null && kayakController != null)
            healthBar.value = kayakController.health;
    }

    public void Pause()
    {
        AudioController.Instance.PlaySound("CLick");
        pauseUI.SetActive(true);
        Invoke(nameof(DisableKayak),0.5f);
        //pauseBtn.gameObject.SetActive(false);
    }
    public void Resume()
    {
        Time.timeScale = 1f;
        AudioController.Instance.PlaySound("Resume");
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void GameOver()
    {
        AudioController.Instance.PlaySound("Lost");
        levelLostUI.gameObject.SetActive(true);
        //pauseBtn.gameObject.SetActive(false);
        levelLostUI.LeanAlpha(1, 0.5f);
        lostPanel.LeanMoveLocalY(0, 0.5f).setEaseOutExpo().delay = 0.1f;
        Invoke(nameof(DisableKayak),0.5f);
    }

    void DisableKayak()
    {
        pauseBtn.gameObject.SetActive(false);
        Time.timeScale = 0f;
    }
    public void OpenQuitMenu()
    {
        AudioController.Instance.PlaySound("Click");
        quitUI.gameObject.SetActive(true);
        quitUI.LeanAlpha(1, 0.5f);
        quitPanel.LeanMoveLocalY(0, 0.5f).setEaseOutExpo().delay = 0.1f;
    }
    public void CloseQuitMenu()
    {
        AudioController.Instance.PlaySound("Rseume");
        quitUI.LeanAlpha(0, 0.5f);
        quitPanel.LeanMoveLocalY(+Screen.height, 0.5f).setEaseInExpo();
        Invoke(nameof(DisableQuitUI), 0.5f);
    }

    private void DisableQuitUI()
    {
        quitUI.gameObject.SetActive(false);
    }

    public void UpdateHealth()
    {
        if (healthBar != null && kayakController != null)
        {
            // Assuming the slider max value is set to 100 in Inspector
            healthBar.value = kayakController.health;

            // Optional: you can add color feedback or lose condition check
            if (kayakController.health <= 0)
            {
                kayakController.health = 0;
                // Handle player death or game over UI here
                Debug.Log("Player is dead!");
                GameOver();
            }
        }   
    }
}
