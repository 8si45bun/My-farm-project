using System;
using UnityEngine;

[Serializable]
public class Job
{
    public Guid id = Guid.NewGuid();
    public CommandType type;
    public int floorId;                // 현재 층만 사용한다면 0으로 고정해도 OK
    public Vector3Int cell;            // 대상 타일(행동은 인접칸에서 수행)
    public PlantData plantData;        // Plant 전용
    public JobStatus status = JobStatus.Queued;
    [NonSerialized] public float createdAt;
    public int priority = 0;           // 동일 우선
    public DroppedItem fromItem;       // Haul 전용
    public StorageBox toStorage;       // Haul 전용

    public override string ToString()
    {
        return $"[{type}] cell={cell} floor={floorId} status={status}";
    }
}
