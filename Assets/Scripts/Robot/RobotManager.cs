using Mono.Cecil;
using NUnit.Framework.Constraints;
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

    [Header("Moving")]
    public Vector2Int bottomLeft, topRight;
    private Vector2Int startPos, targetPos;
    public List<Node> FinalNodeList;
    public bool allowDiagonal, dontCrossCorner;
    public Rigidbody2D robotRigidbody;
    public Transform robotTransform;
    public float moveSpeed = 2f;
    public float FarmFloorSpeed = 0.5f;

    [Header("Harvest")]
    public PlantManager plantManager;
    public PlantData plantData;
    private bool isHarvest = false;

    [Header("Action")]
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

    private void Awake()
    {
        wallLayerMask = LayerMask.NameToLayer("Wall");
        softLayerMask = LayerMask.NameToLayer("SoftGround");

        InitGrid();
    }

    int sizeX, sizeY;
    Node[,] NodeArray;
    Node StartNode, TargetNode, CurNode;
    List<Node> OpenList, ClosedList;

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
                    if (col.gameObject.layer == LayerMask.NameToLayer("Wall")) isWall = true;

                NodeArray[i, j] = new Node(isWall, i + bottomLeft.x, j + bottomLeft.y);
            }
        }
    }

    private bool InBounds(Vector2Int g)  // 왜? 클릭/목표가 Grid 밖이면 IndexOutOfRange 뜸
    {
        return g.x >= bottomLeft.x && g.x <= topRight.x &&
               g.y >= bottomLeft.y && g.y <= topRight.y;
    }

    private void Update()
    {
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
            else if(Input.GetKey(KeyCode.T) && IsCultivateAt(clickGrid))
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
            PathFinding();

        }
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

            Node node = NodeArray[adj.x -  bottomLeft.x, adj.y - bottomLeft.y];
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

        if (!InBounds(startPos)) { Debug.LogWarning("Start out of bounds"); return; }
        if (!InBounds(targetPos)) { Debug.LogWarning("Target out of bounds"); return; }

        // 시작과 끝 노드, 열린리스트와 닫힌리스트, 마지막리스트 초기화
        StartNode = NodeArray[startPos.x - bottomLeft.x, startPos.y - bottomLeft.y];
        TargetNode = NodeArray[targetPos.x - bottomLeft.x, targetPos.y - bottomLeft.y];

        StartNode.G = 0;
        StartNode.H = (Mathf.Abs(StartNode.x - TargetNode.x) + Mathf.Abs(StartNode.y - TargetNode.y)) * 10;

        OpenList = new List<Node>() { StartNode };
        ClosedList = new List<Node>();
        FinalNodeList = new List<Node>();

        while (OpenList.Count > 0)
        {
            // 열린리스트 중 가장 F가 작고 F가 같다면 H가 작은 걸 현재노드로 하고 열린리스트에서 닫힌리스트로 옮기기
            CurNode = OpenList[0];
            for (int i = 1; i < OpenList.Count; i++)
                if (OpenList[i].F <= CurNode.F && OpenList[i].H < CurNode.H) CurNode = OpenList[i];

            OpenList.Remove(CurNode);
            ClosedList.Add(CurNode);


            // 마지막
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

                //for (int i = 0; i < FinalNodeList.Count; i++)
                //{
                //    print(i + "번째는 " + FinalNodeList[i].x + ", " + FinalNodeList[i].y);
                //}

                StartCoroutine(MoveAlongPath());

                return;
            }


            // ↗↖↙↘
            if (allowDiagonal)
            {
                OpenListAdd(CurNode.x + 1, CurNode.y + 1);
                OpenListAdd(CurNode.x - 1, CurNode.y + 1);
                OpenListAdd(CurNode.x - 1, CurNode.y - 1);
                OpenListAdd(CurNode.x + 1, CurNode.y - 1);
            }

            // ↑ → ↓ ←
            OpenListAdd(CurNode.x, CurNode.y + 1);
            OpenListAdd(CurNode.x + 1, CurNode.y);
            OpenListAdd(CurNode.x, CurNode.y - 1);
            OpenListAdd(CurNode.x - 1, CurNode.y);
        }
    }

    void OpenListAdd(int checkX, int checkY)
    {
        // 상하좌우 범위를 벗어나지 않고, 벽이 아니면서, 닫힌리스트에 없다면
        if (checkX >= bottomLeft.x && checkX < topRight.x + 1 && checkY >= bottomLeft.y && checkY < topRight.y + 1 && !NodeArray[checkX - bottomLeft.x, checkY - bottomLeft.y].isWall && !ClosedList.Contains(NodeArray[checkX - bottomLeft.x, checkY - bottomLeft.y]))
        {
            // 대각선 허용시, 벽 사이로 통과 안됨
            if (allowDiagonal) if (NodeArray[CurNode.x - bottomLeft.x, checkY - bottomLeft.y].isWall && NodeArray[checkX - bottomLeft.x, CurNode.y - bottomLeft.y].isWall) return;

            // 코너를 가로질러 가지 않을시, 이동 중에 수직수평 장애물이 있으면 안됨
            if (dontCrossCorner) if (NodeArray[CurNode.x - bottomLeft.x, checkY - bottomLeft.y].isWall || NodeArray[checkX - bottomLeft.x, CurNode.y - bottomLeft.y].isWall) return;


            // 이웃노드에 넣고, 직선은 10, 대각선은 14비용
            Node NeighborNode = NodeArray[checkX - bottomLeft.x, checkY - bottomLeft.y];
            int MoveCost = CurNode.G + (CurNode.x - checkX == 0 || CurNode.y - checkY == 0 ? 10 : 14);


            // 이동비용이 이웃노드G보다 작거나 또는 열린리스트에 이웃노드가 없다면 G, H, ParentNode를 설정 후 열린리스트에 추가
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
        if (FinalNodeList.Count != 0) for (int i = 0; i < FinalNodeList.Count - 1; i++)
                Gizmos.DrawLine(new Vector2(FinalNodeList[i].x, FinalNodeList[i].y), new Vector2(FinalNodeList[i + 1].x, FinalNodeList[i + 1].y));
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
                onSoft = robotDetect.inSoftGround;
                if (onSoft)
                    robotSpeed = moveSpeed * FarmFloorSpeed;
                else
                    robotSpeed = moveSpeed;

                robotTransform.position = (Vector3.MoveTowards(
                    robotTransform.position,
                    nextWorldPos,
                    robotSpeed * Time.deltaTime
                    )
                );
                yield return null;
            }
        }
        if (isDigMode) yield return StartCoroutine(DigWall_GameMinutes(4));
        else if (isCultivate) yield return StartCoroutine(Cultivate_GameMinutes(2));
        else if (isPlant) yield return StartCoroutine(Planting_GameMinutes(1));
        else if (isHarvest) yield return StartCoroutine(HarvestAtTarget(1));
    }
    /*
    private IEnumerator DigWall()
    {
        // (선택) 파괴 애니메이션 / 이펙트 재생 가능
        Debug.Log($"{TargetGrid} 벽을 2초간 파괴합니다.");

        yield return new WaitForSeconds(2f);

        Vector3 worldPos = new Vector3(TargetGrid.x, TargetGrid.y, 0f);
        Vector3Int cellPos = wallTilemap.WorldToCell(worldPos);

        wallTilemap.SetTile(cellPos, null);

        NodeArray[
            TargetGrid.x - bottomLeft.x,
            TargetGrid.y - bottomLeft.y
            ].isWall = false;

        isDigMode = false;
    }

    private IEnumerator Cultivate()
    {
        Debug.Log($"{TargetGrid} 땅을 경작 합니다.");

        yield return new WaitForSeconds(1f);

        Vector3 worldPos = new Vector3(TargetGrid.x, TargetGrid.y, 0f);
        Vector3Int cellPos = FarmTilemap.WorldToCell(worldPos);

        FarmTilemap.SetTile(cellPos, softDirt);

    }
    */

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
        // 심는 데 걸리는 게임 시간 (원하면 0으로)
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
            Debug.Log($"수확 완료: {plantData.plantID}");
        else
            Debug.Log("수확 실패(미성숙/식물 없음)");

        isHarvest = false;
    }


}