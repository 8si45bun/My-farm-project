using System;
using UnityEngine;

[RequireComponent(typeof(RobotManager))]
public class RobotAgent : MonoBehaviour
{
    public int floorId = 0;                    // 현재 소속 층(추후 전이 지원)
    public bool IsIdle => currentJob == null && !robot.IsBusy;

    private RobotManager robot;
    private Job currentJob;
    private Action<Job, bool> onComplete;      // 디스패처 콜백

    private void Awake()
    {
        robot = GetComponent<RobotManager>();
        robot.manualControl = false;           // 입력 직접 조종 비활성화
        robot.OnTaskCycleCompleted += HandleTaskCompleted;
    }

    private void OnEnable() => JobDispatcher.Register(this);
    private void OnDisable() => JobDispatcher.Unregister(this);

    public bool AcceptJob(Job job, Action<Job, bool> completionCallback)
    {
        if (!IsIdle) return false;
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
                // Haul: 아이템 위치 → 인접칸 이동 후 픽업 → 스토리지 인접칸 이동 → 저장
                StartCoroutine(HaulRoutine(job));
                break;
            default:
                Debug.LogWarning($"Unknown job {job}");
                currentJob = null;
                return false;
        }
        job.status = JobStatus.InProgress;
        return true;
    }

    private System.Collections.IEnumerator HaulRoutine(Job job)
    {
        // 아이템 유효성 재검증
        if (job.fromItem == null || job.fromItem.gameObject == null)
        {
            Finish(false); yield break;
        }

        // 아이템 인접칸 이동
        var itemCell = Vector3Int.RoundToInt(job.fromItem.transform.position);
        robot.MoveToAdjacent(itemCell);
        while (robot.IsBusy) yield return null;

        if (job.fromItem == null || job.fromItem.gameObject == null)
        {
            Finish(false); yield break;
        }

        // 픽업
        var it = job.fromItem;
        ItemType pickedType = it.itemType;
        int pickedAmount = Mathf.Max(1, it.amount);

        Reservations.ReleaseItem(it);
        it.Pickup();
        job.fromItem = null;

        // 보관함 인접칸 이동
        if (job.toStorage == null) { Finish(false); yield break; }
        var storageCell = Vector3Int.RoundToInt(job.toStorage.transform.position);
        robot.MoveToAdjacent(storageCell);
        while (robot.IsBusy) yield return null;

        // 저장
        job.toStorage.Store(pickedType, pickedAmount);
        Reservations.ReleaseStorage(job.toStorage);

        Finish(true);
    }

    private void HandleTaskCompleted()
    {
        if (currentJob == null) return;

        if (currentJob.type == CommandType.Haul) return;

        Finish(true);
    }

    private void Finish(bool success)
    {
        var finished = currentJob;

        if (finished == null)
        {
            Debug.LogWarning("[RobotAgent] Finish called but currentJob == null (duplicate finish?)");
            return;
        }

        currentJob = null;
        onComplete?.Invoke(finished, success);
        JobDispatcher.NotifyIdle(this);
    }
}
