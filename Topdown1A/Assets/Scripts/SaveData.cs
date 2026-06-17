using System.Collections.Generic;

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
    // == 기존 시스템 복구 (에러 해결용) ==
    public int deathCount;  // 💡 스크린샷 에러를 잡기 위해 변수를 다시 추가했습니다!

    // == 타이쿤 게임용 데이터 저장소 ==
    public int money = 100;
    public int seedCount = 3;
    public float timeLeft = 60f;
    public int taxGoal = 500;
    public int currentDay = 1;

    // 맵에 심어진 작물들의 데이터 리스트
    public List<CropSaveData> plantedCropsList = new List<CropSaveData>();
}