using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingStationOverviewEntry : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI nameText;
    public ProgressBar progressBar;
    public List<Image> queueSlot;

    private CraftingStation station;
    private CreaterProgress progress;

    public void Bind(CraftingStation station)
    {
        this.station = station;
        progress = station.GetComponent<CreaterProgress>();

        RefreshName();
        RefreshProgress();
        RefreshQueue();
    }

    private void Update()
    {
        RefreshProgress();
        RefreshQueue();
    }

    private void RefreshName()
    {
        string label = station.thing != null && !string.IsNullOrEmpty(station.thing.thingId) 
            ? station.thing.thingId
            : station.name;

        nameText.text = label;
    }

    private void RefreshProgress()
    {
        float p = 0f;
        if (progress != null && progress.IsActive)
            p = progress.current01;

        progressBar.SetProgressBar(p);
    }

    private void RefreshQueue()
    {
        if (queueSlot == null || queueSlot.Count == 0) return;
        if (station == null || station.thing == null) return;

        for (int i = 0; i < queueSlot.Count; i++)
        {
            var img = queueSlot[i];
            if (img == null) continue;
            img.sprite = null;
            img.enabled = false;
        }

        int index = 0;
        var activeJob = JobDispatcher.GetActiveCraftJob(station.thing);
        if (activeJob != null && activeJob.recipeData != null && queueSlot.Count > 0)
        {
            var img = queueSlot[0];
            img.sprite = activeJob.recipeData.icon;
            img.enabled = true;
            index = 1;
        }

        var jobs = JobDispatcher.GetQueueCreaterJob(station.thing);

        for (int i = 0; i < jobs.Count && index + i < queueSlot.Count; i++)
        {
            var job = jobs[i];
            if (job == null || job.recipeData == null) continue;

            var img = queueSlot[index + i];
            if (img == null) continue;

            img.sprite = job.recipeData.icon;
            img.enabled = true;
        }
    }

}
