using TMPro;
using UnityEngine;

public class PowerManager : MonoBehaviour
{
    public static PowerManager Instance { get; private set; }
    public int power = 0;
    public TextMeshProUGUI Electirc;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Electirc.text = power.ToString();
    }

    public void AddPower(int p)
    {
        power += p;
        Electirc.text = power.ToString();   
    }
}
