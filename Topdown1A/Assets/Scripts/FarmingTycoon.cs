using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using System.IO; // 💾 JSON 파일 입출력을 위해 필수!

public class FarmingTycoon : MonoBehaviour
{
    public static FarmingTycoon Instance;

    [Header("== 타일맵 설정 ==")]
    public Tilemap tilemap;      // 'BackTilemap' 오브젝트 연결용
    public TileBase dirtTile;     // 빈 밭 타일 에셋

    [Header("== 작물 프리팹 리스트 (확장형) ==")]
    // 💡 여러 작물을 대응할 수 있도록 리스트로 관리합니다 (0번: 당근, 1번: 토마토 등)
    public List<GameObject> cropPrefabs = new List<GameObject>();
    private int currentSelectedCropIndex = 0; // 현재 선택된 작물의 번호

    [Header("== 플레이어 참조 ==")]
    public Transform playerTransform; // 플레이어 Transform 연결용

    [Header("== 게임 규칙 & 스테이지 세팅 ==")]
    public int money = 100;
    public int seedCount = 3;
    public float timeLeft = 60f;
    public int taxGoal = 500;
    private int currentDay = 1;   // 현재 날짜(Day) 단계 기록
    private bool isGameOver = false;

    [Header("== UI 연결 ==")]
    public TMP_Text moneyText;      // 💡 보유 자산 텍스트 따로 표기
    public TMP_Text taxGoalText;    // 💡 목표 세금 텍스트 따로 표기
    public TMP_Text seedText;
    public TMP_Text timerText;
    public TMP_Text infoText;

    [Header("== 버튼 UI 연결 ==")]
    public Button payTaxButton;     // 💡 목표 충족 시 활성화될 세금 납부 버튼

    // 게임 내 심어진 작물들을 좌표별로 관리하는 딕셔너리
    private Dictionary<Vector3Int, GameObject> plantedCrops = new Dictionary<Vector3Int, GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 이전 판의 상속 보너스 로드
        int bonus = PlayerPrefs.GetInt("LegacyBonus", 0);
        money += bonus;
        PlayerPrefs.SetInt("LegacyBonus", 0);

        // 시작 단계에서는 세금 납부 버튼 클릭 잠금
        if (payTaxButton != null) payTaxButton.interactable = false;

        UpdateUI();
        if (infoText) infoText.text = $"[Day {currentDay}] 세금을 모아 살아남으세요!";
    }

    void Update()
    {
        if (isGameOver) return;

        // 타이머 카운트다운
        if (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0)
            {
                timeLeft = 0;
                CheckTimeOver();
            }
        }
        UpdateUI();

        // 밭 조작 입력 감지
        if (timeLeft > 0 && playerTransform != null && tilemap != null)
        {
            // [E 키] : 씨앗 심기
            if (Input.GetKeyDown(KeyCode.E))
            {
                Vector3Int cellPos = tilemap.WorldToCell(playerTransform.position);
                TryPlantSeed(cellPos);
            }

            // [F 키] : 내 주변 작물 수확
            if (Input.GetKeyDown(KeyCode.F))
            {
                TryHarvestNearbyCrop();
            }

            // 숫자키 1, 2, 3번으로 등록된 작물 품종 스위칭
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchCrop(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchCrop(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchCrop(2);
        }
    }

    // 작물 품종 바꾸기 함수
    public void SwitchCrop(int index)
    {
        if (index >= 0 && index < cropPrefabs.Count)
        {
            currentSelectedCropIndex = index;
            if (infoText) infoText.text = $"심을 작물이 변경되었습니다! (현재 품종: {cropPrefabs[index].name})";
        }
    }

    // 씨앗 심기 연산
    void TryPlantSeed(Vector3Int cellPos)
    {
        TileBase currentTile = tilemap.GetTile(cellPos);

        // 빈 땅이 아니거나 이미 작물이 존재하면 취소
        if (currentTile == null || plantedCrops.ContainsKey(cellPos))
        {
            if (infoText) infoText.text = "여기는 씨앗을 심을 수 있는 공간이 아닙니다.";
            return;
        }

        if (seedCount <= 0)
        {
            if (infoText) infoText.text = "씨앗이 부족합니다! 상점에서 구매하세요.";
            return;
        }

        if (cropPrefabs.Count == 0 || cropPrefabs[currentSelectedCropIndex] == null)
        {
            if (infoText) infoText.text = "등록된 작물 프리팹 에셋이 없습니다!";
            return;
        }

        Vector3 spawnPos = tilemap.GetCellCenterWorld(cellPos);
        spawnPos.z = 0f;

        seedCount--;

        // 현재 선택된 종류의 작물 소환 및 저장소 등록
        GameObject spawnedCrop = Instantiate(cropPrefabs[currentSelectedCropIndex], spawnPos, Quaternion.identity);
        plantedCrops.Add(cellPos, spawnedCrop);

        Crop cropScript = spawnedCrop.GetComponent<Crop>();
        if (cropScript != null)
        {
            // 💡 몇 번째 작물 프리팹인지 고유 인덱스를 넘겨주며 초기화합니다.
            cropScript.Initialize(currentSelectedCropIndex);
        }

        if (infoText) infoText.text = $"{cropPrefabs[currentSelectedCropIndex].name} 씨앗을 심었습니다! (E)";
        UpdateUI();
    }

    // 주변 반경 범위 탐색 및 수확 연산
    void TryHarvestNearbyCrop()
    {
        Vector3 playerPos = playerTransform.position;
        Vector3Int foundCell = Vector3Int.zero;
        bool cropFound = false;
        int rewardMoney = 150; // 기본 실패 방지용 단가

        Vector3Int centerCell = tilemap.WorldToCell(playerPos);

        // 내 주변 3x3 격자를 검사하여 수확 가능한 첫 타겟을 물색합니다.
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkCell = centerCell + new Vector3Int(x, y, 0);

                if (plantedCrops.ContainsKey(checkCell))
                {
                    GameObject cropObj = plantedCrops[checkCell];
                    Crop cropScript = cropObj.GetComponent<Crop>();

                    // 작물이 완전히 자란 상태(IsGrown)인지 판단
                    if (cropScript != null && cropScript.IsGrown())
                    {
                        foundCell = checkCell;
                        cropFound = true;
                        rewardMoney = cropScript.sellPrice; // 작물 고유 가치 반영
                        break;
                    }
                }
            }
            if (cropFound) break;
        }

        if (cropFound)
        {
            GameObject cropObj = plantedCrops[foundCell];

            money += rewardMoney;
            if (infoText) infoText.text = $"작물을 수확했습니다! +{rewardMoney}원 (F)";

            // 맵에서 지우고 오브젝트 파괴 (바닥은 원래 타일 유지)
            plantedCrops.Remove(foundCell);
            Destroy(cropObj);

            UpdateUI();
        }
        else
        {
            if (infoText) infoText.text = "주변에 다 자란 작물이 없거나 너무 멉니다!";
        }
    }

    // 씨앗 상점 연동 함수
    public void BuySeedButton()
    {
        if (isGameOver) return;

        if (money >= 50)
        {
            money -= 50;
            seedCount++;
            if (infoText) infoText.text = "씨앗 구매 완료! (밭 위에서 E를 누르세요)";
        }
        else
        {
            if (infoText) infoText.text = "돈이 부족합니다!";
        }
        UpdateUI();
    }

    // 💡 [세금 납부 완료 시 루프] 시간 +1분 추가 및 목표 상향 연산
    public void ClickPayTaxButton()
    {
        if (isGameOver) return;

        if (money >= taxGoal)
        {
            money -= taxGoal; // 보유금 차감

            currentDay++;                             // 일수 증가
            timeLeft += 60f;                          // 💡 현실 시간 1분 추가
            taxGoal = Mathf.CeilToInt(taxGoal * 1.3f); // 💡 세금 목표치 30% 증가

            if (infoText) infoText.text = $"🎉 세금 납부 성공! [Day {currentDay}]가 시작되었습니다. (시간 +1분, 목표 금액 증가)";
        }
        UpdateUI();
    }

    // UI 정보 동기화 (보유금과 세금 목표 분리 표기 반영)
    void UpdateUI()
    {
        if (moneyText) moneyText.text = $"보유 자산: {money}원";
        if (taxGoalText) taxGoalText.text = $"목표 세금: {taxGoal}원 [Day {currentDay}]";

        if (seedText) seedText.text = $"보유 씨앗: {seedCount}개";
        if (timerText) timerText.text = $"남은 시간: {Mathf.CeilToInt(timeLeft)}초";

        // 세금 충족 여부 실시간 버튼 상태 스위칭
        if (payTaxButton != null)
        {
            if (money >= taxGoal) payTaxButton.interactable = true;
            else payTaxButton.interactable = false;
        }
    }

    // 시간 마감 단계 검사
    void CheckTimeOver()
    {
        if (money < taxGoal)
        {
            isGameOver = true;
            // 💡 금액 납부 실패 시 바로 알림 텍스트 출력!
            if (infoText) infoText.text = "🚨 세금을 내지 못해 파산했습니다... 게임 오버!";

            // 💡 약 2초 가량 딜레이를 주는 코루틴 작동
            StartCoroutine(GameOverDelayRoutine());
        }
        else
        {
            if (infoText) infoText.text = "시간이 종료되었습니다! 어서 '세금 내기' 버튼을 누르세요!";
        }
    }

    // 💡 2초 시간차를 두고 현재 씬을 완전 초기화 로드하는 루틴
    IEnumerator GameOverDelayRoutine()
    {
        // 타임스케일에 방해받지 않는 실시간 2초 정지
        yield return new WaitForSecondsRealtime(2f);

        int legacy = money / 2;
        PlayerPrefs.SetInt("LegacyBonus", legacy);
        PlayerPrefs.Save();

        // 유니티 현재 액티브 씬명으로 다시 재시작 로드
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    // ==========================================
    // 💾 [JSON 기반 데이터 세이브 / 로드 세부 구현]
    // ==========================================

    public void SaveGameData()
    {
        SaveData data = new SaveData();
        data.money = this.money;
        data.seedCount = this.seedCount;
        data.timeLeft = this.timeLeft;
        data.taxGoal = this.taxGoal;
        data.currentDay = this.currentDay;

        foreach (KeyValuePair<Vector3Int, GameObject> pair in plantedCrops)
        {
            if (pair.Value != null)
            {
                Crop cropScript = pair.Value.GetComponent<Crop>();
                if (cropScript != null)
                {
                    CropSaveData cropData = new CropSaveData();
                    cropData.cellX = pair.Key.x;
                    cropData.cellY = pair.Key.y;
                    cropData.cropIndex = cropScript.cropPrefabIndex;
                    cropData.isGrown = cropScript.IsGrown();

                    data.plantedCropsList.Add(cropData); // 대소문자 오타 수정완료 (.Add)
                }
            }
        }

        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.persistentDataPath, "FarmingSave.json");
        File.WriteAllText(path, json);

        if (infoText) infoText.text = "💾 게임 데이터가 무사히 내부 저장소에 세이브되었습니다!";
    }

    public void LoadGameData()
    {
        string path = Path.Combine(Application.persistentDataPath, "FarmingSave.json");

        if (!File.Exists(path))
        {
            if (infoText) infoText.text = "불러올 이전 저장 파일이 발견되지 않았습니다.";
            return;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        this.money = data.money;
        this.seedCount = data.seedCount;
        this.timeLeft = data.timeLeft;
        this.taxGoal = data.taxGoal;
        this.currentDay = data.currentDay;

        // 월드상 깔려있던 작물들 잔여 오브젝트 소거
        foreach (GameObject cropObj in plantedCrops.Values)
        {
            if (cropObj != null) Destroy(cropObj);
        }
        plantedCrops.Clear();

        // 파싱된 데이터 기반 작물 복구 재소환
        foreach (CropSaveData cropData in data.plantedCropsList)
        {
            Vector3Int cellPos = new Vector3Int(cropData.cellX, cropData.cellY, 0);
            Vector3 spawnPos = tilemap.GetCellCenterWorld(cellPos);
            spawnPos.z = 0f;

            if (cropData.cropIndex >= 0 && cropData.cropIndex < cropPrefabs.Count)
            {
                GameObject spawnedCrop = Instantiate(cropPrefabs[cropData.cropIndex], spawnPos, Quaternion.identity);
                plantedCrops.Add(cellPos, spawnedCrop);

                Crop cropScript = spawnedCrop.GetComponent<Crop>();
                if (cropScript != null)
                {
                    cropScript.Initialize(cropData.cropIndex);

                    // 만약 세이브 당시 다 성장한 작물이었다면, 즉시 강제 개화 처리
                    if (cropData.isGrown)
                    {
                        cropScript.StopAllCoroutines();
                        cropScript.CompleteGrowth(); // 작물 즉시 성장 함수 호출
                    }
                }
            }
        }

        if (infoText) infoText.text = "📂 마지막으로 세이브했던 일자 데이터가 로드되었습니다!";
        UpdateUI();
    }
}