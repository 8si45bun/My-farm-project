using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "PlantData", menuName = "MyFarm/PlantData")]
public class PlantData : ScriptableObject
{
    [Header("PlantID")]
    public string plantID = "Corn";      

    [Header("성장 단계 타일")]
    public TileBase[] stages;

    [Header("성장 단계별 시간")]
    public int[] minutesPerStage;

    [Header("수확물")]
    public GameObject dropCrop;
    public int dropCount = 1;

}
