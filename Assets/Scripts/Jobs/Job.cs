using System;
using UnityEngine;

[Serializable]
public class Job
{
    public CommandType type;
    public JobStatus status;
    public Vector3Int cell;            
    public PlantData plantData;
    // Haul
    public DroppedItem fromItem;  
    public StorageBox toStorage;

    //Build
    public Thing targetThing;
    public int buildMinutes = 10;

    //Craft
    public RecipeData recipeData;
    public int recipeMinute = 3;
}
