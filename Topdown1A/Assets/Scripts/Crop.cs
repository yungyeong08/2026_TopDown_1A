using System.Collections;
using UnityEngine;

public class Crop : MonoBehaviour
{
    [Header("== 작물 개별 설정 ==")]
    public float growTime = 3f;   // 작물이 자라는 데 걸리는 시간 (초)
    public int sellPrice = 150;   // 이 작물을 수확했을 때 플레이어가 벌어들일 돈

    [Header("== 스프라이트 이미지 == ")]
    public Sprite seededSprite;   // 심어졌을 때의 씨앗/새싹 이미지
    public Sprite grownSprite;    // 다 자란 상태의 이미지

    [HideInInspector]
    public int cropPrefabIndex;   // 매니저 리스트의 몇 번째 작물인지 저장하는 변수 (세이브/로드용)

    private SpriteRenderer spriteRenderer;
    private bool isGrown = false; // 현재 다 자란 상태인지 체크하는 플래그

    // FarmingTycoon에서 씨앗을 심을 때 호출하여 초기화하는 함수
    public void Initialize(int index)
    {
        this.cropPrefabIndex = index;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (seededSprite != null)
        {
            spriteRenderer.sprite = seededSprite;
        }

        // 💡 [에러 해결!] orderInLayer 오타를 sortingOrder로 수정했습니다.
        spriteRenderer.sortingLayerName = "Background";
        spriteRenderer.sortingOrder = 1;

        StartCoroutine(GrowRoutine());
    }

    IEnumerator GrowRoutine()
    {
        yield return new WaitForSeconds(growTime);
        CompleteGrowth();
    }

    public void CompleteGrowth()
    {
        isGrown = true;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && grownSprite != null)
        {
            spriteRenderer.sprite = grownSprite;
        }
    }

    public bool IsGrown()
    {
        return isGrown;
    }
}