using System.Collections;
using UnityEngine;

public class Crop : MonoBehaviour
{
    [Header("== 작물 개별 설정 ==")]
    public float growTime = 3f;   // 작물이 자라는 데 걸리는 시간 (초)
    public int sellPrice = 150;   // 이 작물을 수확했을 때 플레이어가 벌어들일 돈

    [Header("== 스프라이트 이미지 == ")]
    public Sprite seededSprite;   // 심어졌을 때의 씨앗/새싹 이미지
    public Sprite grownSprite;    // 다 자랐을 때의 작물(당근 등) 이미지

    [HideInInspector]
    public int cropPrefabIndex;   // 매니저 리스트의 몇 번째 작물인지 저장하는 변수 (세이브/로드용)

    private SpriteRenderer spriteRenderer;
    private bool isGrown = false; // 현재 다 자란 상태인지 체크하는 플래그

    // 💡 FarmingTycoon에서 씨앗을 심을 때 호출하여 초기화하는 함수
    public void Initialize(int index)
    {
        this.cropPrefabIndex = index; // 나의 작물 종류 고유 번호 저장

        // SpriteRenderer 컴포넌트 안전하게 가져오기
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 게임 시작 시 처음에는 씨앗 이미지로 세팅
        if (seededSprite != null)
        {
            spriteRenderer.sprite = seededSprite;
        }

        // 2D 레이어 정렬 설정 (바닥 타일맵보다 무조건 앞에 그리도록)
        spriteRenderer.sortingLayerName = "Background";
        spriteRenderer.sortingOrder = 1;

        // 자라나기 스케줄(코루틴) 시작
        StartCoroutine(GrowRoutine());
    }

    // 💡 시간 초를 세며 자라나는 코루틴 루틴
    IEnumerator GrowRoutine()
    {
        yield return new WaitForSeconds(growTime);

        // 지정된 성장 시간이 지나면 완전히 자란 상태로 변신!
        CompleteGrowth();
    }

    // 💡 성장을 즉시 완료시키는 함수 (시간이 다 되거나, 세이브 데이터를 불러왔을 때 사용)
    public void CompleteGrowth()
    {
        isGrown = true;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && grownSprite != null)
        {
            spriteRenderer.sprite = grownSprite;
        }

        Debug.Log($"{gameObject.name} 작물이 다 자랐습니다! F키를 눌러 수확하세요.");
    }

    // 💡 FarmingTycoon 매니저가 이 작물을 수확할 수 있는지 확인할 때 쓰는 함수
    public bool IsGrown()
    {
        return isGrown;
    }
}