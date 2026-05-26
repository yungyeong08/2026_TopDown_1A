using UnityEngine;

public class QuitGame : MonoBehaviour
{
    // 버튼에 연결할 함수 (반드시 public이어야 합니다!)
    public void ExitGame()
    {
        // 1. 실제 빌드된 게임(PC, 모바일 등)을 종료하는 명령어
        Application.Quit();

        // 2. 유니티 에디터 안에서 'Play' 모드를 종료하는 명령어 (테스트용)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}