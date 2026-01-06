using System.Collections.Generic;
using UnityEngine;

public class CraftingStation : MonoBehaviour
{

    public static readonly HashSet<CraftingStation> All = new();
    public Thing thing;
    private StorageBox storageBox;
    public RecipeData[] recipeData;
    private CreaterProgress createrProgress;

    void OnEnable()
    {
        All.Add(this);

        if (storageBox == null)
            storageBox = StorageBox.FindClosest(transform.position);

        if (createrProgress == null)
            createrProgress = GetComponent<CreaterProgress>();
        JobDispatcher.OnJobCompleted += HandleJobCompleted;
    }

    private void OnDisable()
    {
        All.Remove(this);
        JobDispatcher.OnJobCompleted -= HandleJobCompleted;
    }

    StorageBox ResolveStorage()
    {
        if (storageBox == null || !storageBox.isActiveAndEnabled)
            storageBox = StorageBox.FindClosest(transform.position);
        return storageBox;
    }

    public bool canCreaft(RecipeData r)
    {
        foreach (var c in r.costs)
        {
            if (storageBox.GetCount(c.type) < c.count) return false;
        }
        return true;
    }

    public void EnqueueCraft(RecipeData r)
    {
        var box = ResolveStorage();
        if (box == null) { TextManager.ShowDebug("저장고가 존재하지 않습니다."); return; }

        if (!canCreaft(r)) { TextManager.ShowDebug("미내랄이 부족합니다"); return; }

        foreach (var c in r.costs) { storageBox.TakeItem(c.type, c.count); }

        JobDispatcher.Enqueue(new Job
        {
            type = CommandType.Craft,
            cell = Vector3Int.RoundToInt(transform.position),
            targetThing = thing,
            recipeData = r,
            recipeMinute = r.recipeMinutes
        });
    }
    private void HandleJobCompleted(Job job, bool success)
    {
        if (!success) return;
        if (job == null) return;
        if (job.type != CommandType.Craft) return;
        if (job.targetThing != this.thing) return;
        createrProgress.StopProgress();
    }
}
