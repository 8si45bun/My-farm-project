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
        return wall != null && wall.HasTile(cell);
    }

    public bool CanCultivate(Vector3Int cell)
    {
        if (wall != null && wall.HasTile(cell)) return false;
        return farm != null && !farm.HasTile(cell); // 아직 경작X
    }

    public bool CanPlant(Vector3Int cell)
    {
        if (farm == null || !farm.HasTile(cell)) return false;
        return plant == null || !plant.HasPlant(cell);
    }

    public bool CanHarvest(Vector3Int cell)
    {
        if (plant == null) return false;
        return plant.HasPlant(cell) && plant.IsMature(cell);
    }

    public bool CanMove(Vector3Int cell)
    {
        return wall == null || !wall.HasTile(cell);
    }
}
