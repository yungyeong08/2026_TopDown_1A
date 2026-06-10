using UnityEngine;
using System.Collections;

public class Crop : MonoBehaviour
{
    public enum GrowthState { Empty, Seed, Sprout, Mature }
    public GrowthState currentState;

    public Sprite[] growthSprites; // 0: 빈 밭, 1: 씨앗, 2: 새싹, 3: 다 자란 작물
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateCropState(GrowthState.Empty);
    }

    // 씨앗 심기 및 단계별 성장 함수
    public void UpdateCropState(GrowthState newState)
    {
        currentState = newState;
        spriteRenderer.sprite = growthSprites[(int)currentState];

        if (currentState == GrowthState.Seed)
        {
            StartCoroutine(GrowRoutine());
        }
    }

    // 일정 시간 후 다음 단계로 성장시키는 코루틴
    IEnumerator GrowRoutine()
    {
        yield return new WaitForSeconds(5f); // 5초 후 새싹으로 변경
        UpdateCropState(GrowthState.Sprout);

        yield return new WaitForSeconds(10f); // 10초 후 다 자란 작물로 변경
        UpdateCropState(GrowthState.Mature);
    }
}
