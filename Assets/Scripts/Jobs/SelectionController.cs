// SelectionController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SelectionController : MonoBehaviour
{
    [Header("Refs")]
    public Tilemap wallTilemap;
    public Tilemap farmTilemap;
    public PlantManager plantManager;
    public PlantData defaultPlantData;

    // ★ 오버레이: 드래그 미리보기 / 확정 표시 모두 이 Tilemap에 그림
    [Header("Overlay (Preview / Confirm)")]
    public Tilemap overlayTilemap;   // 씬에 빈 Tilemap 하나 만들고 연결
    public TileBase overlayTile;     // 투명 사각형 1x1 같은 단색 Tile

    [Range(0, 1)] public float previewAlpha = 0.20f;

    // ★ 명령별 색상(원하는 색으로 변경하세요)
    [Header("Confirm Colors per Command")]
    public Color digColor = new Color(1f, 0.55f, 0.1f, 0.35f);   // 주황
    public Color cultivateColor = new Color(0.2f, 0.8f, 0.3f, 0.35f); // 초록
    public Color plantColor = new Color(0.2f, 0.6f, 1f, 0.35f); // 파랑
    public Color harvestColor = new Color(0.8f, 0.2f, 0.9f, 0.35f); // 보라
    public Color moveColor = new Color(0.8f, 0.8f, 0.8f, 0.25f); // 회색
    public Color haulColor = new Color(1f, 0.9f, 0.2f, 0.35f); // 노랑(하울링)

    [Header("Input")]
    public KeyCode dragKey = KeyCode.Mouse0;
    public KeyCode commandDig = KeyCode.Alpha1;
    public KeyCode commandCult = KeyCode.Alpha2;
    public KeyCode commandPlant = KeyCode.Alpha3;
    public KeyCode commandHarvest = KeyCode.Alpha4;
    public KeyCode commandMove = KeyCode.Alpha5;

    public CommandType current = CommandType.Dig;

    private bool dragging;
    private Vector3Int startCell, endCell;
    private TileRules rules;

    // ★ 프리뷰/확정 좌표 버퍼(지울 때 씀)
    private readonly List<Vector3Int> previewBuf = new();
    // 확정은 “명령 완료 시 해당 좌표만” 지울 수 있도록 Set 저장
    private readonly HashSet<Vector3Int> confirmedCells = new();

    private void Awake()
    {
        rules = new TileRules(wallTilemap, farmTilemap, plantManager);
    }

    private void OnEnable()
    {
        // ★ 작업 완료 이벤트 구독: 해당 타일의 확정 표시 제거
        JobDispatcher.OnJobCompleted += HandleJobCompleted;
    }

    private void OnDisable()
    {
        JobDispatcher.OnJobCompleted -= HandleJobCompleted;
        ClearPreview();
        ClearAllConfirmed();
    }

    private void Update()
    {
        if (Input.GetKeyDown(commandDig)) current = CommandType.Dig;
        if (Input.GetKeyDown(commandCult)) current = CommandType.Cultivate;
        if (Input.GetKeyDown(commandPlant)) current = CommandType.Plant;
        if (Input.GetKeyDown(commandHarvest)) current = CommandType.Harvest;
        if (Input.GetKeyDown(commandMove)) current = CommandType.Move;

        if (Input.GetKeyDown(dragKey))
        {
            dragging = true;
            startCell = ScreenToCell(Input.mousePosition);
            endCell = startCell;
            UpdatePreview(startCell, endCell);
        }
        else if (dragging && Input.GetKey(dragKey))
        {
            var now = ScreenToCell(Input.mousePosition);
            if (now != endCell)
            {
                endCell = now;
                UpdatePreview(startCell, endCell);
            }
        }
        else if (dragging && Input.GetKeyUp(dragKey))
        {
            dragging = false;
            endCell = ScreenToCell(Input.mousePosition);

            // ★ 먼저 프리뷰 지우고
            ClearPreview();
            // ★ 그 다음 실제 선택 적용(= 확정 타일 칠하기)
            ApplySelection();
        }
    }

    private Vector3Int ScreenToCell(Vector3 screen)
    {
        var world = Camera.main.ScreenToWorldPoint(screen);
        return Vector3Int.RoundToInt(world);
    }

    // === 드래그 프리뷰 ===
    private void UpdatePreview(Vector3Int a, Vector3Int b)
    {
        if (overlayTilemap == null || overlayTile == null) return;

        ClearPreview();

        var rect = GetRect(a, b);
        for (int y = rect.yMin; y < rect.yMax; y++)
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                var c = new Vector3Int(x, y, 0);
                overlayTilemap.SetTile(c, overlayTile);
                overlayTilemap.SetTileFlags(c, TileFlags.None);

                var col = Color.cyan; col.a = previewAlpha;
                overlayTilemap.SetColor(c, col);

                previewBuf.Add(c);
            }
    }

    private void ClearPreview()
    {
        if (overlayTilemap == null) { previewBuf.Clear(); return; }
        foreach (var c in previewBuf)
        {
            // 프리뷰에만 칠했던 좌표를 지움(확정은 confirmedCells로 관리)
            if (!confirmedCells.Contains(c) && overlayTilemap.GetTile(c) == overlayTile)
                overlayTilemap.SetTile(c, null);
        }
        previewBuf.Clear();
    }

    // === 확정(명령 등록) ===
    private void ApplySelection()
    {
        var rect = GetRect(startCell, endCell);

        // RectInt의 xMax/yMax는 배타 경계 → '<' 사용
        var cells = new List<Vector3Int>();
        for (int y = rect.yMin; y < rect.yMax; y++)
            for (int x = rect.xMin; x < rect.xMax; x++)
                cells.Add(new Vector3Int(x, y, 0));

        var valid = FilterByCommand(cells, current);

        // 큐잉
        var jobs = new List<Job>();
        foreach (var c in valid)
        {
            jobs.Add(new Job
            {
                type = current,
                cell = c,
                floorId = 0,
                plantData = (current == CommandType.Plant) ? defaultPlantData : null
            });
        }
        if (jobs.Count > 0)
        {
            JobDispatcher.EnqueueMany(jobs);
            // ★ 명령 타입별 색으로 확정 칠하기
            PaintConfirmed(valid, current);
        }
    }

    private List<Vector3Int> FilterByCommand(List<Vector3Int> cells, CommandType cmd)
    {
        var result = new List<Vector3Int>();
        foreach (var c in cells)
        {
            bool ok = cmd switch
            {
                CommandType.Dig => rules.CanDig(c),
                CommandType.Cultivate => rules.CanCultivate(c),
                CommandType.Plant => rules.CanPlant(c),
                CommandType.Harvest => rules.CanHarvest(c),
                CommandType.Move => rules.CanMove(c),
                CommandType.Haul => true, // 수동 하울이 있다면 필요에 맞게 조건화
                _ => false
            };
            if (ok) result.Add(c);
        }
        return result;
    }

    private void PaintConfirmed(List<Vector3Int> cells, CommandType cmd)
    {
        if (overlayTilemap == null || overlayTile == null) return;

        var color = GetColorFor(cmd);

        foreach (var c in cells)
        {
            overlayTilemap.SetTile(c, overlayTile);
            overlayTilemap.SetTileFlags(c, TileFlags.None);
            overlayTilemap.SetColor(c, color);
            confirmedCells.Add(c);
        }
    }

    private Color GetColorFor(CommandType cmd)
    {
        return cmd switch
        {
            CommandType.Dig => digColor,
            CommandType.Cultivate => cultivateColor,
            CommandType.Plant => plantColor,
            CommandType.Harvest => harvestColor,
            CommandType.Move => moveColor,
            CommandType.Haul => haulColor,
            _ => moveColor
        };
    }

    // === 작업 완료 시 해당 타일만 제거 ===
    private void HandleJobCompleted(Job job, bool success)
    {
        if (overlayTilemap == null) return;
        // 완료든 실패든 표시를 지우고 싶다면 success 무시하고 지워도 됨
        RemoveConfirmed(job.cell);
    }

    private void RemoveConfirmed(Vector3Int cell)
    {
        if (!confirmedCells.Contains(cell)) return;

        // 동일 좌표에 다른 명령이 추가로 덮여있을 수 있다면,
        // 여기서 “남은 잡이 있는지”를 체크해 남아있게 하는 방식도 가능.
        overlayTilemap.SetTile(cell, null);
        confirmedCells.Remove(cell);
    }

    private void ClearAllConfirmed()
    {
        if (overlayTilemap == null) { confirmedCells.Clear(); return; }
        foreach (var c in confirmedCells) overlayTilemap.SetTile(c, null);
        confirmedCells.Clear();
    }

    // 좌상/우하 무관 Rect (배타 경계가 되도록 +1)
    private RectInt GetRect(Vector3Int a, Vector3Int b)
    {
        int xMin = Mathf.Min(a.x, b.x);
        int yMin = Mathf.Min(a.y, b.y);
        int xMax = Mathf.Max(a.x, b.x) + 1; // 배타 경계
        int yMax = Mathf.Max(a.y, b.y) + 1; // 배타 경계
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }
}
