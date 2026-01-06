using UnityEngine;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;

public class StorageTotalsUI : MonoBehaviour
{
    [Header("각 Row의 수량 텍스트 참조")]
    public TextMeshProUGUI cornCountText;
    public TextMeshProUGUI FirebloomCountText;
    public TextMeshProUGUI StarmossCountText;
    public TextMeshProUGUI WoodCountText;
    public TextMeshProUGUI SteelCountText;
    public TextMeshProUGUI StoneCountText;
    public TextMeshProUGUI PileCountText;


    private void OnEnable()
    {
        StorageBox.OnAnyStorageChanged += HandleStorageChanged;
    }

    private void OnDisable()
    {
        StorageBox.OnAnyStorageChanged -= HandleStorageChanged;
    }

    private void HandleStorageChanged()
    {
        RefreshCounts();
    }

    private void RefreshCounts()
    {
        int cornTotal = 0;
        int firebloomTotal = 0;
        int starmossTotal = 0;
        int woodTotal = 0;
        int steelTotal = 0;
        int stoneTotal = 0;
        int pileTotal = 0;

        foreach (StorageBox box in StorageBox.All)
        {
            cornTotal = cornTotal + box.GetCount(ItemType.Corn);
            firebloomTotal = firebloomTotal + box.GetCount(ItemType.Firebloom);
            starmossTotal = starmossTotal + box.GetCount(ItemType.StarMoss);
            woodTotal = woodTotal + box.GetCount(ItemType.Wood);
            steelTotal = steelTotal + box.GetCount(ItemType.Steel);
            stoneTotal = stoneTotal + box.GetCount(ItemType.Stone);
            pileTotal = pileTotal + box.GetCount(ItemType.Pile);
        }

            cornCountText.text = cornTotal.ToString();
            FirebloomCountText.text = firebloomTotal.ToString();
            StarmossCountText.text = starmossTotal.ToString();
            WoodCountText.text = woodTotal.ToString();
            SteelCountText.text = steelTotal.ToString();
            StoneCountText.text = stoneTotal.ToString();
            PileCountText.text = pileTotal.ToString();


    }
}
