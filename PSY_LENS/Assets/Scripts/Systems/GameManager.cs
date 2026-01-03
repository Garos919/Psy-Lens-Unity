// GameManager.cs
// Central global coordinator for Psy-Lens systems
// Handles global state, references, and scene-wide events

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Global References")]
    public PlayerController player;
    public CameraManager cameraManager;
    public Canvas globalUI;

    [Header("Game State")]
    public bool isPaused = false;

    void Awake()
    {
        // enforce singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // global pause toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;

        // optional: show pause menu later
        if (globalUI != null)
        {
            Transform pauseMenu = globalUI.transform.Find("PauseMenu");
            if (pauseMenu != null)
                pauseMenu.gameObject.SetActive(isPaused);
        }
    }

    public void ReloadScene()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
