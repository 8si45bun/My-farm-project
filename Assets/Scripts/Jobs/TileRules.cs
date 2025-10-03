using UnityEngine;
using UnityEngine.Tilemaps;

public class TileRules
{
    private readonly Tilemap wall;
    private readonly Tilemap farm;
    private readonly PlantManager plant;

    public TileRules(Tilemap wall, Tilemap farm, PlantManager plant)
    {
        this.wall = wall; this.farm = farm; this.plant = plant;
    }

    public bool CanDig(Vector3Int cell)
    {
        return wall.HasTile(cell);
    }

    public bool CanCultivate(Vector3Int cell)
    {
        if (wall.HasTile(cell)) return false;
        return !farm.HasTile(cell);  
    }

    public bool CanPlant(Vector3Int cell)
    {
        return !plant.HasPlant(cell) && farm.HasTile(cell);
    }

    public bool CanHarvest(Vector3Int cell)
    {
        return plant.HasPlant(cell) && plant.IsMature(cell);
    }

    public bool CanMove(Vector3Int cell)
    {
        return !wall.HasTile(cell);
    }
}
