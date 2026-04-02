using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
public class JobDispatcher : MonoBehaviour
{
    public static JobDispatcher _inst;
    public static JobDispatcher Instance
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("JobDispatcher");
                _inst = go.AddComponent<JobDispatcher>();
                DontDestroyOnLoad(go);
            }

            return _inst;
        }
    }

    public static event Action<Job, bool> OnJobCompleted;

    public static IReadOnlyList<Job> GetQueueCreaterJob(Thing thing)
    {
        return Instance.jobList
            .Where(j =>
                j != null &&
                j.status == JobStatus.Queued &&
                j.type == CommandType.Craft &&
                j.targetThing == thing)
            .ToList();
    }

    private bool wasRaidActive;
    private List<Job> jobList = new(); // ť�� �յڸ� �����ؼ� �Ѱ谡 ����
    private HashSet<RobotAgent> robots = new(); 

    public static void Enqueue(Job job)
    {
        job.status = JobStatus.Queued;
        Instance.jobList.Add(job);
        Instance.TryAssignJobs();
    }

    public static void EnqueueMany(IEnumerable<Job> jobs)
    {
        foreach (var job in jobs)
        {
            job.status = JobStatus.Queued;
            Instance.jobList.Add(job);
            Instance.TryAssignJobs();
        }
    }

    public static void Register(RobotAgent robotAgent) { Instance.robots.Add(robotAgent); }
    public static void UnRegister(RobotAgent robotAgent) { Instance.robots.Remove(robotAgent); }
    public static void NotifyIdle(RobotAgent robotAgent) { Instance.TryAssignJobs(); }

    private void Update()
    {
        bool raidActive = EnemyAgent.All.Count > 0;
        if (wasRaidActive && !raidActive)
        {
            Debug.Log("레이드 종료 감지 → 수리 작업 생성");
            GenerateRepairJobs();
        }
        wasRaidActive = raidActive;
    }

    private void GenerateRepairJobs()
    {
        var things = FindObjectsByType<Thing>(FindObjectsSortMode.None);
        foreach (var t in things)
        {
            if (t.stage == BuildStage.BulePrint)
            {
                var cell = Vector3Int.RoundToInt(t.transform.position);
                Enqueue(new Job
                {
                    type = CommandType.Repair,
                    cell = cell,
                    targetThing = t,
                    repairMinutes = 8
                });
            }
        }
    }

    private void TryAssignJobs()
    {
        var idel = new List<RobotAgent>();

        foreach (var r in robots)
        {
            if(r.IsIdle())
                idel.Add(r);
        }
        if (idel.Count == 0) return;

        foreach(var j in jobList.ToList())
        {
            if((j.type == CommandType.Craft || 
                j.type == CommandType.Build || 
                j.type == CommandType.Deconstruct) && 
                j.targetThing != null 
                && HasActiveJob(j.targetThing))
                continue;

            var best = PickBestRobot(j, idel);
            if (best == null) continue;

            bool accepted = best.AcceptJob(j, OnJobCompleted);
            if (accepted)
            {
                j.status = JobStatus.InProgress;
                jobList.Remove(j);
                idel.Remove(best);

                if (idel.Count == 0) break;
            }
        }
    }

    private RobotAgent PickBestRobot(Job job, List<RobotAgent> robots)
    {
        RobotAgent bestRobot = null;
        int bestDis = int.MaxValue;

        foreach (var robot in robots)
        {
            if (!robot.CanDoJob(job.type)) continue;

            Vector3 robotP = robot.transform.position;
            int dis = Mathf.Abs((Mathf.RoundToInt(robotP.x) - job.cell.x) +
                (Mathf.RoundToInt(robotP.y) - job.cell.y));

            if(dis < bestDis)
            {
                bestDis = dis;
                bestRobot = robot;
            }
        }
        return bestRobot;
    }

    private void OnJobFinished(Job job, bool success)
    {
        job.status = success ? JobStatus.Done : JobStatus.Failed;
        OnJobCompleted?.Invoke(job, success);

        TryAssignJobs();
    }

    public static Job GetActiveCraftJob(Thing thing)
    {
        if (thing == null) return null;

        foreach (var robot in Instance.robots)
        {
            if (robot == null) continue;

            var job = robot.CurrentJob;
            if (job == null) continue;
            if (job.type != CommandType.Craft) continue;
            if (job.targetThing != thing) continue;

            return job;
        }

        return null;
    }

    public bool HasActiveJob(Thing thing)
    {
        if (thing == null) return false;

        foreach (var robot in robots)
        {
            if (robot == null) continue;

            var job = robot.CurrentJob;
            if (job == null) continue;
            if (job.type != CommandType.Craft && job.type != CommandType.Build
                && job.type != CommandType.Deconstruct) continue;
            if (job.targetThing != thing) continue;

            return true;
        }

        return false;
    }

}