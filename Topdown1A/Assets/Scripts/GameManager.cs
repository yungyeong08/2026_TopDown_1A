using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public string gameSceneName = "Stage_1";  // 본인의 게임 씬 이름
    public string titleSceneName = "Title";    // 본인의 타이틀 씬 이름

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void GameOver()
    {
        // 💡 중요: GameDataManager가 정상적으로 존재할 때만 결과를 저장하도록 안전장치를 합니다.
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SaveGameResult();
        }
        GoTitle();
    }

    public void GoTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }

    public void ExitGame()
    {
        // 나갈 때 데이터 초기화 먼저 수행!
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ResetAllData();
        }

        // 에디터 종료 및 실제 빌드 게임 종료 처리
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}