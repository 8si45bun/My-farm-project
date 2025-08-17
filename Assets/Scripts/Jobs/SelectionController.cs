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

    [Header("Input")]
    public KeyCode dragKey = KeyCode.Mouse0;       // 좌클릭 드래그
    public KeyCode commandDig = KeyCode.Alpha1;
    public KeyCode commandCult = KeyCode.Alpha2;
    public KeyCode commandPlant = KeyCode.Alpha3;
    public KeyCode commandHarvest = KeyCode.Alpha4;
    public KeyCode commandMove = KeyCode.Alpha5;

    public CommandType current = CommandType.Dig;
    private bool dragging;
    private Vector3Int startCell, endCell;

    private TileRules rules;

    private void Awake()
    {
        rules = new TileRules(wallTilemap, farmTilemap, plantManager);
    }

    private void Update()
    {
        // 간단한 커맨드 전환(임시 입력)
        if (Input.GetKeyDown(commandDig)) current = CommandType.Dig;
        if (Input.GetKeyDown(commandCult)) current = CommandType.Cultivate;
        if (Input.GetKeyDown(commandPlant)) current = CommandType.Plant;
        if (Input.GetKeyDown(commandHarvest)) current = CommandType.Harvest;
        if (Input.GetKeyDown(commandMove)) current = CommandType.Move;

        if (Input.GetKeyDown(dragKey))
        {
            dragging = true;
            startCell = ScreenToCell(Input.mousePosition);
        }
        else if (dragging && Input.GetKey(dragKey))
        {
            endCell = ScreenToCell(Input.mousePosition);
            // TODO: 선택 영역 하이라이트 미리보기
        }
        else if (dragging && Input.GetKeyUp(dragKey))
        {
            dragging = false;
            endCell = ScreenToCell(Input.mousePosition);
            ApplySelection();
        }
    }

    private Vector3Int ScreenToCell(Vector3 screen)
    {
        var world = Camera.main.ScreenToWorldPoint(screen);
        return Vector3Int.RoundToInt(world);
    }

    private void ApplySelection()
    {
        var rect = GetRect(startCell, endCell);
        var tiles = new List<Vector3Int>();
        for (int y = rect.yMin; y <= rect.yMax; y++)
            for (int x = rect.xMin; x <= rect.xMax; x++)
                tiles.Add(new Vector3Int(x, y, 0));

        var valid = FilterByCommand(tiles, current);
        var jobs = new List<Job>();
        foreach (var c in valid)
        {
            // 중복 예약 방지: 여기서는 큐에 넣기 전에 예약 시도까지 하지 않음(디스패처에서 처리)
            jobs.Add(new Job
            {
                type = current,
                cell = c,
                floorId = 0,
                plantData = (current == CommandType.Plant) ? defaultPlantData : null
            });
        }
        if (jobs.Count > 0) JobDispatcher.EnqueueMany(jobs);
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
                _ => false
            };
            if (ok) result.Add(c);
        }
        return result;
    }

    private RectInt GetRect(Vector3Int a, Vector3Int b)
    {
        int xMin = Mathf.Min(a.x, b.x);
        int yMin = Mathf.Min(a.y, b.y);
        int xMax = Mathf.Max(a.x, b.x);
        int yMax = Mathf.Max(a.y, b.y);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }
}
