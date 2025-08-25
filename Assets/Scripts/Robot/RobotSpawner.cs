using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RobotSpawner : MonoBehaviour
{
    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private GameObject robot;
    [SerializeField]
    private int robotCount = 10;

    private Vector3 offset = new Vector3(0.5f, -0.05f, 0.5f);
    private List<Vector3> possibleTiles = new List<Vector3>();

    private void Awake()
    {
        // Tilemap의 Bounds 재설정 (맵을 수정했을 때 Bounds 가 변경되지 않는 문제 해결)
        tilemap.CompressBounds();
        // 타일맵의 모든 타일을 대상으로 적 배치가 가능한 타일 계산
        CalculatePossibleTiles();

        // 임의의 타일에 enemyCount 숫자만큼 적 생성
        for(int i = 0; i < robotCount; i++)
        {
            int index = Random.Range(0, possibleTiles.Count);
            GameObject clone = Instantiate(robot, possibleTiles[index], Quaternion.identity, transform);
        }
    }

    private void CalculatePossibleTiles()
    {
        BoundsInt bounds = tilemap.cellBounds;
        // 타일맵 내부 모든 타일의 정보를 불러와 alltiles 배열에 저장
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);

        // 외곽 라인을 제ㅐ외한 모든 타일 검사
        for (int y = 1; y < bounds.size.y - 1; ++y) {
            for (int x = 1; x < bounds.size.x - 1; ++x)
            {
                TileBase tile = allTiles[y * bounds.size.x + x];
                // 해당 타일이 비어있지 않으면 적 배치 가능 타일로 판단

                if (tile != null) { 
                    Vector3Int localPosition = bounds.position + new Vector3Int(x, y);
                    Vector3 position = tilemap.CellToWorld(localPosition) + offset;
                    position.z = 0;
                    possibleTiles.Add(position);   
                }
            }
        }
    }
}
