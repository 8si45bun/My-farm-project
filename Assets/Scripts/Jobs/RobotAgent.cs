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
        // 1) 아이템 유효성 재검증
        if (job.fromItem == null || job.fromItem.gameObject == null)
        {
            Finish(false); yield break;
        }

        // 2) 아이템 인접칸 이동
        var itemCell = Vector3Int.RoundToInt(job.fromItem.transform.position);
        robot.MoveToAdjacent(itemCell);
        while (robot.IsBusy) yield return null;

        if (job.fromItem == null || job.fromItem.gameObject == null)
        {
            Finish(false); yield break;
        }

        // 3) 픽업(적재량 1개 가정)
        job.fromItem.Pickup(); // 실제로는 인벤토리 시스템이 있으면 거기로
        Reservations.ReleaseItem(job.fromItem);

        // 4) 보관함 인접칸 이동
        if (job.toStorage == null) { Finish(false); yield break; }
        var storageCell = Vector3Int.RoundToInt(job.toStorage.transform.position);
        robot.MoveToAdjacent(storageCell);
        while (robot.IsBusy) yield return null;

        // 5) 저장
        job.toStorage.Store(ItemType.Generic, 1);
        Reservations.ReleaseStorage(job.toStorage);

        Finish(true);
    }

    private void HandleTaskCompleted()
    {
        // RobotManager의 이동+행동 사이클 종료 콜백
        if (currentJob == null) return;
        // 작업 후 조건 재검증(간단 버전): 성공으로 간주
        Finish(true);
    }

    private void Finish(bool success)
    {
        var finished = currentJob;
        currentJob = null;
        onComplete?.Invoke(finished, success);
        JobDispatcher.NotifyIdle(this);
    }
}
