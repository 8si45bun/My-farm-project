using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

// 1. �ǹ� ������ ���� ������ Ŭ���� (�ν����Ϳ��� ����)
[System.Serializable]
public class BuildingData
{
    public string buildingName;  // �ǹ� �̸�
    public GameObject prefab;    // ������
    //public Sprite icon;          // UI ������

    [Header("Costs")]
    public int woodCost = 0;
    public int firebloomCost = 0;
    public int steelCost = 0;

    [Header("Settings")]
    public int buildMinutes = 10; // �Ǽ� �ҿ� �ð�
}

public class BuildManager : MonoBehaviour 
{
    public static BuildManager Instance; 

    [Header("References")]
    public StorageBox storageBox;
    public TextManager textManager;

    [Header("Wall Repair")]
    public Tilemap wallTilemap;
    public TileBase wallTile;

    [Header("Buildings List")]
    public List<BuildingData> buildingList = new List<BuildingData>();

    // ���� ���õ� �ǹ� ������ (������ null)
    private BuildingData currentBuilding;

    [HideInInspector]
    public Vector2Int mouseGridPos;
    private Vector2Int lastGrid = new Vector2Int(int.MaxValue, int.MinValue);
    private GameObject previewInstance;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // ���콺 ��ġ ���
        mouseGridPos = Vector2Int.CeilToInt((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition));

        // 1. ���� ���� �ƴϸ� ���� (currentBuilding�� null�̸� �� ���� ����)
        if (currentBuilding == null) return;

        // 2. ������ ���� (���� ����)
        if (previewInstance == null)
        {
            previewInstance = Instantiate(currentBuilding.prefab);

            var thing = previewInstance.GetComponent<Thing>();
            if (thing == null) thing = previewInstance.AddComponent<Thing>();

            // �����Ϳ� �ִ� �̸� ���
            thing.Init(currentBuilding.buildingName, BuildStage.BulePrint);
        }

        // 3. ������ ��ġ ����
        if (mouseGridPos != lastGrid && previewInstance != null)
        {
            previewInstance.transform.position = new Vector3Int(mouseGridPos.x, mouseGridPos.y, 0);
            lastGrid = mouseGridPos;
        }

        // 4. Ŭ���ؼ� �Ǽ� (��Ŭ��)
        if (Input.GetMouseButtonDown(0))
        {
            // ��� Ȯ�� (���ڷ� ���� �ǹ� �����͸� �ѱ�)
            if (CanBuild(currentBuilding, out string msg))
            {
                PayBuild(currentBuilding);

                // Thing ����
                var thing = previewInstance.GetComponentInChildren<Thing>(); // Ȥ�� GetComponent
                if (thing == null) thing = previewInstance.GetComponent<Thing>();
                thing.Setstage(BuildStage.BulePrint); // ö�� ���� (BluePrint)

                Vector3Int cell = Vector3Int.RoundToInt(previewInstance.transform.position);

                // Job ���
                JobDispatcher.Enqueue(new Job
                {
                    type = CommandType.Build,
                    cell = cell,
                    targetThing = thing,
                    buildMinutes = currentBuilding.buildMinutes
                });

                // ������(previewInstance)�� ���� "��¥ �ǹ�"�� �Ǿ����� �ı����� ����
                // �Ŵ����� �տ����� �����ݴϴ�.
                previewInstance = null;
                currentBuilding = null; // ���� ����

                // ���� ���� �Ǽ��� �ϰ� ������ �� �ٵ� ����� �˴ϴ�.
                // ����
            }
            else
            {
                textManager.showText(msg);
            }
        }
        // 5. ��� (��Ŭ��)
        else if (Input.GetMouseButtonDown(1))
        {
            CancelBuildMode(); // ��Ŭ���� ��Ҵϱ� �ı�(Destroy)�ϴ� �� ����
        }
    }

    // �Ǽ� ��� ��� �� �ʱ�ȭ
    private void CancelBuildMode()
    {
        if (previewInstance != null) Destroy(previewInstance);
        previewInstance = null;
        currentBuilding = null; // ���� ����
    }

    public void SelectBuilding(int index)
    {
        if (index >= 0 && index < buildingList.Count)
        {
            // ���� �����䰡 �ִٸ� ����
            if (previewInstance != null) Destroy(previewInstance);

            // ����Ʈ���� �ش� ��ȣ�� �ǹ� ������ ������
            currentBuilding = buildingList[index];
            Debug.Log($"{currentBuilding.buildingName} ���õ�");
        }
        else
        {
            Debug.LogError("�߸��� �ǹ� �ε����Դϴ�.");
        }
    }

    // ��� Ȯ�� ���� (������ Ŭ���� ������� ����)
    private bool CanBuild(BuildingData data, out string lackMessage)
    {
        int haveWood = storageBox.GetCount(ItemType.Wood);
        int haveFirebloom = storageBox.GetCount(ItemType.Firebloom);
        int haveSteel = storageBox.GetCount(ItemType.Steel);

        int lackWood = Mathf.Max(0, data.woodCost - haveWood);
        int lackFirebloom = Mathf.Max(0, data.firebloomCost - haveFirebloom);
        int lackSteel = Mathf.Max(0, data.steelCost - haveSteel);

        if (lackWood == 0 && lackFirebloom == 0 && lackSteel == 0)
        {
            lackMessage = "";
            return true;
        }

        var sb = new StringBuilder("��ᰡ �����մϴ�: ");
        bool first = true;

        if (lackWood > 0)
        {
            sb.Append($"Wood {lackWood}��");
            first = false;
        }
        if (lackFirebloom > 0)
        {
            if (!first) sb.Append(", ");
            sb.Append($"Firebloom {lackFirebloom}개");
            first = false;
        }
        if (lackSteel > 0)
        {
            if (!first) sb.Append(", ");
            sb.Append($"Steel {lackSteel}개");
        }

        lackMessage = sb.ToString();
        return false;
    }

    // ��� ���� ����
    private void PayBuild(BuildingData data)
    {
        if (data.woodCost > 0)
            storageBox.TakeItem(ItemType.Wood, data.woodCost);

        if (data.firebloomCost > 0)
            storageBox.TakeItem(ItemType.Firebloom, data.firebloomCost);

        if (data.steelCost > 0)
            storageBox.TakeItem(ItemType.Steel, data.steelCost);
    }

    // 건물 수리 (BluePrint → Finished)
    public void RepairBuilding(Thing thing)
    {
        if (thing == null) return;
        thing.Setstage(BuildStage.Finished);
    }

    // 벽 수리 (타일맵에 벽 타일 복원)
    public void RepairWall(Vector3Int cell)
    {
        if (wallTilemap == null || wallTile == null) return;
        wallTilemap.SetTile(cell, wallTile);
    }

    // 완성된 건물 목록 반환
    public List<Thing> GetAllFinishedBuildings()
    {
        var result = new List<Thing>();
        var things = FindObjectsByType<Thing>(FindObjectsSortMode.None);
        foreach (var t in things)
        {
            if (t.stage == BuildStage.Finished)
                result.Add(t);
        }
        return result;
    }
}