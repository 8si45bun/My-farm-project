using System;
using UnityEngine;

[Serializable]
public class Job
{
    public CommandType type;
    public JobStatus status;
    public Vector3Int cell;            
    public PlantData plantData;   
    public DroppedItem fromItem;       // Haul 전용
    public StorageBox toStorage;       // Haul 전용

}
