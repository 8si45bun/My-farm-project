using System;
using System.Collections.Generic;
using UnityEngine;

public class StorageBox : MonoBehaviour
{
    [Serializable]
    public class ItemSlot
    {
        public ItemType type;
        public GameObject obj;
    }

    public static readonly HashSet<StorageBox> All = new();
    public static event Action OnAnyStorageChanged;

    public List<ItemSlot> slots = new();

    private readonly Dictionary<ItemType, int> counts = new();
    public readonly Dictionary<GameObject, int> haveValue = new();

    private void Start()
    {
        UIUpdate();
    }

    private void OnEnable() { All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    public bool Store(ItemType type, int amount)
    {
        if(amount <= 0) return false;

        if (!counts.TryGetValue(type, out var cur))
            counts[type] = amount;
        else
            counts[type] = cur + amount;

        UIUpdate();
        OnAnyStorageChanged?.Invoke();
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

    private void UIUpdate()
    {
        foreach(var slot in slots)
        {
            if (slot.obj == null) continue;

            counts.TryGetValue(slot.type, out int count);
            haveValue[slot.obj] = count;

            if(count > 0)
            {
                slot.obj.SetActive(true);
            }
            else
            {
                slot.obj.SetActive(false);
            }
        }
    }

    public static StorageBox FindClosest(Vector3 worldPos)
    {
        StorageBox closest = null;
        float bestDist = float.MaxValue;

        foreach (var s in All)
        {
            if (s == null) continue;
            float d = Vector3.Distance(worldPos, s.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                closest = s;
            }
        }

        return closest;
    }
}
