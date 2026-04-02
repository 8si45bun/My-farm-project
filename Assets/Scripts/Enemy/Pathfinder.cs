using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적용 2-phase A* 경로 탐색 유틸리티.
/// 1차: 벽 차단, 2차: 벽 통과 가능 (높은 비용)
/// </summary>
public static class Pathfinder
{
    private class PNode
    {
        public bool isWall;
        public int x, y, G, H;
        public int F => G + H;
        public PNode parent;

        public PNode(bool wall, int x, int y)
        {
            isWall = wall;
            this.x = x;
            this.y = y;
        }
    }

    /// <summary>
    /// 2-phase 경로 탐색: 먼저 벽 차단으로 시도, 실패 시 벽 통과(고비용)
    /// </summary>
    public static List<Vector2Int> FindPathTwoPhase(
        Vector2Int start, Vector2Int target,
        Vector2Int bottomLeft, Vector2Int topRight,
        int wallCost = 50)
    {
        // 1차: 벽 차단
        var path = FindPath(start, target, bottomLeft, topRight, false, 0);
        if (path != null && path.Count > 0) return path;

        // 2차: 벽 통과 가능 (고비용)
        return FindPath(start, target, bottomLeft, topRight, true, wallCost);
    }

    /// <summary>
    /// A* 경로 탐색
    /// </summary>
    public static List<Vector2Int> FindPath(
        Vector2Int start, Vector2Int target,
        Vector2Int bottomLeft, Vector2Int topRight,
        bool wallsPassable, int wallCost)
    {
        int sizeX = topRight.x - bottomLeft.x + 1;
        int sizeY = topRight.y - bottomLeft.y + 1;

        int wallLayer = LayerMask.NameToLayer("Wall");

        // 그리드 생성
        PNode[,] grid = new PNode[sizeX, sizeY];
        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                bool isWall = false;
                foreach (var col in Physics2D.OverlapCircleAll(
                    new Vector2(i + bottomLeft.x, j + bottomLeft.y), 0.4f))
                {
                    if (col.gameObject.layer == wallLayer)
                        isWall = true;
                }
                grid[i, j] = new PNode(isWall, i + bottomLeft.x, j + bottomLeft.y);
            }
        }

        // 범위 확인
        if (!InBounds(start, bottomLeft, topRight) || !InBounds(target, bottomLeft, topRight))
            return null;

        PNode startNode = grid[start.x - bottomLeft.x, start.y - bottomLeft.y];
        PNode targetNode = grid[target.x - bottomLeft.x, target.y - bottomLeft.y];

        startNode.G = 0;
        startNode.H = Heuristic(startNode, targetNode);

        var openList = new List<PNode> { startNode };
        var closedSet = new HashSet<PNode>();

        while (openList.Count > 0)
        {
            // F값이 가장 작은 노드 선택
            PNode current = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].F < current.F ||
                    (openList[i].F == current.F && openList[i].H < current.H))
                    current = openList[i];
            }

            openList.Remove(current);
            closedSet.Add(current);

            if (current == targetNode)
                return ReconstructPath(current);

            // 4방향 이웃
            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };

            for (int d = 0; d < 4; d++)
            {
                int nx = current.x + dx[d];
                int ny = current.y + dy[d];

                if (!InBounds(new Vector2Int(nx, ny), bottomLeft, topRight)) continue;

                PNode neighbor = grid[nx - bottomLeft.x, ny - bottomLeft.y];
                if (closedSet.Contains(neighbor)) continue;

                // 벽 처리
                if (neighbor.isWall && !wallsPassable) continue;

                int moveCost = 10;
                if (neighbor.isWall && wallsPassable) moveCost += wallCost;

                int newG = current.G + moveCost;

                if (newG < neighbor.G || !openList.Contains(neighbor))
                {
                    neighbor.G = newG;
                    neighbor.H = Heuristic(neighbor, targetNode);
                    neighbor.parent = current;

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }

        return null; // 경로 없음
    }

    private static int Heuristic(PNode a, PNode b)
    {
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y)) * 10;
    }

    private static List<Vector2Int> ReconstructPath(PNode end)
    {
        var path = new List<Vector2Int>();
        PNode current = end;
        while (current != null)
        {
            path.Add(new Vector2Int(current.x, current.y));
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    private static bool InBounds(Vector2Int pos, Vector2Int bottomLeft, Vector2Int topRight)
    {
        return pos.x >= bottomLeft.x && pos.x <= topRight.x &&
               pos.y >= bottomLeft.y && pos.y <= topRight.y;
    }

    /// <summary>
    /// 특정 위치가 벽인지 확인
    /// </summary>
    public static bool IsWallAt(Vector2Int pos)
    {
        int wallLayer = LayerMask.NameToLayer("Wall");
        var cols = Physics2D.OverlapCircleAll(new Vector2(pos.x, pos.y), 0.4f);
        foreach (var col in cols)
        {
            if (col.gameObject.layer == wallLayer)
                return true;
        }
        return false;
    }
}
