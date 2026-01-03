using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string newGameSceneName = "Game_Main";

    public void OnNewGame()
    {
        // Load the main gameplay scene
        SceneManager.LoadScene(newGameSceneName);
    }

    public void OnContinue()
    {
        // TODO: hook up save/load later.
        // For now you can just start a new game as a placeholder:
        SceneManager.LoadScene(newGameSceneName);
    }

    public void OnOptions()
    {
        // TODO: show options UI later.
        Debug.Log("Options menu not implemented yet.");
    }

    public void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
