using System.Collections.Generic;
using UnityEngine;

public class CraftingStationsOverviewPanel : MonoBehaviour
{
    public static CraftingStationsOverviewPanel Instance;

    [Header("UI")]
    public GameObject panel;
    public Transform contentRoot;
    public CraftingStationOverviewEntry entryPrefab;

    private readonly List<CraftingStationOverviewEntry> entries = new();

    public bool IsVisible => panel != null && panel.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void Show()
    {
        if (panel == null) return;
        panel.SetActive(true);
        Rebuild();
    }

    public void Hide()
    {
        if (panel == null) return;
        panel.SetActive(false);
    }

    public void Toggle()
    {
        if (panel == null) return;

        bool active = !panel.activeSelf;
        panel.SetActive(active);

        if (active)
            Rebuild();
    }

    public void Rebuild()
    {
        if (contentRoot == null || entryPrefab == null) return;

        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }
        entries.Clear();

        var stations = CraftingStation.All;

        foreach (var station in stations)
        {
            if (station == null) continue;

            var entry = Instantiate(entryPrefab, contentRoot);
            entry.gameObject.SetActive(true);
            entry.Bind(station);
            entries.Add(entry);
        }
    }
}
