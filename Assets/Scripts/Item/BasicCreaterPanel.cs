using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BasicCreaterPanel : MonoBehaviour
{
    public static BasicCreaterPanel Instance;
    public GameObject panel;
    public Transform listPanel;
    public Button buttonPrefab;
    public TextMeshProUGUI title;
    public ProgressBar progressBar;

    CraftingStation cur;
    CreaterProgress curProgress;

    private void Awake()
    {
        title.text = "¡¶¿€¥Î";
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(CraftingStation station)
    {
        cur = station;
        curProgress = station.GetComponent<CreaterProgress>();
         
        foreach (Transform t in listPanel) Destroy(t.gameObject);
        foreach (var r in station.recipeData)
        {
            var b = Instantiate(buttonPrefab, listPanel);
            //b.GetComponentInChildren<Sprite>()
            b.interactable = station.canCreaft(r);
            b.onClick.AddListener(() => cur.EnqueueCraft(r));
        }
        panel.SetActive(true);
    }

    public void Hide() { panel.SetActive(false); cur = null; curProgress = null; }

    private void Update()
    {
        if (panel == null || !panel.activeSelf || progressBar == null) return;

        float p = 0f;
        if (curProgress != null && curProgress.IsActive)
            p = curProgress.current01;
        progressBar.SetProgressBar(p);
    }
}
