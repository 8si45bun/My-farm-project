using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RobotAgent : MonoBehaviour
{
    [Header("로봇 배터리")]
    public float maxBattery = 100f;
    public float currentBattery;
    public float workDrainRate = 1.0f; // 작업 시 초당 소모량
    public float moveDrainRate = 0.5f; // 이동 시 초당 소모량
    public float chargeRate = 10f;    // 충전 속도
    public float lowBatteryThreshold = 10f; // 배터리 부족 임계치
    public float chargedThreshold = 90f; // 충전 완료 임계치

    [Header("작업 메모리")]
    private Job savedJob;
    private Action<Job, bool> savedCallback;

    [Header("하울")]
    public bool isCarrying = false;
    public ItemType carriedType;
    public int carryingAmount;

    [Header("해체 환급 아이템 프리팹")]
    public GameObject woodPrefab;
    public GameObject steelPrefab;

    private RobotProgress progress;
    private RobotManager robot;
    private Job currentJob;
    private Action<Job, bool> onComplete;
    public Job CurrentJob => currentJob;
    public RobotState currentState = RobotState.Idle; // 로봇 상태

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

    private void HandleBattery()
    {
        bool isPowered = false;

        if(PowerManager.Instance != null)
        {
            isPowered = PowerManager.Instance.IsIInPowerRange(robot.transform.position);
        }

        if (isPowered)
        {
            // 범위 내일때 충전
            currentBattery += chargeRate * Time.deltaTime;

            if(currentState == RobotState.Charging && currentBattery >= chargedThreshold)
            {
                Debug.Log("충전 완료 (구역 내)");
                currentState = RobotState.Idle;
                StartCoroutine(NotifyIdleNextFrame());
            }
        }
        else
        {
            float drainRate = 0f;
            if (currentState == RobotState.Working) drainRate = workDrainRate;
            else if (currentState == RobotState.Moving) drainRate = moveDrainRate;
            
            currentBattery -= drainRate * Time.deltaTime;
        }

        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);

        if (!isPowered && currentBattery <= lowBatteryThreshold && currentState != RobotState.Emergency)
        {
            Debug.Log("배터리 부족 전력망 구역으로 대피합니다.");

            if (currentJob != null)
            {
                savedJob = currentJob;
                savedCallback = onComplete;
                currentJob = null;
                onComplete = null;
            }

            currentState = RobotState.Emergency;
            robot.GoToChargeStation(); // 가장 가까운 전력 구역으로    
        }

        if (currentState == RobotState.Emergency && isPowered)
        {
            Debug.Log("전력망 진입 충전 대기.");
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

        // 건물 제거
        Destroy(job.targetThing.gameObject);

        yield return null;
        Finish(true);
    }

        private IEnumerator CraftRoutine(Job job)
    {
        Debug.Log("제작 명령");
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
            Debug.Log("제작 후 운반");
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
        Debug.Log("건설 명령");
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
        // 줍기 (아무것도 안 들고 있을 때만 실행)
        if (!isCarrying)
        {
            // CASE A: 창고 -> 발전기 (연료 보급)
            if (job.fromStorage != null && job.toGenerator != null)
            {
                var storageCell = Vector3Int.RoundToInt(job.fromStorage.transform.position);
                robot.MoveToAdjacent(storageCell);
                while (robot.IsBusy) yield return null;

                // 도착 후 검증
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
                carriedType = job.haulItem; // 가져오려는 아이템 타입
                carryingAmount = amount;
            }

            // CASE B: 땅에 떨어진 아이템 -> 창고 (일반 운반)
            else if (job.fromItem != null)
            {
                // 아이템 유효성 검사
                if (job.fromItem == null || job.fromItem.gameObject == null)
                {
                    Finish(false); yield break;
                }

                // 이동
                var itemCell = Vector3Int.RoundToInt(job.fromItem.transform.position);
                robot.MoveToAdjacent(itemCell);
                while (robot.IsBusy) yield return null;

                // 이동 후 다시 확인 
                if (job.fromItem == null || job.fromItem.gameObject == null)
                {
                    Finish(false); yield break;
                }

                // 픽업 및 인벤토리 업데이트
                var it = job.fromItem;
                isCarrying = true;
                carriedType = it.itemType;
                carryingAmount = Mathf.Max(1, it.amount);

                it.Pickup(); // 맵 오브젝트 제거
                job.fromItem = null; // 참조 제거 (메모리 누수 방지)
            }
            else
            {
                Finish(false); yield break;
            }
        }
        else
        {
            Debug.Log("이미 물건을 소지중입니다. 줍기 단계를 건너뜁니다.");
        }

        // 놓기 / 배달 (물건을 들고 있을 때만 실행)
        if (isCarrying)
        {
            // CASE A: 발전기로 배달
            if (job.toGenerator != null)
            {
                var genCell = Vector3Int.RoundToInt(job.toGenerator.transform.position);
                robot.MoveToAdjacent(genCell);
                while (robot.IsBusy) yield return null;

                // 발전기에 연료 주입
                if (job.toGenerator != null)
                {
                    job.toGenerator.OnFuelDelivered(carryingAmount);
                }
            }
            // CASE B: 창고로 배달
            else if (job.toStorage != null)
            {
                var storageCell = Vector3Int.RoundToInt(job.toStorage.transform.position);
                robot.MoveToAdjacent(storageCell);
                while (robot.IsBusy) yield return null;

                // 창고에 저장
                if (job.toStorage != null)
                {
                    job.toStorage.Store(carriedType, carryingAmount);
                }
            }

            // 배달 완료 후 인벤토리 비우기
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
