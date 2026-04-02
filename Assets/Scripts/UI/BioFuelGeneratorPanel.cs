using TMPro;
using UnityEngine;

public class BioFuelGeneratorPanel : MonoBehaviour
{
    public static BioFuelGeneratorPanel Instance;

    [Header("UI")]
    public GameObject panelRoot;          // �г� ��ü
    public TextMeshProUGUI countText;    
    public TextMeshProUGUI statusText;    

    BioFuelGenerator cur;                 // ���� ���õ� ������

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

        // 전력 범위 시각화
        if (PowerRangeVisualizer.Instance != null)
            PowerRangeVisualizer.Instance.ShowNetwork(generator);

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

        // 전력 범위 시각화 숨기기
        if (PowerRangeVisualizer.Instance != null)
            PowerRangeVisualizer.Instance.HideAll();
    }

    public void Refresh()
    {
        if (cur == null) return;

        if (countText != null)
            countText.text = $"Input : {cur.DesiredInput}";

        if (statusText != null)
            statusText.text = $"{cur.StoredFuel} / {cur.DesiredInput}";
    }

    // ----- ��ư�� �Լ� -----

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
