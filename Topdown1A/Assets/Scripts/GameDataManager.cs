using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // 💡 타이틀 UI에 텍스트를 뿌리기 위해 추가 필수!

public class GameDataManager : MonoBehaviour
{
    private static GameDataManager instance;
    public static GameDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameDataManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameDataManager");
                    instance = go.AddComponent<GameDataManager>();
                }
            }
            return instance;
        }
    }

    public SaveData saveData = new SaveData();
    public bool isLoadTriggered = false;

    private string filePath;

    [Header("== 타이틀 UI 연결 ==")]
    [SerializeField] private TMP_Text recordText; // 📊 타이틀 화면에 기록을 표시할 텍스트 컴포넌트

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        filePath = Path.Combine(Application.persistentDataPath, "saveData.json");
    }

    void Start()
    {
        // 🔄 타이틀 화면이 켜지자마자 저장되어 있던 JSON 파일과 최고 기록을 읽어옵니다.
        LoadjsonData();
        UpdateTitleRecordUI();
    }

    // 💾 데이터를 JSON 파일로 쓰는 함수
    public void SaveJsonData()
    {
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(filePath, json);
        Debug.Log($"💾 JSON 파일 저장 완료: {filePath}");
    }

    // 📂 JSON 파일을 읽어서 saveData 변수에 채워넣는 함수
    public void LoadjsonData()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            saveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("📂 JSON 파일 로드 및 saveData 변수 동기화 완료!");
        }
        else
        {
            Debug.LogWarning("⚠️ 세이브 파일이 존재하지 않습니다!");
        }
    }

    // 🛒 타이틀 화면의 '불러오기' 버튼이 직접 호출할 함수
    public void TitleLoadGame()
    {
        if (File.Exists(filePath))
        {
            isLoadTriggered = true;
            Debug.Log("🎯 [불러오기] 버튼 클릭됨! isLoadTriggered = true 설정 완료.");
            SceneManager.LoadScene("Stage_1");
        }
        else
        {
            Debug.LogWarning("❌ 세이브 파일이 없어서 게임을 불러올 수 없습니다.");
        }
    }

    // 💡 GameManager의 GameOver()에서 호출하는 게임 결과 저장용 함수
    public void SaveGameResult()
    {
        SaveJsonData();
        Debug.Log("GameManager의 요청으로 게임 오버 결과가 JSON 파일에 안전하게 저장되었습니다.");
    }

    // 🧹 모든 세이브 데이터와 로그라이크 보너스를 태초의 상태로 되돌리는 함수
    public void ResetAllData()
    {
        // 1. JSON 세이브 파일 삭제
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("🗑️ JSON 세이브 파일이 성공적으로 삭제되었습니다.");
        }

        // 2. saveData 변수도 깨끗하게 초기화
        saveData = new SaveData();

        // 3. PlayerPrefs에 저장되어 있던 로그라이크 보너스(돈, 씨앗)도 0으로 싹 밀기
        PlayerPrefs.DeleteKey("LegacyBonus");
        PlayerPrefs.DeleteKey("SeedBonus");
        PlayerPrefs.DeleteKey("BestSurvivedDay"); // 🏆 최고 점수 기록도 초기화 대상에 추가!
        PlayerPrefs.Save();

        // UI도 즉시 0일로 동기화 갱신
        UpdateTitleRecordUI();

        Debug.Log("🌱 모든 게임 데이터가 기본값으로 완벽하게 초기화되었습니다!");
    }

    // =========================================================================
    // 🏆 [기획 반영] PlayerPrefs(최고점수)와 JSON(직전 성적표)을 UI에 그리는 핵심 함수
    // =========================================================================
    public void UpdateTitleRecordUI()
    {
        if (recordText == null)
        {
            Debug.LogWarning("⚠️ 타이틀 화면의 RecordText(TMP_Text) 조립칸이 비어 있습니다!");
            return;
        }

        // 1. PlayerPrefs에서 역대 최고 생존 기록 가져오기 (기본값 0일)
        int bestDay = PlayerPrefs.GetInt("BestSurvivedDay", 0);

        string uiText = $"🏆 역대 최고 점수: {bestDay}일 버팀\n";
        uiText += "--------------------------------------\n";
        uiText += "📊 역대 농사 성적 기록 리스트\n";

        // 2. JSON 데이터에서 누적 기록 리스트 가져오기
        var records = saveData.historyRecords;

        if (records == null || records.Count == 0)
        {
            uiText += "   아직 플레이 기록이 없습니다.";
        }
        else
        {
            // 최신 기록부터 위에 보이도록 뒤에서부터 거꾸로 반복문을 돌립니다.
            for (int i = records.Count - 1; i >= 0; i--)
            {
                uiText += $"   [{records[i].playNumber}회차] {records[i].survivedDays}일 생존 / {records[i].totalMoney}원\n";
            }
        }

        // 3. 최종 조립된 문자열을 UI 텍스트 상자에 주입합니다.
        recordText.text = uiText;
    }
}