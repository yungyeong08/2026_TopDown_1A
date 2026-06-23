using UnityEngine;
using UnityEngine.SceneManagement; // 💡 씬 전환(LoadScene)을 위해 필수!

public class PauseMenuController : MonoBehaviour
{
    [Header("== 일시정지 UI 패널 연결 ==")]
    public GameObject pauseMenuPanel; // 하이라키의 PauseMenuPanel을 연결하는 칸

    private bool isPaused = false;

    void Update()
    {
        // ⌨️ 플레이어가 ESC 키를 눌렀을 때 작동
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // 💡 게임을 멈추고 메뉴창을 띄우는 함수
    public void PauseGame()
    {
        isPaused = true;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true); // 일시정지 UI 창 켜기
        }

        // ⏱️ 타임스케일을 0으로 만들어 인게임 시간 흐름과 타이머를 완전히 정지시킵니다.
        Time.timeScale = 0f;
    }

    // 💡 메뉴창을 닫고 게임으로 돌아가는 함수
    public void ResumeGame()
    {
        isPaused = false;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false); // 일시정지 UI 창 끄기
        }

        // ⏱️ 정상적인 인게임 플레이를 위해 시간 배율을 다시 1배속으로 복구합니다.
        Time.timeScale = 1f;
    }

    // 💡 [메인 화면으로] 버튼에 연결할 함수
    public void GoToMainMenu()
    {
        // 🔥 매우 중요: 타임스케일이 0f(정지)인 상태로 메인 화면에 가면 
        // 메인 화면 UI나 애니메이션까지 먹통이 되므로, 넘어가기 전에 반드시 1f로 풀어줍니다.
        Time.timeScale = 1f;

        // 📂 인스펙터 Build Settings에 등록된 메인 타이틀 씬 이름을 적어주세요.
        // 현재 사용 중인 메인 씬 이름이 다를 경우 "Title" 부분을 해당 이름으로 수정하시면 됩니다.
        SceneManager.LoadScene("Title");
    }
}