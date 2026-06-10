using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public string titleSceneName = "TitleScene";
    public string gameSceneName = "GameScene";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
    public void GameOver()
    {
        GameDataManager.Instance.SaveGameResult();
        GoTitle();
    }
    public void GoTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}