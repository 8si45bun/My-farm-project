using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class MinerStation : MonoBehaviour
{
    [Header("UI")]
    public Button WorkingButton;
    public Color offColor = Color.red;
    public Color onColor = Color.green;

    [Header("¼³Á¤")]
    public int mineMinutes = 5;
    public Thing thing;

    [Header("±¤¹°")]
    public Tilemap resourceTimemap = null;
    public TileBase steelTile;
    public GameObject SteelPrefab;
    public GameObject StonePrefab;

    private bool IsWorking = false;
    private bool jobActive = false;

    private void Awake()
    {
        if(thing == null)
            thing = GetComponent<Thing>();

        var go = GameObject.FindWithTag("FloorTilemap");
        resourceTimemap = go.GetComponent<Tilemap>();

        UpdateButtonColor();
    }

    private void OnEnable()
    {
        JobDispatcher.OnJobCompleted += HandleJobCompleted;
    }

    private void OnDisable()
    {
        JobDispatcher.OnJobCompleted -= HandleJobCompleted;
    }

    public GameObject GetOrePrefab(Vector3Int cell)
    {
        if(resourceTimemap == null) return null;

        var tile = resourceTimemap.GetTile(cell);
        if (tile == null) return null;

        if (tile == steelTile) return SteelPrefab;
        else return StonePrefab; // ¾Æ¹« ±¤¹°µµ ¾Æ´Ò½Ã
    }

    public void Working()
    {
        IsWorking = !IsWorking;
        UpdateButtonColor();

        Debug.Log(IsWorking);
        if (IsWorking)
        {
            Debug.Log("Ã¤±¼");
            TryRequestMineJob();
        }
    }

    private void UpdateButtonColor()
    {
        var colors = WorkingButton.colors;
        var c = IsWorking ? onColor : offColor;

        colors.normalColor = c;
        colors.highlightedColor = c;
        colors.pressedColor = c;
        colors.selectedColor = c;
        WorkingButton.colors = colors;
    }

    private void TryRequestMineJob()
    {
        if(!IsWorking) return;
        if (jobActive) return;
        if (thing == null) return;

        var cell = Vector3Int.RoundToInt(transform.position);

        JobDispatcher.Enqueue(new Job
        {
            type = CommandType.Mine,
            cell = cell,
            targetThing = thing,
            MinerMinute = mineMinutes,

        });

        jobActive = true;
    }

    private void HandleJobCompleted(Job job, bool success)
    {
        if(!success) return;
        if(job == null) return;
        if (job.type != CommandType.Mine) return;
        if(job.targetThing != this.thing) return;

        jobActive = false;
        Debug.Log(IsWorking);
        if (IsWorking)
        {
            Debug.Log("´Ù½Ã Ã¤±¼");
            TryRequestMineJob();
        }
    }
}
