using System.Collections.Generic;
using UnityEngine;

public static class Reservations
{
    // 타일, 아이템, 스토리지에 대한 단순 예약 테이블
    private static readonly Dictionary<Vector3Int, string> cellResv = new();
    private static readonly HashSet<DroppedItem> itemResv = new();
    private static readonly HashSet<StorageBox> storageResv = new();

    public static bool TryReserveCell(Vector3Int cell, string tag)
    {
        if (cellResv.ContainsKey(cell)) return false;
        cellResv[cell] = tag;
        return true;
    }
    public static void ReleaseCell(Vector3Int cell, string tag)
    {
        if (cellResv.TryGetValue(cell, out var t) && t == tag) cellResv.Remove(cell);
    }

    public static bool TryReserveItem(DroppedItem item)
    {
        if (item == null) return false;
        if (itemResv.Contains(item)) return false;
        itemResv.Add(item);
        item.isReserved = true;
        return true;
    }
    public static void ReleaseItem(DroppedItem item)
    {
        if (item == null) return;
        if (itemResv.Remove(item)) item.isReserved = false;
    }

    public static bool TryReserveStorage(StorageBox box)
    {
        if (box == null) return false;
        if (storageResv.Contains(box)) return false;
        storageResv.Add(box);
        return true;
    }
    public static void ReleaseStorage(StorageBox box)
    {
        if (box == null) return;
        storageResv.Remove(box);
    }
}
