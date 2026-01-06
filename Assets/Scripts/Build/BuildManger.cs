using System.Text;
using TMPro;
using UnityEngine;

public class BuildManger : MonoBehaviour
{
    [Header("References")]
    public StorageBox storageBox;
    public TextManager textManager;

    [HideInInspector]
    public Vector2Int mouseGridPos;
    private bool isBuilding = false;
    private Vector2Int lastGrid = new Vector2Int(int.MaxValue, int.MinValue);

    [Header("Prefebs")]
    public GameObject creater;
    public GameObject miner;
    public GameObject generator;

    private GameObject previewPrefebs;

    [Header("Inspecter")]
    public int createrWoodCost = 1;
    public int minerFirebloomCost = 1;
    public int minerWoodCost = 1;
    public int buildMinutesCreater = 10;
    public int generatorWoodCost = 1;
    public int buildMinutesGenerator = 5;

    private enum BuildMode { None, Creater, Miner, Generator }
    private BuildMode buildMode = BuildMode.None;

    private void Update()
    {
        mouseGridPos = Vector2Int.CeilToInt(
            (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)
        );

        if (!isBuilding) return;

        if (previewPrefebs == null)
        {
            if (buildMode == BuildMode.Creater) previewPrefebs = Instantiate(creater);
            else if (buildMode == BuildMode.Miner) previewPrefebs = Instantiate(miner);
            else if (buildMode == BuildMode.Generator) previewPrefebs = Instantiate(generator);

            var thing = previewPrefebs.GetComponent<Thing>();
            if (thing == null) thing = previewPrefebs.AddComponent<Thing>();
            thing.Init(buildMode.ToString(), BuildStage.BulePrint);
        }

        if (mouseGridPos != lastGrid && previewPrefebs != null)
        {
            previewPrefebs.transform.position = new Vector3Int(mouseGridPos.x, mouseGridPos.y, 0);
            lastGrid = mouseGridPos;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!CanBuild(buildMode, out string lackMsg))
            {
                textManager.showText(lackMsg);
                return;
            }

            PayBuild(buildMode);

            var thing = previewPrefebs.GetComponentInChildren<Thing>();
            if (thing == null) thing = previewPrefebs.GetComponent<Thing>();
            thing.Setstage(BuildStage.BulePrint);

            Vector3Int cell = Vector3Int.RoundToInt(previewPrefebs.transform.position);

            int minutes = GetBuildMinutes(buildMode);

            JobDispatcher.Enqueue(new Job
            {
                type = CommandType.Build,
                cell = cell,
                targetThing = thing,
                buildMinutes = minutes
            });

            previewPrefebs = null;
            isBuilding = false;
            buildMode = BuildMode.None;
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Destroy(previewPrefebs);
            previewPrefebs = null;
            isBuilding = false;
            buildMode = BuildMode.None;
        }
    }
    private bool CanBuild(BuildMode mode, out string lackMessage)
    {
        int needWood = 0;
        int needFirebloom = 0;

        if (mode == BuildMode.Creater)
        {
            needWood = createrWoodCost;
        }
        else if (mode == BuildMode.Miner)
        {
            needWood = minerWoodCost;
            needFirebloom = minerFirebloomCost;
        }
        else if (mode == BuildMode.Generator)
        {
            needWood = generatorWoodCost;
        }

        int haveWood = storageBox.GetCount(ItemType.Wood);
        int haveFirebloom = storageBox.GetCount(ItemType.Firebloom);

        int lackWood = Mathf.Max(0, needWood - haveWood);
        int lackFirebloom = Mathf.Max(0, needFirebloom - haveFirebloom);

        if (lackWood == 0 && lackFirebloom == 0)
        {
            lackMessage = "";
            return true;
        }

        var sb = new StringBuilder("재료가 부족합니다: ");
        bool first = true;

        if (lackWood > 0)
        {
            sb.Append($"Wood {lackWood}개");
            first = false;
        }
        if (lackFirebloom > 0)
        {
            if (!first) sb.Append(", ");
            sb.Append($"Firebloom {lackFirebloom}개");
        }

        lackMessage = sb.ToString();
        return false;
    }

    private void PayBuild(BuildMode mode)
    {
        if (mode == BuildMode.Creater)
        {
            storageBox.TakeItem(ItemType.Wood, createrWoodCost);
        }
        else if (mode == BuildMode.Miner)
        {
            storageBox.TakeItem(ItemType.Wood, minerWoodCost);
            storageBox.TakeItem(ItemType.Firebloom, minerFirebloomCost);
        }
        else if (mode == BuildMode.Generator)
        {
            storageBox.TakeItem(ItemType.Wood, generatorWoodCost);
        }
    }

    private int GetBuildMinutes(BuildMode mode)
    {
        if (mode == BuildMode.Creater) return buildMinutesCreater;
        if (mode == BuildMode.Generator) return buildMinutesGenerator;
        return buildMinutesCreater;
    }

    public void BuildCreater()
    {
        if (CanBuild(BuildMode.Creater, out string msg))
        {
            isBuilding = true;
            buildMode = BuildMode.Creater;
            previewPrefebs = Instantiate(creater);
        }
        else
        {
            textManager.showText(msg);
        }
    }

    public void BuildMiner()
    {
        if (CanBuild(BuildMode.Miner, out string msg))
        {
            isBuilding = true;
            buildMode = BuildMode.Miner;
            previewPrefebs = Instantiate(miner);
        }
        else
        {
            textManager.showText(msg);
        }
    }

    public void BuildGenerator()
    {
        if (CanBuild(BuildMode.Generator, out string msg))
        {
            isBuilding = true;
            buildMode = BuildMode.Generator;
            previewPrefebs = Instantiate(generator);
        }
        else
        {
            textManager.showText(msg);
        }
    }
}
