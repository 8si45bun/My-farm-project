using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class JobDispatcher : MonoBehaviour
{
    private static JobDispatcher _inst;
    public static JobDispatcher I
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

    private readonly List<Job> queue = new();
    private readonly HashSet<RobotAgent> robots = new();

    // === Public API ===
    public static void Enqueue(Job job)
    {
        job.createdAt = Time.time;       
        I.queue.Add(job);
        I.TryAssignJobs();
    }
    public static void EnqueueMany(IEnumerable<Job> jobs)
    {
        float now = Time.time;            
        foreach (var j in jobs)
            j.createdAt = now;          
        I.queue.AddRange(jobs);
        I.TryAssignJobs();
    }

    public static void Register(RobotAgent agent) => I.robots.Add(agent);
    public static void Unregister(RobotAgent agent) => I.robots.Remove(agent);
    public static void NotifyIdle(RobotAgent agent) => I.TryAssignJobs();

    // === Core ===
    private void TryAssignJobs()
    {
        // 1) 먼저 수동 Job(Queued)들만 본다.
        var queued = queue.Where(j => j.status == JobStatus.Queued && j.type != CommandType.Haul).ToList();
        if (queued.Count > 0)
        {
            AssignBatch(queued);
            return;
        }

        // 2) 수동이 없다면 Idle 로봇에 하울링 Fallback 제공
        var anyIdle = robots.Any(r => r.IsIdle);
        if (anyIdle)
        {
            var haulJob = HaulingManager.TryMakeHaulJob(); // 없으면 null
            if (haulJob != null)
            {
                queue.Add(haulJob);
                AssignBatch(new List<Job> { haulJob });
            }
        }
    }

    private void AssignBatch(List<Job> jobs)
    {
        // Idle 로봇만 대상으로
        var idleRobots = robots.Where(r => r.IsIdle).ToList();
        if (idleRobots.Count == 0) return;

        foreach (var job in jobs.ToList())
        {
            var best = PickBestRobotFor(job, idleRobots);
            if (best == null) continue;

            // 예약(중복 방지)
            if (job.type == CommandType.Haul)
            {
                if (job.fromItem == null || job.toStorage == null) continue;
                if (!Reservations.TryReserveItem(job.fromItem)) continue;
                if (!Reservations.TryReserveStorage(job.toStorage))
                {
                    Reservations.ReleaseItem(job.fromItem);
                    continue;
                }
            }
            else
            {
                if (!Reservations.TryReserveCell(job.cell, job.id.ToString())) continue;
            }

            job.status = JobStatus.Reserved;
            var accepted = best.AcceptJob(job, OnJobComplete);
            if (accepted)
            {
                queue.Remove(job);
                idleRobots.Remove(best);
                if (idleRobots.Count == 0) break;
            }
            else
            {
                // 예약 롤백
                if (job.type == CommandType.Haul)
                {
                    Reservations.ReleaseItem(job.fromItem);
                    Reservations.ReleaseStorage(job.toStorage);
                }
                else
                {
                    Reservations.ReleaseCell(job.cell, job.id.ToString());
                }
            }
        }
    }

    private RobotAgent PickBestRobotFor(Job job, List<RobotAgent> candidates)
    {
        // Idle 우선은 이미 필터링됨. 거리 근사(맨해튼)로 최단 선택.
        RobotAgent best = null;
        int bestCost = int.MaxValue;

        foreach (var r in candidates)
        {
            // 층이 달라도 일단 근사비용: 같은 층=맨해튼, 다른 층=가중치 + 맨해튼 (전이 TODO)
            int penalty = (r.floorId == job.floorId) ? 0 : 50; // 전이 가중치(임시)
            Vector3 rp = r.transform.position;
            int dist = Mathf.Abs(job.cell.x - Mathf.RoundToInt(rp.x)) + Mathf.Abs(job.cell.y - Mathf.RoundToInt(rp.y)) + penalty;

            if (dist < bestCost)
            {
                bestCost = dist;
                best = r;
            }
        }
        return best;
    }

    private void OnJobComplete(Job job, bool success)
    {
        // 예약 해제
        if (job.type == CommandType.Haul)
        {
            Reservations.ReleaseStorage(job.toStorage);
        }
        else
        {
            Reservations.ReleaseCell(job.cell, job.id.ToString());
        }

        job.status = success ? JobStatus.Done : JobStatus.Failed;

        // 남은 작업 혹은 하울링 재시도
        TryAssignJobs();
    }
}
