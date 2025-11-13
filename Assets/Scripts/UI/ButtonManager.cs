using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    [Header("Category Buttons")]
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

    private bool isBuild = false;
    private bool isWork = false;
    private bool isCreate = false;

    private void Update()
    {
        if (isBuild) ShowBuildButtons();
        else HideBuildButtons();

        if (isWork) ShowWorkButtons();
        else HideWorkButtons();

        if (isCreate) ShowCreateButtons();
        else HideCreateButtons();
    }



    public void BuildButtonPress()
    {
        isBuild = !isBuild;
        isWork = false;
        isCreate = false;
    }

    public void WorkButtonPress()
    {
        isWork = !isWork;
        isBuild = false;
        isCreate = false;
    }

    public void CreateButtonPress()
    {
        isCreate = !isCreate;
        isBuild = false;
        isWork = false;
    }

    private void ShowCreateButtons()
    {
        CreateCategory.SetActive(true);
    }

    private void HideCreateButtons()
    {
        CreateCategory.SetActive(false);
    }

    private void ShowBuildButtons()
    {
        BuildCategory.SetActive(true);
    }

    private void HideBuildButtons()
    {
        BuildCategory.SetActive(false);
    }

    private void ShowWorkButtons()
    {
        WorkCategory.SetActive(true);
    }

    private void HideWorkButtons()
    {
        WorkCategory.SetActive(false);
    }

}
