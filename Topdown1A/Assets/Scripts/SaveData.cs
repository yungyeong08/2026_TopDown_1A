using System.Collections.Generic;
using UnityEngine;

// =======================================================
// 📝 [추가] 성적표 한 장 한 장의 명세서 규격 (회차별 기록용)
// =======================================================
[System.Serializable]
public class GameRecord
{
    public int playNumber;     // 몇 번째 판인지 (예: 1회차, 2회차...)
    public int survivedDays;   // 이번 회차에 최종 버틴 일수
    public int totalMoney;     // 이번 회차에 최종 보유했던 코인
}

[System.Serializable]
public class CropSaveData
{
    public int cellX;       // 작물이 심어진 타일맵 X 좌표
    public int cellY;       // 작물이 심어진 타일맵 Y 좌표
    public int cropIndex;   // 어떤 작물인지 (0: 당근, 1: 토마토 등)
    public bool isGrown;    // 다 자란 상태인지 여부
}

[System.Serializable]
public class SaveData
{
    // =======================================================
    // 📊 [핵심 변경] 역대 모든 농사 성적표 기록들이 누적되어 쌓이는 리스트
    // =======================================================
    [Header("== 역대 농사 성적 기록 리스트 (누적) ==")]
    public List<GameRecord> historyRecords = new List<GameRecord>(); // 📋 데이터가 지워지지 않고 밑으로 계속 쌓입니다.

    [Header("== 인게임 실시간 데이터 ==")]
    public int deathCount;
    public int money = 100;
    public float timeLeft = 60f;
    public int taxGoal = 500;
    public int currentDay = 1;

    [Header("== 플레이어 위치 데이터 ==")]
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;

    [Header("== 씨앗 보유량 데이터 ==")]
    // 인덱스 [0]: 기본 씨앗, [1]: 새 씨앗A, [2]: 새 씨앗B, [3]: 새 씨앗C
    public int[] seedCounts = new int[4] { 3, 0, 0, 0 };

    [Header("== 심어진 작물 리스트 ==")]
    public List<CropSaveData> plantedCropsList = new List<CropSaveData>();
}