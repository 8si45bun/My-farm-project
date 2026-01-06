using TMPro;
using UnityEngine;

public class BioFuelGeneratorPanel : MonoBehaviour
{
    public static BioFuelGeneratorPanel Instance;

    [Header("UI")]
    public GameObject panelRoot;          // 패널 전체
    public TextMeshProUGUI countText;    
    public TextMeshProUGUI statusText;    

    BioFuelGenerator cur;                 // 현재 선택된 발전기

    private void Awake()
    {
        Instance = this;
        if (panelRoot == null)
            panelRoot = gameObject;

        panelRoot.SetActive(false);
    }

    private void OnDisable()
    {
        if (cur != null)
        {
            cur.OnStateChanged -= Refresh;
            cur = null;
        }
    }

    public void Show(BioFuelGenerator generator)
    {
        if (cur != null)
            cur.OnStateChanged -= Refresh;

        cur = generator;

        if (cur != null)
            cur.OnStateChanged += Refresh;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        Refresh();
    }

    public void Hide()
    {
        if (cur != null)
        {
            cur.OnStateChanged -= Refresh;
            cur = null;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Refresh()
    {
        if (cur == null) return;

        if (countText != null)
            countText.text = $"Input : {cur.DesiredInput}";

        if (statusText != null)
            statusText.text = $"{cur.StoredFuel} / {cur.DesiredInput}";
    }

    // ----- 버튼용 함수 -----

    public void OnPlusBtn()
    {
        if (cur == null) return;
        cur.ChangeDesiredInput(+1);
    }

    public void OnMinusBtn()
    {
        if (cur == null) return;
        cur.ChangeDesiredInput(-1);
    }

    public void OnPlayBtn()
    {
        if (cur == null) return;
        cur.SetAutoRun(true);
    }

    public void OnPauseBtn()
    {
        if (cur == null) return;
        cur.SetAutoRun(false);
    }
}
