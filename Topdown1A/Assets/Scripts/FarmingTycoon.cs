using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

public class FarmingTycoon : MonoBehaviour
{
    public static FarmingTycoon Instance;

    [Header("== 타일맵 설정 ==")]
    public Tilemap tilemap;
    public TileBase dirtTile;

    [Header("== 작물 프리팹 리스트 (총 4개 등록할 것) ==")]
    public List<GameObject> cropPrefabs = new List<GameObject>();
    private int currentSelectedCropIndex = 0;

    [Header("== 씨앗 가격 설정 (3개 묶음 가격 기준) ==")]
    public List<int> seedPrices = new List<int>() { 120, 300, 600, 1200 };

    [Header("== 플레이어 참조 ==")]
    public Transform playerTransform;

    [Header("== 게임 규칙 & 스테이지 세팅 ==")]
    public int money = 100;

    // 실시간 인게임에서 관리할 4종류의 씨앗 인벤토리 배열
    public int[] seedCounts = new int[4] { 3, 0, 0, 0 };

    public float timeLeft = 60f;
    public int taxGoal = 500;
    public int currentDay = 1;
    private bool isGameOver = false;

    [Header("== UI 연결 ==")]
    public TMP_Text moneyText;
    public TMP_Text taxGoalText;
    public TMP_Text seedText; // 현재 선택된 씨앗과 개수를 보여줄 UI
    public TMP_Text timerText;
    public TMP_Text infoText;

    [Header("== 버튼 UI 연결 ==")]
    public Button payTaxButton;

    private Dictionary<Vector3Int, GameObject> plantedCrops = new Dictionary<Vector3Int, GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (payTaxButton != null) payTaxButton.interactable = false;

        // 💡 메인 화면에서 '불러오기'를 누르고 들어왔는지 가장 먼저 체크합니다.
        if (GameDataManager.Instance != null && GameDataManager.Instance.isLoadTriggered)
        {
            GameDataManager.Instance.isLoadTriggered = false;
            LoadGameData(); // 📂 불러오기를 했을 때는 리셋 로직을 건너뛰고 파일 데이터로 완전히 채웁니다.
        }
        else
        {
            // 🌱 [새 게임으로 시작할 때만] 로그라이크 보너스 및 리셋 로직 작동
            int bonus = PlayerPrefs.GetInt("LegacyBonus", 0);
            money += bonus;
            PlayerPrefs.SetInt("LegacyBonus", 0);

            // 가방의 모든 씨앗 칸 초기화
            for (int i = 0; i < seedCounts.Length; i++)
            {
                seedCounts[i] = 0;
            }

            // 기본 씨앗만 제공
            int seedBonus = PlayerPrefs.GetInt("SeedBonus", 0);
            seedCounts[0] = 3 + seedBonus;

            if (infoText)
            {
                if (seedBonus > 0)
                    infoText.text = $"[Day {currentDay}] 세금을 모으세요! (로그라이크 보너스: 기본 씨앗 +{seedBonus}개 반영됨)";
                else
                    infoText.text = $"[Day {currentDay}] 세금을 모아 살아남으세요!";
            }

            UpdateUI();
        }
    }

    void Update()
    {
        if (isGameOver) return;

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

        if (timeLeft > 0 && playerTransform != null && tilemap != null)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Vector3Int cellPos = tilemap.WorldToCell(playerTransform.position);
                TryPlantSeed(cellPos);
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                TryHarvestNearbyCrop();
            }

            // 숫자키 1, 2, 3, 4로 씨앗 품종 교체 (0번~3번 인덱스)
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchCrop(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchCrop(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchCrop(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchCrop(3);
        }
    }

    public void SwitchCrop(int index)
    {
        if (index >= 0 && index < cropPrefabs.Count)
        {
            currentSelectedCropIndex = index;
            if (infoText) infoText.text = $"심을 작물이 변경되었습니다! (현재 품종: {cropPrefabs[index].name})";
            UpdateUI();
        }
    }

    public void BuySpecificSeed(int cropIndex)
    {
        if (isGameOver) return;
        if (cropIndex < 0 || cropIndex >= seedPrices.Count) return;

        // [보유 제한 검사] 현재 수량에서 3개를 더했을 때 최대치인 10개를 초과하는지 체크
        if (seedCounts[cropIndex] + 3 > 10)
        {
            if (infoText) infoText.text = "🎒 가방 공간이 부족합니다! (씨앗은 종류당 최대 10개까지만 보유 가능)";
            return;
        }

        int price = seedPrices[cropIndex];

        if (money >= price)
        {
            money -= price;
            seedCounts[cropIndex] += 3;

            if (infoText) infoText.text = $"{cropPrefabs[cropIndex].name} 씨앗 3개 구매 완료! (-{price}원)";
        }
        else
        {
            if (infoText) infoText.text = "돈이 부족합니다!";
        }
        UpdateUI();
    }

    void TryPlantSeed(Vector3Int cellPos)
    {
        TileBase currentTile = tilemap.GetTile(cellPos);

        if (currentTile == null || plantedCrops.ContainsKey(cellPos))
        {
            if (infoText) infoText.text = "여기는 씨앗을 심을 수 있는 공간이 아닙니다.";
            return;
        }

        if (seedCounts[currentSelectedCropIndex] <= 0)
        {
            if (infoText) infoText.text = $"{cropPrefabs[currentSelectedCropIndex].name} 씨앗이 부족합니다! 상점에서 구매하세요.";
            return;
        }

        if (cropPrefabs.Count == 0 || cropPrefabs[currentSelectedCropIndex] == null)
        {
            if (infoText) infoText.text = "등록된 작물 프리팹 에셋이 없습니다!";
            return;
        }

        Vector3 spawnPos = tilemap.GetCellCenterWorld(cellPos);
        spawnPos.z = 0f;

        seedCounts[currentSelectedCropIndex]--;

        GameObject spawnedCrop = Instantiate(cropPrefabs[currentSelectedCropIndex], spawnPos, Quaternion.identity);
        plantedCrops.Add(cellPos, spawnedCrop);

        Crop cropScript = spawnedCrop.GetComponent<Crop>();
        if (cropScript != null)
        {
            cropScript.Initialize(currentSelectedCropIndex);
        }

        if (infoText) infoText.text = $"{cropPrefabs[currentSelectedCropIndex].name} 씨앗을 심었습니다! (E)";
        UpdateUI();
    }

    void TryHarvestNearbyCrop()
    {
        Vector3 playerPos = playerTransform.position;
        Vector3Int foundCell = Vector3Int.zero;
        bool cropFound = false;
        int rewardMoney = 150;

        Vector3Int centerCell = tilemap.WorldToCell(playerPos);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkCell = centerCell + new Vector3Int(x, y, 0);

                if (plantedCrops.ContainsKey(checkCell))
                {
                    GameObject cropObj = plantedCrops[checkCell];
                    Crop cropScript = cropObj.GetComponent<Crop>();

                    if (cropScript != null && cropScript.IsGrown())
                    {
                        foundCell = checkCell;
                        cropFound = true;
                        rewardMoney = cropScript.sellPrice;
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

            plantedCrops.Remove(foundCell);
            Destroy(cropObj);

            UpdateUI();
        }
        else
        {
            if (infoText) infoText.text = "주변에 다 자란 작물이 없거나 너무 멉니다!";
        }
    }

    public void ClickPayTaxButton()
    {
        if (isGameOver) return;

        if (money >= taxGoal)
        {
            money -= taxGoal;

            currentDay++;
            timeLeft += 60f;
            taxGoal = Mathf.CeilToInt(taxGoal * 1.3f);

            if (infoText) infoText.text = $"🎉 세금 납부 성공! [Day {currentDay}]가 시작되었습니다. (시간 +1분, 목표 금액 증가)";
        }
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (moneyText) moneyText.text = $"보유 자산: {money}원";
        if (taxGoalText) taxGoalText.text = $"목표 세금: {taxGoal}원 [Day {currentDay}]";

        if (seedText && cropPrefabs.Count > currentSelectedCropIndex && cropPrefabs[currentSelectedCropIndex] != null)
        {
            string currentName = cropPrefabs[currentSelectedCropIndex].name;
            int currentQty = seedCounts[currentSelectedCropIndex];
            seedText.text = $"선택된 씨앗: {currentName} ({currentQty}개 보유)";
        }

        if (timerText) timerText.text = $"남은 시간: {Mathf.CeilToInt(timeLeft)}초";

        if (payTaxButton != null)
        {
            if (money >= taxGoal) payTaxButton.interactable = true;
            else payTaxButton.interactable = false;
        }
    }

    void CheckTimeOver()
    {
        if (money < taxGoal)
        {
            isGameOver = true;
            if (infoText) infoText.text = "🚨 세금을 내지 못해 파산했습니다... 게임 오버!";
            StartCoroutine(GameOverDelayRoutine());
        }
        else
        {
            if (infoText) infoText.text = "시간이 종료되었습니다! 어서 '세금 내기' 버튼을 누르세요!";
        }
    }

    IEnumerator GameOverDelayRoutine()
    {
        yield return new WaitForSecondsRealtime(2f);

        // ==========================================
        // 📊 [기획 구현 1] JSON에 이번 회차 성적표 기록
        // ==========================================
        if (GameDataManager.Instance != null)
        {
            // 1. 새로운 성적표 객체를 한 장 새로 만듭니다.
            GameRecord newRecord = new GameRecord();

            // 2. 성적표 내용을 채웁니다.
            newRecord.playNumber = GameDataManager.Instance.saveData.historyRecords.Count + 1;
            newRecord.survivedDays = this.currentDay; // 버틴 일수
            newRecord.totalMoney = this.money;       // 최종 코인

            // 3. 누적 가방(리스트)에 이 성적표를 넣어줍니다.
            GameDataManager.Instance.saveData.historyRecords.Add(newRecord);

            // 4. 리스트가 포함된 최종 데이터를 JSON으로 저장합니다.
            GameDataManager.Instance.SaveJsonData();
        }

        // ==========================================
        // 🏆 [기획 구현 2] PlayerPrefs에 역대 최고 점수 기록
        // ==========================================
        // 기존에 저장된 역대 최고 일수를 가져옵니다 (없으면 0)
        int bestDay = PlayerPrefs.GetInt("BestSurvivedDay", 0);

        // 이번 기록이 기존 기록보다 높다면 갱신!
        if (this.currentDay > bestDay)
        {
            PlayerPrefs.SetInt("BestSurvivedDay", this.currentDay);
            Debug.Log($"🏆 [최고 기록 갱신!] 이전 기록: {bestDay}일 -> 신기록: {currentDay}일");
        }

        // 기존 로그라이크 보너스 로직 유지
        int legacy = money / 2;
        PlayerPrefs.SetInt("LegacyBonus", legacy);

        int currentSeedBonus = PlayerPrefs.GetInt("SeedBonus", 0);
        PlayerPrefs.SetInt("SeedBonus", currentSeedBonus + 3);

        PlayerPrefs.Save();

        // 파산 시 메인 타이틀 화면으로 복귀
        UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
    }

    // ==========================================
    // 💾 [세이브 / 로드 구역]
    // ==========================================

    public void SaveGameData()
    {
        Debug.Log("🎯 [FarmingTycoon] 저장 버튼이 성공적으로 클릭되었습니다!");

        if (GameDataManager.Instance == null)
        {
            Debug.LogError("❌ [FarmingTycoon] GameDataManager 인스턴스를 찾을 수 없어 저장이 취소되었습니다.");
            if (infoText) infoText.text = "저장 실패: 데이터 매니저가 없습니다.";
            return;
        }

        SaveData data = GameDataManager.Instance.saveData;

        data.money = this.money;
        data.timeLeft = this.timeLeft;
        data.taxGoal = this.taxGoal;
        data.currentDay = this.currentDay;

        if (playerTransform != null)
        {
            data.playerPosX = playerTransform.position.x;
            data.playerPosY = playerTransform.position.y;
            data.playerPosZ = playerTransform.position.z;
        }

        if (this.seedCounts != null && data.seedCounts != null)
        {
            for (int i = 0; i < seedCounts.Length; i++)
            {
                data.seedCounts[i] = this.seedCounts[i];
            }
        }

        data.plantedCropsList.Clear();
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

                    data.plantedCropsList.Add(cropData);
                }
            }
        }

        GameDataManager.Instance.SaveJsonData();

        Debug.Log("💾 [FarmingTycoon] JSON 세이브 파일에 최종 수치가 기록 완료되었습니다!");
        if (infoText) infoText.text = "[저장 완료] 게임 데이터가 안전하게 보존되었습니다!";
    }

    public void LoadGameData()
    {
        if (GameDataManager.Instance == null) return;

        GameDataManager.Instance.LoadjsonData();
        SaveData data = GameDataManager.Instance.saveData;

        this.money = data.money;
        this.timeLeft = data.timeLeft;
        this.taxGoal = data.taxGoal;
        this.currentDay = data.currentDay;

        if (data.seedCounts != null && data.seedCounts.Length == this.seedCounts.Length)
        {
            for (int i = 0; i < seedCounts.Length; i++)
            {
                this.seedCounts[i] = data.seedCounts[i];
            }
        }

        if (playerTransform != null)
        {
            playerTransform.position = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
            Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        foreach (GameObject cropObj in plantedCrops.Values)
        {
            if (cropObj != null) Destroy(cropObj);
        }
        plantedCrops.Clear();

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
                    if (cropData.isGrown)
                    {
                        cropScript.StopAllCoroutines();
                        cropScript.CompleteGrowth();
                    }
                }
            }
        }

        // 💡 불러온 직후 최신 수치를 UI 오브젝트들에 강제 새로고침합니다.
        UpdateUI();
        if (infoText) infoText.text = "[로드 완료] 이어서 시작합니다!";
    }
}