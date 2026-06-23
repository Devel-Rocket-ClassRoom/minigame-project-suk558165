using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임에 존재하는 모든 도전과제(AchievementData)의 목록.
/// ScriptableObject로 만들어서 AchievementManager에 연결한다.
///
/// 사용법:
///  1) Create > Game > AchievementDatabase 로 .asset 생성
///  2) Inspector에서 achievements 리스트에 도전과제 .asset들 드래그
///  3) AchievementManager의 Database 슬롯에 연결
/// </summary>
[CreateAssetMenu(menuName = "Game/AchievementDatabase", fileName = "AchievementDatabase")]
public class AchievementDatabase : ScriptableObject
{
    [Tooltip("이 게임의 모든 도전과제 목록")]
    public List<AchievementData> achievements = new List<AchievementData>();

    /// <summary>id로 도전과제를 찾아 반환. 없으면 null.</summary>
    public AchievementData Find(string id)
    {
        foreach (var a in achievements)
            if (a != null && a.id == id)
                return a;
        return null;
    }
}
