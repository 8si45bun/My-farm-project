using UnityEngine;

public class PlantUI : MonoBehaviour
{
    public static PlantUI Instance { get; private set; }

    public PlantData CurrentPlant { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SelectPlant(PlantData data)
    {
        CurrentPlant = data;
        Debug.Log($"선택한 작물: {data.name}");
    }
}
