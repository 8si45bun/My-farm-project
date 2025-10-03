using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
public class PlantManager : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap farmTilemap;
    public Tilemap plantTilemap;
    public Tilemap wallTilemap;

    private class PlantInstance
    {
        public PlantData data;
        public int stage;
        public int minutesInStage; 
    }

    private readonly Dictionary<Vector3Int, PlantInstance> plants = new();

    private void OnEnable()
    {
        TimeManager.OnMinuteChanged += OnMinuteTick;
    }
    private void OnDisable()
    {
        TimeManager.OnMinuteChanged -= OnMinuteTick;
    }

    private void OnMinuteTick()
    {
        var keys = new List<Vector3Int>(plants.Keys);
        foreach (var cell in keys)
        {
            var p = plants[cell];

            if (p.stage >= p.data.stages.Length - 1) continue;

            p.minutesInStage++;
            int need = p.data.minutesPerStage[p.stage];
            if(p.minutesInStage >= need)
            {
                p.stage++;
                p.minutesInStage = 0;
                plantTilemap.SetTile(cell, p.data.stages[p.stage]);
            }

        }

    }

    public bool HasPlant(Vector3Int cell)
    {
        return plants.ContainsKey(cell);
    }

    public bool IsMature(Vector3Int cell)
    {
        PlantInstance p;
        if(plants.TryGetValue(cell, out p))
        {
            int lastStageIndex = p.data.stages.Length - 1;
            return p.stage >= lastStageIndex;
        }
        return false;
    }

    public bool PlantAt(Vector3Int cell, PlantData data)
    {
        if(!farmTilemap.HasTile(cell)) return false;
        if(plants.ContainsKey(cell)) return false;

        plants[cell] = new PlantInstance { data = data, stage = 0, minutesInStage = 0 };
        plantTilemap.SetTile(cell, data.stages[0]);
        return true;
    }

    public bool WildPlantAt(Vector3Int cell, PlantData data)
    {
        if (wallTilemap.HasTile(cell)) return false;
        if (farmTilemap.HasTile(cell)) return false;
        if (plants.ContainsKey(cell)) return false;

        plants[cell] = new PlantInstance { data = data, stage = 0, minutesInStage = 0 };
        plantTilemap.SetTile(cell, data.stages[0]);
        return true;
    }

    public bool HarvestAt(Vector3Int cell, PlantData data)
    {
        if(!plants.TryGetValue(cell, out var p)) return false;
        if (p.stage < p.data.stages.Length - 1) return false;

        string plantId = p.data.plantID;

        plants.Remove(cell);
        plantTilemap.SetTile(cell, null);

        Vector3 center = plantTilemap.GetCellCenterWorld(cell);
        for(int i = 0; i < Mathf.Max(1, p.data.dropCount); i++)
        {
            var pos = center += new Vector3(Random.Range(0f, 0.4f), Random.Range(0f, 0.4f), Random.Range(0f, 0.4f));
            var go = Instantiate(p.data.dropCrop, pos , Quaternion.identity);

        }

        return true;
    }



}
