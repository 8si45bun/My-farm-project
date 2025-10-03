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
    public int floorId = 0;
    // 초기 설계: 무제한 수용
    // 아이템 타입과 게임 오브젝트 리스트 딕셔너리, 아이템 타입과 수량 딕셔너리
    public List<ItemSlot> slots = new();
    // 아이템 타입별 개수
    private readonly Dictionary<ItemType, int> counts = new();
    // 오브젝트별 개수
    public readonly Dictionary<GameObject, int> haveValue = new(); // 여기에 오브젝트 리스트랑 counts값 넣음

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
}
