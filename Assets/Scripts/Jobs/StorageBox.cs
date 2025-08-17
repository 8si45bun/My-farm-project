using System.Collections.Generic;
using UnityEngine;

public class StorageBox : MonoBehaviour
{
    public static readonly HashSet<StorageBox> All = new();

    public int floorId = 0;
    // 초기 설계: 무제한 수용, 타입 필터 없음
    private int storedCount = 0;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    public bool Store(ItemType type, int amount)
    {
        storedCount += amount;
        // 시각화/사운드 등은 추후
        return true;
    }
}
