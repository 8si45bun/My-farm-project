using UnityEngine;
using UnityEngine.Tilemaps;

public class WildPlantSpawner : MonoBehaviour
{
    [Header("참조")]
    public PlantManager plantManager;
    public PlantData[] plantData;

    [Header("Grid")]
    public Vector2Int BottomLeft, TopRight;

    public float chance = 1f;

    private void Awake()
    {
        InitGrid();
    }

    private void InitGrid()
    {
        int sizeX = TopRight.x - BottomLeft.x + 1;
        int sizeY = TopRight.y - BottomLeft.y + 1;

        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                Vector2Int pos = new Vector2Int(i + BottomLeft.x, j + BottomLeft.y);

                foreach (Collider2D col in Physics2D.OverlapCircleAll(pos, 0.4f))
                {
                    if (col.gameObject.layer == LayerMask.NameToLayer("Dirt"))
                    {
                        SpawnWildPlant(pos);
                    }
                    else
                        Debug.Log("WildPlantSpawner스크립트에서 Dirt가 없습니다");
                }
            }
        }
    }

    private void SpawnWildPlant(Vector2Int pos)
    {
        float value = Random.Range(0f, 10f);
        int plantIndex = Random.Range(0, 2);
        if (value < chance && plantManager.WildPlantAt((Vector3Int)pos, plantData[plantIndex]))
            Debug.Log("야생 식물 생성");


    }
    // 랜덤 식물을 랜덤 확률로 자라나게 하게
}
