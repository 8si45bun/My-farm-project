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
    public StorageBox fromStorage;
    public BioFuelGenerator toGenerator;
    public ItemType haulItem;
    public int haulCount = 1;

    //Build
    public Thing targetThing;
    public int buildMinutes = 10;

    //Craft
    public RecipeData recipeData;
    public int recipeMinute = 3;

    //Miner
    public int MinerMinute = 5;    

    //Demolition
    public int DemolitionMinute = 5;
}
