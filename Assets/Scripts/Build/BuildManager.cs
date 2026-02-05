using System.Collections.Generic; 
using System.Text;
using UnityEngine;

// 1. 건물 정보를 담을 데이터 클래스 (인스펙터에서 보임)
[System.Serializable]
public class BuildingData
{
    public string buildingName;  // 건물 이름
    public GameObject prefab;    // 프리팹
    //public Sprite icon;          // UI 아이콘

    [Header("Costs")]
    public int woodCost = 0;     
    public int firebloomCost = 0;

    [Header("Settings")]
    public int buildMinutes = 10; // 건설 소요 시간
}

public class BuildManager : MonoBehaviour 
{
    public static BuildManager Instance; 

    [Header("References")]
    public StorageBox storageBox;
    public TextManager textManager;

    [Header("Buildings List")]
    public List<BuildingData> buildingList = new List<BuildingData>();

    // 현재 선택된 건물 데이터 (없으면 null)
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
        // 마우스 위치 계산
        mouseGridPos = Vector2Int.CeilToInt((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition));

        // 1. 짓는 중이 아니면 리턴 (currentBuilding이 null이면 안 짓는 상태)
        if (currentBuilding == null) return;

        // 2. 프리뷰 생성 (없을 때만)
        if (previewInstance == null)
        {
            previewInstance = Instantiate(currentBuilding.prefab);

            var thing = previewInstance.GetComponent<Thing>();
            if (thing == null) thing = previewInstance.AddComponent<Thing>();

            // 데이터에 있는 이름 사용
            thing.Init(currentBuilding.buildingName, BuildStage.BulePrint);
        }

        // 3. 프리뷰 위치 갱신
        if (mouseGridPos != lastGrid && previewInstance != null)
        {
            previewInstance.transform.position = new Vector3Int(mouseGridPos.x, mouseGridPos.y, 0);
            lastGrid = mouseGridPos;
        }

        // 4. 클릭해서 건설 (좌클릭)
        if (Input.GetMouseButtonDown(0))
        {
            // 재료 확인 (인자로 현재 건물 데이터를 넘김)
            if (CanBuild(currentBuilding, out string msg))
            {
                PayBuild(currentBuilding);

                // Thing 설정
                var thing = previewInstance.GetComponentInChildren<Thing>(); // 혹은 GetComponent
                if (thing == null) thing = previewInstance.GetComponent<Thing>();
                thing.Setstage(BuildStage.BulePrint); // 철자 주의 (BluePrint)

                Vector3Int cell = Vector3Int.RoundToInt(previewInstance.transform.position);

                // Job 등록
                JobDispatcher.Enqueue(new Job
                {
                    type = CommandType.Build,
                    cell = cell,
                    targetThing = thing,
                    buildMinutes = currentBuilding.buildMinutes
                });

                // 프리뷰(previewInstance)는 이제 "진짜 건물"이 되었으니 파괴하지 말고
                // 매니저의 손에서만 놓아줍니다.
                previewInstance = null;
                currentBuilding = null; // 선택 해제

                // 만약 연속 건설을 하고 싶으면 이 줄도 지우면 됩니다.
                // ▲▲▲
            }
            else
            {
                textManager.showText(msg);
            }
        }
        // 5. 취소 (우클릭)
        else if (Input.GetMouseButtonDown(1))
        {
            CancelBuildMode(); // 우클릭은 취소니까 파괴(Destroy)하는 게 맞음
        }
    }

    // 건설 모드 취소 및 초기화
    private void CancelBuildMode()
    {
        if (previewInstance != null) Destroy(previewInstance);
        previewInstance = null;
        currentBuilding = null; // 선택 해제
    }

    public void SelectBuilding(int index)
    {
        if (index >= 0 && index < buildingList.Count)
        {
            // 기존 프리뷰가 있다면 삭제
            if (previewInstance != null) Destroy(previewInstance);

            // 리스트에서 해당 번호의 건물 정보를 가져옴
            currentBuilding = buildingList[index];
            Debug.Log($"{currentBuilding.buildingName} 선택됨");
        }
        else
        {
            Debug.LogError("잘못된 건물 인덱스입니다.");
        }
    }

    // 비용 확인 로직 (데이터 클래스 기반으로 변경)
    private bool CanBuild(BuildingData data, out string lackMessage)
    {
        int haveWood = storageBox.GetCount(ItemType.Wood);
        int haveFirebloom = storageBox.GetCount(ItemType.Firebloom);

        // 데이터에 적힌 비용과 비교
        int lackWood = Mathf.Max(0, data.woodCost - haveWood);
        int lackFirebloom = Mathf.Max(0, data.firebloomCost - haveFirebloom);

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

    // 비용 지불 로직
    private void PayBuild(BuildingData data)
    {
        if (data.woodCost > 0)
            storageBox.TakeItem(ItemType.Wood, data.woodCost);

        if (data.firebloomCost > 0)
            storageBox.TakeItem(ItemType.Firebloom, data.firebloomCost);
    }
}