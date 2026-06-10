using UnityEngine;

[CreateAssetMenu(menuName = "GameData/GameSettingData")]
public class GameSettingData : ScriptableObject
{
    public int startHp = 100;
    public int startAttack = 10;
    public float playerMoveSpeed = 5f;

    public int hpBonusPerDeath = 5;
    public int atkBonusPerDeath = 1;
}
