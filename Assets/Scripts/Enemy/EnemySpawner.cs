using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public float spawnIntervalHours = 12f;
    public int enemiesPerWave = 3;
    public int waveIncrement = 2;

    [Header("Map Bounds")]
    public Vector2Int bottomLeft;
    public Vector2Int topRight;

    [Header("References")]
    public Tilemap wallTilemap;

    public bool IsRaidActive => EnemyAgent.All.Count > 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(RaidTimer());
    }

    private IEnumerator RaidTimer()
    {
        while (true)
        {
            int minutes = Mathf.RoundToInt(spawnIntervalHours * 60f);
            yield return TimeManager.WaitGameMinutes(minutes);

            SpawnWave();
            enemiesPerWave += waveIncrement;
        }
    }

    private void SpawnWave()
    {
        Debug.Log($"적 웨이브 스폰: {enemiesPerWave}마리");

        for (int i = 0; i < enemiesPerWave; i++)
        {
            Vector3 spawnPos = GetEdgeSpawnPosition();

            var go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            var agent = go.GetComponent<EnemyAgent>();
            if (agent != null)
            {
                agent.Init(bottomLeft, topRight, wallTilemap);
            }
        }
    }

    private Vector3 GetEdgeSpawnPosition()
    {
        // 맵 가장자리 4면 중 랜덤 위치
        int side = Random.Range(0, 4);
        float x, y;

        switch (side)
        {
            case 0: // 상단
                x = Random.Range(bottomLeft.x, topRight.x + 1);
                y = topRight.y;
                break;
            case 1: // 하단
                x = Random.Range(bottomLeft.x, topRight.x + 1);
                y = bottomLeft.y;
                break;
            case 2: // 좌측
                x = bottomLeft.x;
                y = Random.Range(bottomLeft.y, topRight.y + 1);
                break;
            default: // 우측
                x = topRight.x;
                y = Random.Range(bottomLeft.y, topRight.y + 1);
                break;
        }

        return new Vector3(x, y, 0f);
    }
}
