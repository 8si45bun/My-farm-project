using UnityEngine;
using TMPro;

public class ThingStatus : MonoBehaviour
{
    public static ThingStatus Instance;

    [Header("UI Reference")]
    public GameObject panel;
    public TextMeshProUGUI id;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(Thing t)
    {
        panel.SetActive(true);
        id.text = t.thingId ?? "¿À·ù";
    }

    public void Hide() 
    {
        panel.SetActive(false);
    }  
}

