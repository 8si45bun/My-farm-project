using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class BioFuelGenerator : MonoBehaviour, IPowerSource
{
    [Header("World UI")]
    public ProgressBar worldProgressBar;

    [Header("����")]
    private ItemType fuelType = ItemType.Corn;
    public float secondsPerFuel = 20f;      // ������ 1�� ó�� �ð�
    public int powerPerFuel = 10;          // 1���� ����

    public int DesiredInput { get; private set; }  // �гο��� �����ϴ� ��ǥ ����
    public int StoredFuel { get; private set; }  // �̹� �����⿡ ���� �ִ� ����

    public float suuplyRaduis = 3f; // ������ ���� ���� ����

    public float powerOutput = 30f;   // 가동 중 제공하는 전력량
    private bool isRunning = false;

    bool autoRun = true;
    bool processing = false;
    float progress01 = 0f;
    int pendingHaul = 0;

    public event Action OnStateChanged;
    void NotifyChanged() => OnStateChanged?.Invoke();

    private void Start()
    {
        if (PowerManager.Instance != null)
        {
            PowerManager.Instance.RegisterSource(this);
        }
    }

    private void OnDestroy()
    {
        SetRunning(false);
        if (PowerManager.Instance != null)
        {
            PowerManager.Instance.UnRegisterSource(this);
        }
    }

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

    public Vector3 GetPosition () => transform.position;
    public float GetRadius () => suuplyRaduis;

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

    void SetRunning(bool running)
    {
        if (isRunning == running) return;
        isRunning = running;
        if (PowerManager.Instance != null)
        {
            if (running) PowerManager.Instance.AddCapacity(powerOutput);
            else         PowerManager.Instance.RemoveCapacity(powerOutput);
        }
    }

    // �κ��� ������ �����ͼ� ���� ��
    public void OnFuelDelivered(int amount)
    {
        StoredFuel += amount;
        pendingHaul = Mathf.Max(0, pendingHaul - amount);

        NotifyChanged();
        EnsureFuelJobs();
        TryStartProcess();
    } 

    // ---- ���� ó�� ----

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
        SetRunning(true);

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

        progress01 = 0f;
        processing = false;
        SetRunning(false);

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
        // ��ǥ - (�̹� ������ �� + �̹� �� ���� �ִ� pending) ��ŭ�� â������ �� ��������� ��û
        int need = DesiredInput - (StoredFuel + pendingHaul);
        if (need <= 0) return;

        // ���� ����� â�� ã�� 
        var storage = StorageBox.FindClosest(transform.position);
        if (storage == null) return;

        // â���� ������ �ִ� ����
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
