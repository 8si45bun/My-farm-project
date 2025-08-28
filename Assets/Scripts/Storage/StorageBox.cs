using System;
using System.Collections.Generic;
using UnityEngine;

public class StorageBox : MonoBehaviour
{
    public static readonly HashSet<StorageBox> All = new();
    public static event Action OnAnyStorageChanged;
    public int floorId = 0;
    // 초기 설계: 무제한 수용

    private readonly Dictionary<ItemType, int> counts = new Dictionary<ItemType, int>();

    private void OnEnable() { All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    public bool Store(ItemType type, int amount)
    {
        if(amount <= 0) return false;

        if (!counts.TryGetValue(type, out var cur))
            counts[type] = amount;
        else
            counts[type] = cur + amount;

        if (OnAnyStorageChanged != null)
            OnAnyStorageChanged.Invoke();

        return true;
    }

    public int GetCount(ItemType type)
    {
        int value;
        bool found = counts.TryGetValue(type, out value);

        if(found)
            return value;
        else
            return 0;
    }

    public int GetTotalCount()
    {
        int sum = 0;
        foreach (var x in counts) sum += x.Value;
        return sum;
    }
}
