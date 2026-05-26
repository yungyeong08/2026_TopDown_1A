using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위해 반드시 필요한 네임스페이스

public class SceneChanger : MonoBehaviour
{
    // 버튼이 눌렸을 때 실행할 함수 (반드시 public이어야 버튼에 연결할 수 있습니다)
    public void ChangeGameScene()
    {
        // "GameScene" 자리에 이동하고 싶은 실제 게임 씬의 이름을 정확히 적어주세요.
        SceneManager.LoadScene("Stage_1");
    }
}