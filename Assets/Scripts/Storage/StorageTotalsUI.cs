using UnityEngine;
using TMPro;

public class StorageTotalsUI : MonoBehaviour
{
    [Header("각 Row의 수량 텍스트 참조")]
    public TextMeshProUGUI cornCountText;
    public TextMeshProUGUI steelCountText;
    public TextMeshProUGUI buildCountText;

    private void OnEnable()
    {
        // 최초 1회 갱신
        if (cornCountText != null || steelCountText != null || buildCountText != null)
        {
            RefreshCounts();
        }

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
        int buildTotal = 0;

        foreach (StorageBox box in StorageBox.All)
        {
            if (box == null)
            {
                continue;
            }

            cornTotal = cornTotal + box.GetCount(ItemType.Corn);
            steelTotal = steelTotal + box.GetCount(ItemType.Steel);
            buildTotal = buildTotal + box.GetCount(ItemType.Build);
        }

        if (cornCountText != null)
        {
            cornCountText.text = cornTotal.ToString();
        }

        if (steelCountText != null)
        {
            steelCountText.text = steelTotal.ToString();
        }

        if (buildCountText != null)
        {
            buildCountText.text = buildTotal.ToString();
        }
    }
}
