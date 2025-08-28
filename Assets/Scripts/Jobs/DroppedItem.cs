using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public static readonly HashSet<DroppedItem> All = new();

    public ItemType itemType = ItemType.Corn;
    public int amount = 1;
    public int floorId = 0;
    [HideInInspector] public bool isReserved = false;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    public void Pickup()
    {
        Destroy(gameObject);
    }
}
