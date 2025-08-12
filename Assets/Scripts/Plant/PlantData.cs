using UnityEngine;
using UnityEngine.Tilemaps;

public class PlantData : MonoBehaviour
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

    private void OnVaildate()
    {
        if (stages == null || stages.Length == 0) return;
        if(minutesPerStage == null || minutesPerStage.Length != stages.Length- 1) return;
    }
}
