using UnityEngine;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;

public class StorageTotalsUI : MonoBehaviour
{
    [Header("각 Row의 수량 텍스트 참조")]
    public TextMeshProUGUI cornCountText;
    public TextMeshProUGUI steelCountText;
    public TextMeshProUGUI MinerCountText;
    public TextMeshProUGUI CornSeedCountText;


    private void OnEnable()
    {
        // 저장 변화 알림 구독
        StorageBox.OnAnyStorageChanged += HandleStorageChanged;
    }

    private void OnDisable()
    {
        // 구독 해제
        StorageBox.OnAnyStorageChanged -= HandleStorageChanged;
    }

    private void HandleStorageChanged()
    {
        RefreshCounts();
    }

    private void RefreshCounts()
    {
        int cornTotal = 0;
        int steelTotal = 0;
        int minerTotal = 0;
        int CornSeedTotal = 0;
        foreach (StorageBox box in StorageBox.All)
        {
            cornTotal = cornTotal + box.GetCount(ItemType.Corn);
            steelTotal = steelTotal + box.GetCount(ItemType.Steel);
            minerTotal = minerTotal + box.GetCount(ItemType.Miner);
            CornSeedTotal = CornSeedTotal + box.GetCount(ItemType.CornSeed);
        }

            cornCountText.text = cornTotal.ToString();
            steelCountText.text = steelTotal.ToString();
            MinerCountText.text = minerTotal.ToString();
            CornSeedCountText.text = CornSeedTotal.ToString();
    }
}
