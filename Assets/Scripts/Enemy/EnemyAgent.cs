using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyAgent : MonoBehaviour
{
    public static HashSet<EnemyAgent> All = new HashSet<EnemyAgent>();

    [Header("Stats")]
    public float moveSpeed = 2f;
    public float attackDamage = 1f;
    public float maxHp = 10f;
    public float wallBreakTime = 3f;

    [HideInInspector] public float currentHp;
    [HideInInspector] public EnemyState currentState = EnemyState.Idle;

    [Header("Pathfinding")]
    public Vector2Int bottomLeft;
    public Vector2Int topRight;

    private List<Vector2Int> path;
    private int pathIndex;
    private Tilemap wallTilemap;
    private Thing targetBuilding;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    private void Start()
    {
        currentHp = maxHp;
        currentState = EnemyState.Moving;
        FindTargetAndPath();
    }

    /// <summary>
    /// EnemySpawner에서 호출: 맵 정보 및 타일맵 참조 주입
    /// </summary>
    public void Init(Vector2Int bl, Vector2Int tr, Tilemap wallMap)
    {
        bottomLeft = bl;
        topRight = tr;
        wallTilemap = wallMap;
    }

    private void FindTargetAndPath()
    {
        // 가장 가까운 완성된 건물을 타겟으로
        Vector2Int myPos = Vector2Int.RoundToInt(transform.position);
        Thing closest = null;
        float minDist = float.MaxValue;

        var things = Object.FindObjectsByType<Thing>(FindObjectsSortMode.None);
        foreach (var t in things)
        {
            if (t.stage != BuildStage.Finished) continue;

            float d = Vector2.Distance(transform.position, t.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = t;
            }
        }

        if (closest == null)
        {
            currentState = EnemyState.Idle;
            return;
        }

        targetBuilding = closest;
        Vector2Int targetPos = Vector2Int.RoundToInt(closest.transform.position);

        path = Pathfinder.FindPathTwoPhase(myPos, targetPos, bottomLeft, topRight);

        if (path == null || path.Count == 0)
        {
            currentState = EnemyState.Idle;
            return;
        }

        pathIndex = 0;
        StartCoroutine(FollowPath());
    }

    private IEnumerator FollowPath()
    {
        currentState = EnemyState.Moving;

        while (pathIndex < path.Count)
        {
            Vector2Int nextCell = path[pathIndex];

            // 벽 노드인지 확인
            if (Pathfinder.IsWallAt(nextCell))
            {
                yield return StartCoroutine(BreakWall(new Vector3Int(nextCell.x, nextCell.y, 0)));
            }

            Vector3 targetWorld = new Vector3(nextCell.x, nextCell.y, 0f);

            while (Vector3.Distance(transform.position, targetWorld) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, targetWorld, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetWorld;
            pathIndex++;
        }

        // 경로 끝에 도달 → 건물 공격
        if (targetBuilding != null && targetBuilding.stage == BuildStage.Finished)
        {
            AttackBuilding(targetBuilding);
        }

        // 새 타겟 찾기
        yield return null;
        FindTargetAndPath();
    }

    private IEnumerator BreakWall(Vector3Int cell)
    {
        currentState = EnemyState.Attacking;
        yield return new WaitForSeconds(wallBreakTime);

        // 타일맵에서 벽 제거
        if (wallTilemap != null)
        {
            wallTilemap.SetTile(cell, null);
        }

        // RobotManager 그리드 갱신 (모든 로봇의 그리드)
        var robots = Object.FindObjectsByType<RobotManager>(FindObjectsSortMode.None);
        foreach (var robot in robots)
        {
            // 그리드의 벽 정보를 업데이트하기 위해 노드 직접 접근
            int ix = cell.x - robot.bottomLeft.x;
            int iy = cell.y - robot.topRight.y;
            // 외부에서 직접 접근이 어려우면 그냥 넘어감
        }

        currentState = EnemyState.Moving;
    }

    private void AttackBuilding(Thing thing)
    {
        if (thing == null) return;
        currentState = EnemyState.Attacking;
        thing.Setstage(BuildStage.BulePrint);
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        if (currentHp <= 0f)
        {
            currentState = EnemyState.Dead;
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }
}
