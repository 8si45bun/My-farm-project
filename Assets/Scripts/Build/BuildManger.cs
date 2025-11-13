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
    private Vector2Int lastGrid = new Vector2Int(int.MaxValue,int.MinValue);

    [Header("Prefebs")]
    public GameObject creater;
    public GameObject miner;

    private GameObject previewPrefebs;

    [Header("Inspecter")]
    public int createrCost = 1;
    public int minerFirebloomCost = 1;
    public int minerWoodCost = 1;
    public int buildMinutesCreater = 10;

    private enum BuildMode { None, Creater, Miner }
    private BuildMode buildMode = BuildMode.None;


    private void Update()
    {
        mouseGridPos = Vector2Int.CeilToInt(
                (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)
                );

        if (isBuilding)
        {
           
            if (previewPrefebs == null)
            {
                if(buildMode == BuildMode.Creater) previewPrefebs = Instantiate(creater);
                else if(buildMode == BuildMode.Miner) previewPrefebs = Instantiate(miner);

                var thing = previewPrefebs.GetComponent<Thing>();
                if (thing == null) previewPrefebs.AddComponent<Thing>();
                thing.Init(buildMode.ToString(), BuildStage.BulePrint);                        
            }

            if (mouseGridPos != lastGrid && previewPrefebs != null)
            {
                previewPrefebs.transform.position = new Vector3Int(mouseGridPos.x, mouseGridPos.y, 0);
                lastGrid = mouseGridPos;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (!CanBuild(buildMode))
                {
                    textManager.showText("재료가 부족합니다 그게 뭐가 됐건");
                    return;
                }

                PayBuild(buildMode);

                var thing = previewPrefebs.GetComponentInChildren<Thing>();
                if (thing == null) previewPrefebs.GetComponent<Thing>();
                thing.Setstage(BuildStage.BulePrint);

                Vector3Int cell = Vector3Int.RoundToInt(previewPrefebs.transform.position);

                JobDispatcher.Enqueue(new Job
                {
                    type = CommandType.Build,
                    cell = cell,
                    targetThing = thing,
                    buildMinutes = buildMinutesCreater
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
    }

    private bool CanBuild(BuildMode mode)
    {
        if (mode == BuildMode.Creater)
        {
            return storageBox.GetCount(ItemType.Wood) >= createrCost;
        }
        else if (mode == BuildMode.Miner)
        {
            return storageBox.GetCount(ItemType.Wood) >= minerWoodCost
                && storageBox.GetCount(ItemType.Firebloom) >= minerFirebloomCost;
        }
        return false;
    }

    private void PayBuild(BuildMode mode)
    {
        if (mode == BuildMode.Creater)
        {
            storageBox.TakeItem(ItemType.Wood, createrCost);
        }
        else if (mode == BuildMode.Miner)
        {
            storageBox.TakeItem(ItemType.Wood, minerWoodCost);
            storageBox.TakeItem(ItemType.Firebloom, minerFirebloomCost);
        }
    }

    public void BuildCreater()
    {
        int cnt = storageBox.GetCount(ItemType.Wood);
        if (cnt >= createrCost)
        {
            isBuilding = true;
            buildMode = BuildMode.Creater;
            previewPrefebs = Instantiate(creater);
        }
        else
        {
            textManager.showText("재료가 부족합니다 그게 뭐가 됐건");
        }
    }

    public void BuildMiner()
    {
        int wood = storageBox.GetCount(ItemType.Wood);
        int firebloom = storageBox.GetCount(ItemType.Firebloom);
        if (wood >= minerFirebloomCost && firebloom >= minerWoodCost)
        {
            isBuilding = true;
            buildMode = BuildMode.Miner;
            previewPrefebs = Instantiate(miner);
        }
        else
        {
            textManager.showText("재료가 부족합니다 그게 뭐가 됐건");
        }
    }
}
