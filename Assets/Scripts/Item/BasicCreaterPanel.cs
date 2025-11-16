using System.Collections.Generic;
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
    public List<GameObject> waitingList = new List<GameObject>();

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
            Image image = b.GetComponent<Image>();
            image.sprite = r.icon;
            
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
