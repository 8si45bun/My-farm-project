using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class Node
{
    public Node(bool _isWall, int _x, int _y)
    {
        isWall = _isWall;
        x = _x;
        y = _y;
    }

    public bool isWall;
    public Node ParentNode;

    // G : 시작으로부터 이동했던 거리, H : |가로|+|세로| 장애물 무시하여 목표까지의 거리, F : G + H
    public int x, y, G, H;
    public int F { get { return G + H; } }
}

public class RobotManager : MonoBehaviour
{
    [Header("References")]
    public RobotDetect robotDetect;

    [Header("Control")]
    public bool manualControl = true;

    public bool IsBusy { get; private set; }
    public event Action OnTaskCycleCompleted;

    [Header("Moving")]
    public Vector2Int bottomLeft, topRight;
    private Vector2Int startPos, targetPos;
    public List<Node> FinalNodeList = new List<Node>();
    public bool allowDiagonal, dontCrossCorner;
    public Rigidbody2D robotRigidbody;
    public Transform robotTransform;
    public float moveSpeed = 2f;
    public float FarmFloorSpeed = 0.5f;

    [Header("Harvest / Plant")]
    public PlantManager plantManager;
    public PlantData plantData;
    private bool isHarvest = false;

    [Header("Action Flags")]
    private bool isDigMode = false;
    private bool isCultivate = false;
    private bool isPlant = false;
    private Vector2Int TargetGrid;
    private Vector2Int robotGrid;
    private int wallLayerMask;
    private int softLayerMask;
    private bool onSoft;

    [Header("TileMaps")]
    public Tilemap wallTilemap;
    public Tilemap FloorTilemap;
    public Tilemap FarmTilemap;
    public Tilemap PlantTilemap;
    public Tile softDirt;
    public Tile plant; 

    int sizeX, sizeY;
    Node[,] NodeArray;
    Node StartNode, TargetNode, CurNode;
    List<Node> OpenList, ClosedList;

    private void Awake()
    {
        wallLayerMask = LayerMask.NameToLayer("Wall");
        softLayerMask = LayerMask.NameToLayer("SoftDirt");
        InitGrid();
    }

    private void InitGrid()
    {
        // NodeArray의 크기 정해주고, isWall, x, y 대입
        sizeX = topRight.x - bottomLeft.x + 1;
        sizeY = topRight.y - bottomLeft.y + 1;
        NodeArray = new Node[sizeX, sizeY];

        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                bool isWall = false;
                foreach (Collider2D col in Physics2D.OverlapCircleAll(new Vector2(i + bottomLeft.x, j + bottomLeft.y), 0.4f))
                {
                    if (col.gameObject.layer == LayerMask.NameToLayer("Wall"))
                        isWall = true;
                }

                NodeArray[i, j] = new Node(isWall, i + bottomLeft.x, j + bottomLeft.y);
            }
        }
    }

    private bool InBounds(Vector2Int g)  // 클릭/목표가 Grid 밖이면 IndexOutOfRange 방지
    {
        return g.x >= bottomLeft.x && g.x <= topRight.x &&
               g.y >= bottomLeft.y && g.y <= topRight.y;
    }

    private void Update()
    {
        // 외부 제어 모드면 입력 무시
        if (!manualControl) return;

        if (Input.GetMouseButtonDown(1))
        {
            Vector2 clickWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int clickGrid = Vector2Int.RoundToInt(clickWorld);

            if (!InBounds(clickGrid))
            {
                Debug.Log("그리드 범위를 벗어났습니다.");
                return;
            }

            Vector2 robotWorld = robotTransform.position;
            robotGrid = new Vector2Int(
                Mathf.RoundToInt(robotWorld.x),
                Mathf.RoundToInt(robotWorld.y)
            );
            startPos = robotGrid;

            if (Input.GetKey(KeyCode.Q))
            { // 채굴 모드
                if (IsWallAt(clickGrid))
                {
                    isCultivate = false;
                    isDigMode = true;
                    isPlant = false;
                    isHarvest = false;
                    TargetGrid = clickGrid;

                    targetPos = GetNearestAdjacent(clickGrid);
                }
                else
                {
                    Debug.Log("벽을 클릭해야 합니다.");
                    return;
                }
            }
            else if (Input.GetKey(KeyCode.E) && !IsWallAt(clickGrid))
            { // 땅 경작
                isDigMode = false;
                isCultivate = true;
                isHarvest = false;
                isPlant = false;
                TargetGrid = clickGrid;

                targetPos = GetNearestAdjacent(clickGrid);
            }
            else if (Input.GetKey(KeyCode.T) && IsCultivateAt(clickGrid))
            {
                var cell = new Vector3Int(clickGrid.x, clickGrid.y, 0);

                if (plantManager && plantManager.HasPlant(cell))
                {
                    if (plantManager.IsMature(cell))
                    {
                        isDigMode = isCultivate = isPlant = false;
                        isHarvest = true;
                        TargetGrid = clickGrid;
                        targetPos = GetNearestAdjacent(clickGrid);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    isDigMode = isCultivate = isHarvest = false;
                    isPlant = true;
                    TargetGrid = clickGrid;
                    targetPos = GetNearestAdjacent(clickGrid);
                }
            }
            else
            { // 이동 모드
                isDigMode = false;
                isHarvest = false;
                isCultivate = false;
                isPlant = false;
                targetPos = clickGrid;
            }

            StopAllCoroutines(); // 이전 행동 멈추기
            IsBusy = true;
            PathFinding();
        }
    }

    // 외부 제어용 래퍼
    public void MoveTo(Vector3Int cell)
    {
        PreparePathFlags(false, false, false, false, cell);
        PathFinding();
    }
    public void MoveToAdjacent(Vector3Int targetCell)
    {
        Vector2Int adj = GetNearestAdjacent((Vector2Int)targetCell);
        PreparePathFlags(false, false, false, false, (Vector3Int)adj);
        PathFinding();
    }
    public void StartDig(Vector3Int targetCell)
    {
        PreparePathFlags(true, false, false, false, targetCell);
        PathFinding();
    }
    public void StartCultivate(Vector3Int targetCell)
    {
        PreparePathFlags(false, true, false, false, targetCell);
        PathFinding();
    }
    public void StartPlant(Vector3Int targetCell, PlantData data)
    {
        this.plantData = data;
        PreparePathFlags(false, false, true, false, targetCell);
        PathFinding();
    }
    public void StartHarvest(Vector3Int targetCell)
    {
        PreparePathFlags(false, false, false, true, targetCell);
        PathFinding();
    }

    private void PreparePathFlags(bool dig, bool cult, bool plantFlag, bool harvest, Vector3Int targetCell)
    {
        isDigMode = dig; isCultivate = cult; isPlant = plantFlag; isHarvest = harvest;

        Vector2 robotWorld = robotTransform.position;
        robotGrid = new Vector2Int(
            Mathf.RoundToInt(robotWorld.x),
            Mathf.RoundToInt(robotWorld.y)
        );
        startPos = robotGrid;

        TargetGrid = (Vector2Int)targetCell;

        // 인접 동작이면 도착 목표는 인접칸, 이동만이면 목표칸
        if (dig || cult || plantFlag || harvest)
        {
            var adj = GetNearestAdjacent(TargetGrid);

            if(adj == TargetGrid)
            {
                IsBusy = false;
                OnTaskCycleCompleted?.Invoke();
                return;
            }
            targetPos = adj;
        }  
        else
        {
            targetPos = TargetGrid;
        }

        StopAllCoroutines();
        IsBusy = true;
    }

    private bool IsWallAt(Vector2Int grid)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(
            new Vector2(grid.x, grid.y), 0.4f,
            1 << wallLayerMask
        );
        return cols.Length > 0;
    }

    private bool IsCultivateAt(Vector2Int grid)
    {
        Vector3Int cell = new Vector3Int(grid.x, grid.y, 0);
        return FarmTilemap != null && FarmTilemap.HasTile(cell);
    }

    private Vector2Int GetNearestAdjacent(Vector2Int wallGrid)
    {
        Vector2Int[] dirs =
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        Vector2Int best = wallGrid;
        float bestDist = float.MaxValue;

        foreach (var d in dirs)
        {
            Vector2Int adj = wallGrid + d;

            if (adj.x < bottomLeft.x || adj.x > topRight.x ||
                adj.y < bottomLeft.y || adj.y > topRight.y)
                continue;

            Node node = NodeArray[adj.x - bottomLeft.x, adj.y - bottomLeft.y];
            if (node.isWall) continue;

            float dist = Vector2Int.Distance(robotGrid, adj);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = adj;
            }
        }
        return best;
    }

    public void PathFinding()
    {
        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                var n = NodeArray[i, j];
                n.G = int.MaxValue;   // 큰 값으로 초기화
                n.H = 0;
                n.ParentNode = null;
            }
        }

        if (!InBounds(startPos)) { Debug.LogWarning("Start out of bounds"); IsBusy = false; return; }
        if (!InBounds(targetPos)) { Debug.LogWarning("Target out of bounds"); IsBusy = false; return; }

        // 시작/끝 노드, 리스트 초기화
        StartNode = NodeArray[startPos.x - bottomLeft.x, startPos.y - bottomLeft.y];
        TargetNode = NodeArray[targetPos.x - bottomLeft.x, targetPos.y - bottomLeft.y];

        StartNode.G = 0;
        StartNode.H = (Mathf.Abs(StartNode.x - TargetNode.x) + Mathf.Abs(StartNode.y - TargetNode.y)) * 10;

        OpenList = new List<Node>() { StartNode };
        ClosedList = new List<Node>();
        FinalNodeList = new List<Node>();

        while (OpenList.Count > 0)
        {
            // 열린리스트 중 F가 가장 작고, 같으면 H가 작은 노드 선택
            CurNode = OpenList[0];
            for (int i = 1; i < OpenList.Count; i++)
                if (OpenList[i].F <= CurNode.F && OpenList[i].H < CurNode.H) CurNode = OpenList[i];

            OpenList.Remove(CurNode);
            ClosedList.Add(CurNode);

            // 목적지 도달
            if (CurNode == TargetNode)
            {
                Node TargetCurNode = TargetNode;
                while (TargetCurNode != StartNode)
                {
                    FinalNodeList.Add(TargetCurNode);
                    TargetCurNode = TargetCurNode.ParentNode;
                }
                FinalNodeList.Add(StartNode);
                FinalNodeList.Reverse();

                // 경로 따라 이동 시작
                StartCoroutine(MoveAlongPath());
                return;
            }

            // 대각 이동
            if (allowDiagonal)
            {
                OpenListAdd(CurNode.x + 1, CurNode.y + 1);
                OpenListAdd(CurNode.x - 1, CurNode.y + 1);
                OpenListAdd(CurNode.x - 1, CurNode.y - 1);
                OpenListAdd(CurNode.x + 1, CurNode.y - 1);
            }

            // 상하좌우
            OpenListAdd(CurNode.x, CurNode.y + 1);
            OpenListAdd(CurNode.x + 1, CurNode.y);
            OpenListAdd(CurNode.x, CurNode.y - 1);
            OpenListAdd(CurNode.x - 1, CurNode.y);
        }

        Debug.LogWarning("Path not found");
        IsBusy = false;
        OnTaskCycleCompleted?.Invoke();
    }

    void OpenListAdd(int checkX, int checkY)
    {
        // 범위 내, 벽 아님, 닫힌리스트에 없음
        if (checkX >= bottomLeft.x && checkX < topRight.x + 1 &&
            checkY >= bottomLeft.y && checkY < topRight.y + 1 &&
            !NodeArray[checkX - bottomLeft.x, checkY - bottomLeft.y].isWall &&
            !ClosedList.Contains(NodeArray[checkX - bottomLeft.x, checkY - bottomLeft.y]))
        {
            // 대각선 허용 시, 벽 사이로 통과 금지
            if (allowDiagonal)
                if (NodeArray[CurNode.x - bottomLeft.x, checkY - bottomLeft.y].isWall &&
                    NodeArray[checkX - bottomLeft.x, CurNode.y - bottomLeft.y].isWall)
                    return;

            // 코너 가로질러 금지 옵션
            if (dontCrossCorner)
                if (NodeArray[CurNode.x - bottomLeft.x, checkY - bottomLeft.y].isWall ||
                    NodeArray[checkX - bottomLeft.x, CurNode.y - bottomLeft.y].isWall)
                    return;

            Node NeighborNode = NodeArray[checkX - bottomLeft.x, checkY - bottomLeft.y];
            int MoveCost = CurNode.G + (CurNode.x - checkX == 0 || CurNode.y - checkY == 0 ? 10 : 14);

            if (MoveCost < NeighborNode.G || !OpenList.Contains(NeighborNode))
            {
                NeighborNode.G = MoveCost;
                NeighborNode.H = (Mathf.Abs(NeighborNode.x - TargetNode.x) + Mathf.Abs(NeighborNode.y - TargetNode.y)) * 10;
                NeighborNode.ParentNode = CurNode;

                OpenList.Add(NeighborNode);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (FinalNodeList != null && FinalNodeList.Count != 0)
        {
            for (int i = 0; i < FinalNodeList.Count - 1; i++)
                Gizmos.DrawLine(new Vector2(FinalNodeList[i].x, FinalNodeList[i].y),
                                new Vector2(FinalNodeList[i + 1].x, FinalNodeList[i + 1].y));
        }
    }

    private IEnumerator MoveAlongPath()
    {
        float robotSpeed;

        for (int i = 0; i < FinalNodeList.Count - 1; i++)
        {
            Vector3 nextWorldPos = new Vector3(
                FinalNodeList[i + 1].x,
                FinalNodeList[i + 1].y,
                0f
            );

            // 목표 지점에 거의 도착할 때까지 반복
            while (Vector3.Distance(robotTransform.position, nextWorldPos) > 0.01f)
            {
                onSoft = robotDetect != null && robotDetect.inSoftGround;
                if (onSoft)
                    robotSpeed = moveSpeed * FarmFloorSpeed;
                else
                    robotSpeed = moveSpeed;

                robotTransform.position = Vector3.MoveTowards(
                    robotTransform.position,
                    nextWorldPos,
                    robotSpeed * Time.deltaTime
                );
                yield return null;
            }
        }

        // 도착 후 행동 실행
        if (isDigMode) yield return StartCoroutine(DigWall_GameMinutes(4));
        else if (isCultivate) yield return StartCoroutine(Cultivate_GameMinutes(2));
        else if (isPlant) yield return StartCoroutine(Planting_GameMinutes(1));
        else if (isHarvest) yield return StartCoroutine(HarvestAtTarget(1));

        // 사이클 종료 콜백
        IsBusy = false;
        OnTaskCycleCompleted?.Invoke();
    }

    // === 작업 코루틴 ===

    private IEnumerator DigWall_GameMinutes(int minutes)
    {
        yield return TimeManager.WaitGameMinutes(minutes);

        Vector3Int cellPos = wallTilemap.WorldToCell(new Vector3(TargetGrid.x, TargetGrid.y, 0f));
        wallTilemap.SetTile(cellPos, null);

        // 길 갱신
        NodeArray[TargetGrid.x - bottomLeft.x, TargetGrid.y - bottomLeft.y].isWall = false;

        isDigMode = false;
    }

    private IEnumerator Cultivate_GameMinutes(int minutes)
    {
        yield return TimeManager.WaitGameMinutes(minutes);

        Vector3Int cellPos = FarmTilemap.WorldToCell(new Vector3(TargetGrid.x, TargetGrid.y, 0f));
        FarmTilemap.SetTile(cellPos, softDirt);

        isCultivate = false;
    }

    private IEnumerator Planting_GameMinutes(int minutes)
    {
        // 심는 데 걸리는 게임 시간
        yield return TimeManager.WaitGameMinutes(minutes);

        var cell = new Vector3Int(TargetGrid.x, TargetGrid.y, 0);
        if (plantManager && plantManager.PlantAt(cell, plantData))
            Debug.Log($"{TargetGrid} 심기 완료");
        else
            Debug.Log("심기 실패(경작 아님/이미 심어짐/데이터 없음)");

        isPlant = false;
    }

    private IEnumerator HarvestAtTarget(int minutes)
    {
        yield return TimeManager.WaitGameMinutes(minutes);

        var cell = new Vector3Int(TargetGrid.x, TargetGrid.y, 0);
        if (plantManager && plantManager.HarvestAt(cell, plantData))
            Debug.Log("수확 완료");
        else
            Debug.Log("수확 실패");

        isHarvest = false;
    }
}
