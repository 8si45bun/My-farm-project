using System;
using System.Collections;
using UnityEngine;

public class RobotAgent : MonoBehaviour
{
    private RobotProgress progress;
    private RobotManager robot;
    private Job currentJob;
    private Action<Job, bool> onComplete;
    public Job CurrentJob => currentJob;
    private void Awake()
    {
        robot = GetComponent<RobotManager>();
        robot.OnTaskCycleCompleted += HandleTaskCompleted;
        progress = GetComponent<RobotProgress>();
    }

    private void OnEnable() { JobDispatcher.Register(this); }
    private void OnDisable() { JobDispatcher.UnRegister(this); }

    public bool IsIdle()
    {
        return (currentJob == null && !robot.IsBusy);
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
            default:
                currentJob = null;
                return false;
        }
        job.status = JobStatus.InProgress;
        return true;
    }

    private IEnumerator CraftRoutine(Job job)
    {
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

    private IEnumerator HaulRoutine(Job job)
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

        it.Pickup();
        job.fromItem = null;

        // 보관함 인접칸 이동
        if (job.toStorage == null) { Finish(false); yield break; }
        var storageCell = Vector3Int.RoundToInt(job.toStorage.transform.position);
        robot.MoveToAdjacent(storageCell);
        while (robot.IsBusy) yield return null;

        // 저장
        job.toStorage.Store(pickedType, pickedAmount);

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
            currentJob.type == CommandType.Craft) return;
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
