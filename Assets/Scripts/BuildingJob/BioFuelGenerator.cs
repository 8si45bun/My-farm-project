using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class BioFuelGenerator : MonoBehaviour
{
    [Header("World UI")]
    public ProgressBar worldProgressBar;

    [Header("설정")]
    private ItemType fuelType = ItemType.Corn;
    public float secondsPerFuel = 20f;      // 옥수수 1개 처리 시간
    public int powerPerFuel = 10;          // 1개당 전력

    public int DesiredInput { get; private set; }  // 패널에서 설정하는 목표 개수
    public int StoredFuel { get; private set; }  // 이미 발전기에 들어와 있는 개수

    bool autoRun = true;   
    bool processing = false;
    float progress01 = 0f;

    int pendingHaul = 0;

    public event Action OnStateChanged;
    void NotifyChanged() => OnStateChanged?.Invoke();

    private void OnEnable()
    {
        StorageBox.OnAnyStorageChanged += HandleStorageChanged;
    }

    private void OnDisable()
    {
        StorageBox.OnAnyStorageChanged -= HandleStorageChanged;
    }

    private void Update()
    {
        if (worldProgressBar != null)
            worldProgressBar.SetProgressBar(progress01);
    }

    public void ChangeDesiredInput(int delta)
    {
        DesiredInput = Mathf.Max(0, DesiredInput + delta);

        NotifyChanged();
        EnsureFuelJobs();
    }

    public void SetAutoRun(bool on)
    {
        autoRun = on;

        NotifyChanged();
        TryStartProcess();
    }

    // 로봇이 옥수수 가져와서 넣을 때
    public void OnFuelDelivered(int amount)
    {
        StoredFuel += amount;
        pendingHaul = Mathf.Max(0, pendingHaul - amount);

        NotifyChanged();
        EnsureFuelJobs();
        TryStartProcess();
    } 

    // ---- 내부 처리 ----

    void TryStartProcess()
    {
        if (!autoRun) return;
        if (processing) return;
        if (StoredFuel <= 0) return;

        StartCoroutine(ProcessRoutine());
    }

    IEnumerator ProcessRoutine()
    {
        processing = true;

        StoredFuel = Mathf.Max(0, StoredFuel - 1);
        NotifyChanged();

        progress01 = 0f;
        float t = 0f;
        while (t < secondsPerFuel)
        {
            t += Time.deltaTime;
            progress01 = Mathf.Clamp01(t / secondsPerFuel);
            yield return null;
        }

        PowerManager.Instance.AddPower(powerPerFuel);

        progress01 = 0f;
        processing = false;

        NotifyChanged();
        TryStartProcess();
        EnsureFuelJobs();
    }

    private void HandleStorageChanged()
    {
        EnsureFuelJobs();
    }

    private void EnsureFuelJobs()
    {
        // 목표 - (이미 발전기 안 + 이미 길 위에 있는 pending) 만큼만 창고에서 더 가져오라고 요청
        int need = DesiredInput - (StoredFuel + pendingHaul);
        if (need <= 0) return;

        // 가장 가까운 창고 찾기 
        var storage = StorageBox.FindClosest(transform.position);
        if (storage == null) return;

        // 창고에 실제로 있는 수량
        int available = storage.GetCount(fuelType);
        if (available <= 0) return;

        int toRequest = Mathf.Min(need, available);

        for (int i = 0; i < toRequest; i++)
        {
            var job = new Job
            {
                type = CommandType.Haul,
                status = JobStatus.Queued,
                fromStorage = storage,
                toGenerator = this,
                haulItem = fuelType,
                haulCount = 1
            };

            JobDispatcher.Enqueue(job);
            pendingHaul++;
        }
    }
}
