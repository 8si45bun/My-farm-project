using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    [Header("Category Panels")]
    public GameObject WorkCategory;
    public GameObject BuildCategory;
    public GameObject CreateCategory;

    [Header("Work Buttons")]
    public GameObject Dig;
    public GameObject Cult;
    public GameObject Plant;
    public GameObject Harvest;
    public GameObject Haul;

    [Header("Build Buttons")]
    public GameObject Creator;
    public GameObject Miner;

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
}
