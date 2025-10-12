using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SelectionController : MonoBehaviour
{
    [Header("참조")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;
    public Tilemap farmTilemap;
    public PlantManager plantManager;

    [Header("오버레이")]
    public Tilemap overlayTilemap;
    public TileBase overlayTile;

    [Header("드래그")]
    [SerializeField] private LineRenderer previewLine;
    private Vector3 startWorld, endWorld;

    [Header("색깔")]
    public Color digColor = new Color(1f, 0.55f, 0.1f, 0.35f);   // 주황
    public Color cultivateColor = new Color(0.2f, 0.8f, 0.3f, 0.35f); // 초록
    public Color plantColor = new Color(0.2f, 0.6f, 1f, 0.35f); // 파랑
    public Color harvestColor = new Color(0.8f, 0.2f, 0.9f, 0.35f); // 보라
    public Color moveColor = new Color(0.8f, 0.8f, 0.8f, 0.25f);
    public Color haulColor = new Color(1f, 0.9f, 0.2f, 0.35f); // 노랑
    public Color NullColor = new Color(0f, 0f, 0f, 0f);

    [Header("입력키")]
    public KeyCode dragKey = KeyCode.Mouse0;
    public KeyCode commandDefault = KeyCode.Alpha1;
    public KeyCode commandDig = KeyCode.Alpha2;
    public KeyCode commandCult = KeyCode.Alpha3;
    public KeyCode commandPlant = KeyCode.Alpha4;
    public KeyCode commandHarvest = KeyCode.Alpha5;
    public KeyCode commandHaul = KeyCode.Alpha6;

    [Header("식물")]
    public PlantData[] plantCatalog;
    public int plantIndex = 0;

    public CommandType current = CommandType.Default;

    private bool dragging;
    private Vector3Int startCell, endCell;
    private TileRules rules;

    private Grid gridForCell;

    private readonly List<Vector3Int> previewBuf = new();
    private readonly HashSet<Vector3Int> confirmedCells = new();

    private void Awake()
    {
        rules = new TileRules(floorTilemap, wallTilemap, farmTilemap, plantManager);
        gridForCell = overlayTilemap.layoutGrid;
    }

    private void OnEnable()
    {
        JobDispatcher.OnJobCompleted += HandleJobCompleted;
    }

    private void OnDisable()
    {
        JobDispatcher.OnJobCompleted -= HandleJobCompleted;
        HidePreviewLine();
        ClearAllConfirmed();
    }

    private void Update()
    {
        if (Input.GetKeyDown(commandDefault)) current = CommandType.Default;
        if (Input.GetKeyDown(commandDig)) current = CommandType.Dig;
        if (Input.GetKeyDown(commandCult)) current = CommandType.Cultivate;
        if (Input.GetKeyDown(commandPlant)) current = CommandType.Plant;
        if (Input.GetKeyDown(commandHarvest)) current = CommandType.Harvest;
        if (Input.GetKeyDown(commandHaul)) current = CommandType.Haul;

        if (Input.GetKeyDown(dragKey))
        {
            dragging = true;
            startWorld = ScreenToWorld2D(Input.mousePosition);
            endWorld = startWorld;

            startCell = gridForCell.WorldToCell(startWorld);
            endCell = startCell;

            UpdatePreviewLine(startWorld, endWorld);
        }
        else if (dragging && Input.GetKey(dragKey))
        {
            var now = ScreenToWorld2D(Input.mousePosition);
            if (now != endWorld)
            {
                endWorld = now;
                endCell = gridForCell.WorldToCell(endWorld);
                UpdatePreviewLine(startWorld, endWorld);
            }
        }
        else if (dragging && Input.GetKeyUp(dragKey))
        {
            dragging = false;
            endWorld = ScreenToWorld2D(Input.mousePosition);
            endCell = gridForCell.WorldToCell(endWorld);

            HidePreviewLine();
            ApplySelection();
        }

    }

    private Vector3 ScreenToWorld2D(Vector3 screen)
    {
        var world = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0.5f));
        world.z = 0;
        return world;
    }

    // 드래그 프리뷰 
    private void UpdatePreviewLine(Vector3 a, Vector3 b)
    {
        float xMin = Mathf.Min(a.x, b.x);
        float xMax = Mathf.Max(a.x, b.x);
        float yMin = Mathf.Min(a.y, b.y);
        float yMax = Mathf.Max(a.y, b.y);

        Vector3 bl = new Vector3(xMin, yMin, 0);
        Vector3 tl = new Vector3(xMin, yMax, 0);
        Vector3 tr = new Vector3(xMax, yMax, 0);
        Vector3 br = new Vector3(xMax, yMin, 0);

        previewLine.positionCount = 4;
        previewLine.SetPosition(0, bl);
        previewLine.SetPosition(1, tl);
        previewLine.SetPosition(2, tr);
        previewLine.SetPosition(3, br);
        previewLine.enabled = true;
    }

    private void HidePreviewLine()
    {
        previewLine.enabled = false;
    }

    // 확정(명령 등록) 
    private void ApplySelection()
    {
        var rect = GetRect(startCell, endCell);

        var cells = new List<Vector3Int>();
        for (int y = rect.yMin; y < rect.yMax; y++)
            for (int x = rect.xMin; x < rect.xMax; x++)
                cells.Add(new Vector3Int(x, y, 0));

        var valid = new List<Vector3Int>();
        var jobs = new List<Job>();

        if (current == CommandType.Haul)
        {
            foreach (var c in cells)
            {
                Vector3 worldPos = floorTilemap.CellToWorld(c) + new Vector3(0.5f, 0.5f);
                int itemMask = LayerMask.GetMask("Item");
                Collider2D col = Physics2D.OverlapCircle(worldPos, 0.3f, itemMask);
                if (col == null) continue;

                var item = col.GetComponent<DroppedItem>();
                if (item == null)
                {
                    RemoveConfirmed(c);
                    continue;
                }

                var storage = StorageBox.FindClosest(worldPos);
                if (storage == null)
                {
                    Debug.LogWarning("Haul 명령: StorageBox를 찾을 수 없습니다!");
                    continue;
                }

                valid.Add(c); 

                jobs.Add(new Job
                {
                    type = CommandType.Haul,
                    cell = c,
                    fromItem = item,
                    toStorage = storage
                });
            }
        }
        else
        {
            var filtered = FilterByCommand(cells, current);
            foreach (var c in filtered)
            {
                jobs.Add(new Job
                {
                    type = current,
                    cell = c,
                    plantData = (current == CommandType.Plant) ? plantCatalog[plantIndex] : null
                });
            }
            valid.AddRange(filtered);
        }

        if (jobs.Count > 0)
        {
            JobDispatcher.EnqueueMany(jobs);
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
                CommandType.Haul => true,
                _ => false
            };
            if (ok) result.Add(c);
        }
        return result;
    }

    private void PaintConfirmed(List<Vector3Int> cells, CommandType cmd)
    {
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
            _ => NullColor
        };
    }

    // 작업 완료 시 해당 타일만 제거 
    private void HandleJobCompleted(Job job, bool success)
    {
        RemoveConfirmed(job.cell);
    }

    private void RemoveConfirmed(Vector3Int cell)
    {
        if (!confirmedCells.Contains(cell)) return;

        overlayTilemap.SetTile(cell, null);
        confirmedCells.Remove(cell);
    }

    private void ClearAllConfirmed()
    {
        foreach (var c in confirmedCells) overlayTilemap.SetTile(c, null);
        confirmedCells.Clear();
    }

    private RectInt GetRect(Vector3Int a, Vector3Int b)
    {
        int xMin = Mathf.Min(a.x, b.x);
        int yMin = Mathf.Min(a.y, b.y);
        int xMax = Mathf.Max(a.x, b.x) + 1;
        int yMax = Mathf.Max(a.y, b.y) + 1;
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }
}
