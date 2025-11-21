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
        title.text = "제작대";
        Instance = this;
        panel.SetActive(false);
    }
    private void OnEnable()
    {
        JobDispatcher.OnJobCompleted += HandleJobCompleted;
    }

    private void OnDisable()
    {
        JobDispatcher.OnJobCompleted -= HandleJobCompleted;
    }

    public void Show(CraftingStation station)
    {
        cur = station;
        curProgress = station.GetComponent<CreaterProgress>();

        foreach (Transform t in listPanel) Destroy(t.gameObject);
        foreach (var r in station.recipeData)
        {
            var recipe = r;

            var b = Instantiate(buttonPrefab, listPanel);
            Image image = b.GetComponent<Image>();
            image.sprite = r.icon;

            b.interactable = station.canCreaft(r);

            b.onClick.AddListener(() =>
            {
                if (TryAddWaitingSlot(recipe.icon))
                {
                    cur.EnqueueCraft(r);
                }
                else
                {
                    TextManager.ShowDebug("대기열이 포화상태 입니다");
                }

            });
        }

        foreach (var slot in waitingList)
        {
            var img = slot.GetComponent<Image>();
            if (img != null) img.sprite = null;
        }

        if (cur != null && cur.thing != null)
        {
            var jobs = JobDispatcher.GetQueueCreaterJob(cur.thing);
            for (int i = 0; i < waitingList.Count && i < jobs.Count; i++)
            {
                var img = waitingList[i].GetComponent<Image>();
                if (img != null && jobs[i].recipeData != null)
                    img.sprite = jobs[i].recipeData.icon;
            }
        }

        panel.SetActive(true);
    }

    private bool TryAddWaitingSlot(Sprite icon)
    {
        foreach (var slot in waitingList)
        {
            var img = slot.GetComponent<Image>();
            if (img.sprite == null)
            {
                img.sprite = icon;
                return true;
            }

        }

        return false;
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

    private void HandleJobCompleted(Job job, bool success)
    {
        if (!success) return;
        if (job == null) return;
        if (job.type != CommandType.Craft) return;

        if (cur == null || cur.thing == null) return;
        if (job.targetThing != cur.thing) return;

        PopWaitingSlot();
    }
    private void PopWaitingSlot()
    {
        int lastFilled = -1;
        for (int i = 0; i < waitingList.Count; i++)
        {
            var slot = waitingList[i];
            var img = slot.GetComponent<Image>();
            if (img.sprite != null)
            {
                lastFilled = i;
            }
        }

        if (lastFilled == -1) return;

        for (int i = 0; i < lastFilled; i++)
        {
            var curSlot = waitingList[i];
            var nextSlot = waitingList[i + 1];

            var curImg = curSlot.GetComponent<Image>();
            var nextImg = nextSlot.GetComponent<Image>();

            if (nextImg != null)
            {
                curImg.sprite = nextImg.sprite;
            }
            else
            {
                curImg.sprite = null;
            }

        }

        var lastSlot = waitingList[lastFilled];
        if (lastSlot != null)
        {
            var lastImg = lastSlot.GetComponent<Image>();
            lastImg.sprite = null;
        }
    }

}
