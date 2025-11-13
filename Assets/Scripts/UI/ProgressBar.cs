using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    public Image fill;

    public void SetProgressBar(float progress)
    {
        fill.fillAmount = Mathf.Clamp01(progress);        
    }
}
