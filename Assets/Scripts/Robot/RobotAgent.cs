using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RobotAgent : MonoBehaviour
{
    [Header("�κ� ���͸�")]
    public float maxBattery = 100f;
    public float currentBattery;
    public float workDrainRate = 1.0f; // �۾� �� �ʴ� �Ҹ�
    public float moveDrainRate = 0.5f; // �̵� �� �ʴ� �Ҹ�
    public float chargeRate = 10f;    // ���� �ӵ�
    public float lowBatteryThreshold = 10f; // ���͸� ���� �Ӱ�ġ
    public float chargedThreshold = 90f; // ���� �Ϸ� �Ӱ�ġ
    public float chargeConsumption = 5f;      // 충전 시 사용하는 전력량
    private bool isChargingFromGrid = false;  // 현재 전력망 예약 중 여부

    [Header("�۾� �޸�")]
    private Job savedJob;
    private Action<Job, bool> savedCallback;

    [Header("�Ͽ�")]
    public bool isCarrying = false;
    public ItemType carriedType;
    public int carryingAmount;

    [Header("��ü ȯ�� ������ ������")]
    public GameObject woodPrefab;
    public GameObject steelPrefab;

    private RobotProgress progress;
    private RobotManager robot;
    private Job currentJob;
    private Action<Job, bool> onComplete;
    public Job CurrentJob => currentJob;
    public RobotState currentState = RobotState.Idle; // �κ� ����

    private void Awake()
    {
        robot = GetComponent<RobotManager>();
        robot.OnTaskCycleCompleted += HandleTaskCompleted;
        progress = GetComponent<RobotProgress>();       
    }

    private void Start()
    {
       currentBattery = maxBattery;
    }

    private void Update()
    {
        HandleBattery();
    }

    private void OnEnable() { JobDispatcher.Register(this); }
    private void OnDisable() { JobDispatcher.UnRegister(this); }

    public bool IsIdle()
    {
        return (currentJob == null && !robot.IsBusy);
    }

    void RegisterGridCharge()
    {
        if (isChargingFromGrid) return;
        if (PowerManager.Instance != null && PowerManager.Instance.TryAddConsumer(chargeConsumption))
            isChargingFromGrid = true;
    }

    void UnregisterGridCharge()
    {
        if (!isChargingFromGrid) return;
        PowerManager.Instance?.RemoveConsumer(chargeConsumption);
        isChargingFromGrid = false;
    }

    private void HandleBattery()
    {
        bool isPowered = false;

        if(PowerManager.Instance != null)
        {
            isPowered = PowerManager.Instance.IsIInPowerRange(robot.transform.position);
        }

        if (isPowered)
        {
            RegisterGridCharge();

            if (isChargingFromGrid)
            {
                // 발전기가 죽어 전력 과부하 시 충전 해제
                if (PowerManager.Instance != null &&
                    PowerManager.Instance.TotalCapacity < PowerManager.Instance.TotalConsumed)
                {
                    UnregisterGridCharge();
                }
                else
                {
                    currentBattery += chargeRate * Time.deltaTime;

                    if (currentState == RobotState.Charging && currentBattery >= chargedThreshold)
                    {
                        Debug.Log("충전 완료 (범위 내)");
                        UnregisterGridCharge();
                        currentState = RobotState.Idle;
                        StartCoroutine(NotifyIdleNextFrame());
                    }
                }
            }
        }
        else
        {
            UnregisterGridCharge();

            float drainRate = 0f;
            if (currentState == RobotState.Working) drainRate = workDrainRate;
            else if (currentState == RobotState.Moving) drainRate = moveDrainRate;

            currentBattery -= drainRate * Time.deltaTime;
        }

        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);

        if (!isPowered && currentBattery <= lowBatteryThreshold && currentState != RobotState.Emergency)
        {
            Debug.Log("배터리 부족 상태로 비상모드로 전환합니다.");

            if (currentJob != null)
            {
                savedJob = currentJob;
                savedCallback = onComplete;
                currentJob = null;
                onComplete = null;
            }

            currentState = RobotState.Emergency;
            robot.GoToChargeStation();
        }

        if (currentState == RobotState.Emergency && isPowered)
        {
            Debug.Log("비상을 해제 후에 충전 중.");
            currentState = RobotState.Charging;
        }
    }


    public bool AcceptJob(Job job, Action<Job, bool> completionCallback)
    {
        if (!IsIdle()) return false;
        currentJob = job;
        onComplete = completionCallback;

        switch (job.type)
        {
            case CommandType.Move:
                robot.MoveTo(job.cell);
                break;
            case CommandType.Dig:
                robot.StartDig(job.cell);
                break;
            case CommandType.Cultivate:
                robot.StartCultivate(job.cell);
                break;
            case CommandType.Plant:
                robot.StartPlant(job.cell, job.plantData);
                break;
            case CommandType.Harvest:
                robot.StartHarvest(job.cell);
                break;
            case CommandType.Haul:           
                StartCoroutine(HaulRoutine(job));
                break;
            case CommandType.Build:
                StartCoroutine(BuildRoutine(job));
                break;
            case CommandType.Craft:
                StartCoroutine(CraftRoutine(job));
                break;
            case CommandType.Mine:
                StartCoroutine(MineRoutine(job));
                break;
            case CommandType.Deconstruct:
                StartCoroutine(DeconstructRoutine(job));
                break;
            default:
                currentJob = null;
                return false;
        }
        job.status = JobStatus.InProgress;
        return true;
    }

    private void GiveDeconstructRefund(Thing thing)
    {
        Vector3 dropPos = thing.transform.position;

        ItemType refundType = ItemType.Wood;
        int refundAmount = 1;

        switch (thing.thingId)
        {
            case "Creater":
                refundType = ItemType.Wood;
                refundAmount = 1;
                break;

            case "Miner":
                refundType = ItemType.Steel;
                refundAmount = 1;
                break;

            case "Generator":
                refundType = ItemType.Wood;
                refundAmount = 1;
                break;
        }

        GameObject prefab = null;
        switch (refundType)
        {
            case ItemType.Wood:
                prefab = woodPrefab;
                break;
            case ItemType.Steel:
                prefab = steelPrefab;
                break;

            default: return;
        }
        if (prefab == null) return;

        Vector3 offset = new Vector3(
        Random.Range(-0.1f, 0.1f),
        Random.Range(-0.1f, 0.1f),
        0f);

        var go = Instantiate(prefab, dropPos + offset, Quaternion.identity);

        var dropped = go.GetComponent<DroppedItem>();
        if (dropped != null)
        {
            dropped.itemType = refundType;
            dropped.amount = refundAmount;
        }
    }

    private IEnumerator DeconstructRoutine(Job job)
    {
        if(job.targetThing == null)
        {
            Finish(false);
            yield break;
        }
        currentState = RobotState.Working;
        var targetCell = Vector3Int.RoundToInt(job.targetThing.transform.position);

        robot.MoveToAdjacent(targetCell);
        while (robot.IsBusy) yield return null;

        int minutes = Mathf.Max(1, job.buildMinutes);
        progress.PlayGameMinutes(minutes);
        yield return TimeManager.WaitGameMinutes(minutes);

        GiveDeconstructRefund(job.targetThing);

        progress.StopHide();

        // �ǹ� ����
        Destroy(job.targetThing.gameObject);

        yield return null;
        Finish(true);
    }

        private IEnumerator CraftRoutine(Job job)
    {
        Debug.Log("���� ����");
        var targetCell = job.cell;
        robot.MoveTo(targetCell);
        while (robot.IsBusy) yield return null;

        int minutes = Mathf.Max(1, job.recipeMinute);
        progress.PlayGameMinutes(minutes);
        var panelProgress = job.targetThing.GetComponent<CreaterProgress>();

        panelProgress.StartProgress(minutes);
        yield return TimeManager.WaitGameMinutes(minutes);

        var station = job.targetThing.GetComponent<CraftingStation>();
        var outCell = Vector3Int.RoundToInt(station.transform.position);
        StorageBox targetStorage = StorageBox.FindClosest(outCell);

        if (targetStorage != null)
        {
            var p = Instantiate(job.recipeData.outputPrefebs, outCell, Quaternion.identity);
            var item = p.GetComponent<DroppedItem>();
            Debug.Log("���� �� ���");
            JobDispatcher.Enqueue(new Job
            {
                type = CommandType.Haul,
                cell = outCell,
                fromItem = item,
                toStorage = targetStorage
            });
        }
        
        progress.StopHide();
        yield return null;
        Finish(true);
    }

    private IEnumerator BuildRoutine(Job job)
    {
        Debug.Log("�Ǽ� ����");
        var targetCell = job.cell;
        robot.MoveToAdjacent(targetCell);
        while (robot.IsBusy) yield return null;

        int minutes = Mathf.Max(1, job.buildMinutes);
        progress.PlayGameMinutes(minutes);
        yield return TimeManager.WaitGameMinutes(minutes);

        job.targetThing.Setstage(BuildStage.Finished);

        progress.StopHide();
        yield return null;
        Finish(true);
    }

    private IEnumerator MineRoutine(Job job)
    {
        var targetCell = job.cell;
        robot.MoveTo(targetCell);
        while (robot.IsBusy) yield return null;

        int minutes = Mathf.Max(1, job.MinerMinute);
        progress.PlayGameMinutes(minutes);
        yield return TimeManager.WaitGameMinutes(minutes);

        GameObject orePrefab = null;
        var station = job.targetThing.GetComponent<MinerStation>();
        orePrefab = station.GetOrePrefab(targetCell);

        Vector3 worldPos = (Vector3)targetCell;
        Vector3 offset = new Vector3(
        Random.Range(-0.1f, 0.1f), -1, 0f);

        var oreObj = Instantiate(orePrefab, worldPos+ offset, Quaternion.identity);

        progress.StopHide();
        yield return null;
        Finish(true);
    }

    private IEnumerator HaulRoutine(Job job)
    {
        // �ݱ� (�ƹ��͵� �� ��� ���� ���� ����)
        if (!isCarrying)
        {
            // CASE A: â�� -> ������ (���� ����)
            if (job.fromStorage != null && job.toGenerator != null)
            {
                var storageCell = Vector3Int.RoundToInt(job.fromStorage.transform.position);
                robot.MoveToAdjacent(storageCell);
                while (robot.IsBusy) yield return null;

                // ���� �� ����
                if (job.fromStorage == null)
                {
                    Finish(false); yield break; 
                }

                int amount = Mathf.Max(1, job.haulCount);
                bool taken = job.fromStorage.TryTake(job.haulItem, amount);

                if (!taken)
                {
                    Finish(false); yield break;
                }
                isCarrying = true;
                carriedType = job.haulItem; // ���������� ������ Ÿ��
                carryingAmount = amount;
            }

            // CASE B: ���� ������ ������ -> â�� (�Ϲ� ���)
            else if (job.fromItem != null)
            {
                // ������ ��ȿ�� �˻�
                if (job.fromItem == null || job.fromItem.gameObject == null)
                {
                    Finish(false); yield break;
                }

                // �̵�
                var itemCell = Vector3Int.RoundToInt(job.fromItem.transform.position);
                robot.MoveToAdjacent(itemCell);
                while (robot.IsBusy) yield return null;

                // �̵� �� �ٽ� Ȯ�� 
                if (job.fromItem == null || job.fromItem.gameObject == null)
                {
                    Finish(false); yield break;
                }

                // �Ⱦ� �� �κ��丮 ������Ʈ
                var it = job.fromItem;
                isCarrying = true;
                carriedType = it.itemType;
                carryingAmount = Mathf.Max(1, it.amount);

                it.Pickup(); // �� ������Ʈ ����
                job.fromItem = null; // ���� ���� (�޸� ���� ����)
            }
            else
            {
                Finish(false); yield break;
            }
        }
        else
        {
            Debug.Log("�̹� ������ �������Դϴ�. �ݱ� �ܰ踦 �ǳʶݴϴ�.");
        }

        // ���� / ��� (������ ��� ���� ���� ����)
        if (isCarrying)
        {
            // CASE A: ������� ���
            if (job.toGenerator != null)
            {
                var genCell = Vector3Int.RoundToInt(job.toGenerator.transform.position);
                robot.MoveToAdjacent(genCell);
                while (robot.IsBusy) yield return null;

                // �����⿡ ���� ����
                if (job.toGenerator != null)
                {
                    job.toGenerator.OnFuelDelivered(carryingAmount);
                }
            }
            // CASE B: â���� ���
            else if (job.toStorage != null)
            {
                var storageCell = Vector3Int.RoundToInt(job.toStorage.transform.position);
                robot.MoveToAdjacent(storageCell);
                while (robot.IsBusy) yield return null;

                // â���� ����
                if (job.toStorage != null)
                {
                    job.toStorage.Store(carriedType, carryingAmount);
                }
            }

            // ��� �Ϸ� �� �κ��丮 ����
            isCarrying = false;
            carryingAmount = 0;
        }

        yield return null;
        Finish(true);
    }

    private bool notifying;
    private IEnumerator NotifyIdleNextFrame()
    {
        if (notifying) yield break;
        notifying = true;
        yield return null;
        JobDispatcher.NotifyIdle(this);
        notifying = false;
    }

    private void HandleTaskCompleted()
    {
        if (currentJob == null) return;
        if (currentJob.type == CommandType.Haul ||
            currentJob.type == CommandType.Build ||
            currentJob.type == CommandType.Craft ||
            currentJob.type == CommandType.Mine) return;
        Finish(true);
    }

    private void Finish(bool success)
    {
        var finished = currentJob;
        currentJob = null;
        onComplete?.Invoke(finished, success);

        StartCoroutine(NotifyIdleNextFrame());      
    }

}
