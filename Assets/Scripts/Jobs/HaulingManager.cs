using System.Linq;
using UnityEngine;

public static class HaulingManager
{
    // 수동 작업이 없고 Idle일 때만 호출됨
    public static Job TryMakeHaulJob()
    {
        var items = DroppedItem.All.Where(i => i != null && i.gameObject.activeInHierarchy && !i.isReserved).ToList();
        if (items.Count == 0) return null;

        var boxes = StorageBox.All.Where(b => b != null && b.gameObject.activeInHierarchy).ToList();
        if (boxes.Count == 0) return null;

        // 간단: 가장 가까운 조합 하나
        DroppedItem bestItem = null; StorageBox bestBox = null; float best = float.MaxValue;

        foreach (var it in items)
        {
            foreach (var box in boxes)
            {
                if (box.floorId != it.floorId) continue; // 초기는 같은 층만
                float d = Vector3.SqrMagnitude(it.transform.position - box.transform.position);
                if (d < best)
                {
                    best = d; bestItem = it; bestBox = box;
                }
            }
        }

        if (bestItem == null || bestBox == null) return null;

        return new Job
        {
            type = CommandType.Haul,
            floorId = bestItem.floorId,
            fromItem = bestItem,
            toStorage = bestBox,
            cell = Vector3Int.RoundToInt(bestItem.transform.position)
        };
    }
}
