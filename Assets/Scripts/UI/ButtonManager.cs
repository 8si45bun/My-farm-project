using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    public static ButtonManager Instance { get; private set; }

    [Header("Category Panels")]
    public GameObject WorkCategory;
    public GameObject BuildCategory;
    public GameObject CreateCategory;
    public GameObject PlantCategory;

    [Header("식물 데이터")]
    public PlantData CurrentPlant { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        HideAllCategories();
        HideOverviewPanel();
    }

    private void HideAllCategories()
    {
        if (WorkCategory != null) WorkCategory.SetActive(false);
        if (BuildCategory != null) BuildCategory.SetActive(false);
        if (CreateCategory != null) CreateCategory.SetActive(false);
        if (PlantCategory != null) PlantCategory.SetActive(false);
    }

    private void HideOverviewPanel()
    {
        if (CraftingStationsOverviewPanel.Instance != null)
        {
            CraftingStationsOverviewPanel.Instance.Hide();
        }
    }

    public void BuildButtonPress()
    {
        bool willShow = BuildCategory != null && !BuildCategory.activeSelf;

        HideAllCategories();
        HideOverviewPanel();

        if (BuildCategory != null && willShow)
            BuildCategory.SetActive(true);
    }

    public void WorkButtonPress()
    {
        bool willShow = WorkCategory != null && !WorkCategory.activeSelf;

        HideAllCategories();
        HideOverviewPanel();

        if (WorkCategory != null && willShow)
            WorkCategory.SetActive(true);
    }

    public void CreateButtonPress()
    {
        bool willShow = CreateCategory != null && !CreateCategory.activeSelf;

        HideAllCategories();
        HideOverviewPanel();

        if (CreateCategory != null && willShow)
            CreateCategory.SetActive(true);
    }

    public void PlantButtonPress()
    {
        bool willShow = PlantCategory != null && !PlantCategory.activeSelf;

        //HideAllCategories();
        //HideOverviewPanel();

        if (PlantCategory != null && willShow)
            PlantCategory.SetActive(true);
    }

    public void CraftOverviewButtonPress()
    {
        var overview = CraftingStationsOverviewPanel.Instance;
        if (overview == null) return;

        bool willShow = !overview.IsVisible;

        HideAllCategories();

        if (willShow)
            overview.Show();
        else
            overview.Hide();
    }

    public void SelectPlant(PlantData data)
    {
        CurrentPlant = data;
        Debug.Log($"선택한 작물: {data.name}");
    }
}
