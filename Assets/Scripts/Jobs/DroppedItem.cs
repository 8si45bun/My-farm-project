using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public static readonly HashSet<DroppedItem> All = new();

    public ItemType itemType = ItemType.Generic;
    public int amount = 1;
    public int floorId = 0;
    [HideInInspector] public bool isReserved = false;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    // 실제 인벤토리 시스템이 없다면, 픽업 시 파괴
    public void Pickup()
    {
        Destroy(gameObject);
    }
}
