using UnityEngine;

public class CreaterProgress : MonoBehaviour
{
    private int startTotal;
    private int durationMinutes;
    public bool IsActive = false;  

    public float current01
    {
        get
        {
            if (!IsActive || durationMinutes < 0) return 0f;
            int now = TimeManager.Hour * 60 + TimeManager.Minute;
            int elapsed = (now - startTotal + 24 * 60) % (24 * 60);
            return Mathf.Clamp01(elapsed / (float)durationMinutes);
        }
    }

    public void StartProgress(int minutes)
    {
        durationMinutes = Mathf.Max(1, minutes);
        startTotal = TimeManager.Hour * 60 + TimeManager.Minute;
        IsActive = true;
    }

    public void StopProgress()
    {
        IsActive = false;
        durationMinutes = 0;
    }
}
