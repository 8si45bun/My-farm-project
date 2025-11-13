using System.Collections;
using UnityEngine;

public class RobotProgress : MonoBehaviour
{
    [Header("Reference")]
    public Canvas canvas;
    public ProgressBar bar;

    private Coroutine progressRoutine;

    private void Awake()
    {
        canvas.gameObject.SetActive(false);
        bar.SetProgressBar(0);
    }

    public void PlayGameMinutes(int minute)
    {
        StopIfRunning();
        progressRoutine = StartCoroutine(GameMinutesRoutine(Mathf.Max(0, minute)));
    }

    public void StopHide()
    {
        StopIfRunning();
        bar.SetProgressBar(0);
        canvas.gameObject.SetActive(false);
    }

    private void StopIfRunning()
    {
        if (progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
            progressRoutine = null;
        }
    }

    private IEnumerator GameMinutesRoutine(int minute)
    {
        canvas.gameObject.SetActive(true);
        if(minute <= 0)
        {
            bar.SetProgressBar(1);
            canvas.gameObject.SetActive(false);
            yield break;
        }

        bar.SetProgressBar(0);

        int startTotal = TimeManager.Hour * 60 + TimeManager.Minute;
        int targetTotal = (startTotal + minute) % (24 * 60);

        while (true)
        {
            int currTotal = TimeManager.Hour * 60 + TimeManager.Minute;
            int elapsed = (currTotal - startTotal + 24 * 60) % (24 * 60);

            float p = Mathf.Clamp01(elapsed / (float)minute);
            if (bar) bar.SetProgressBar(p);

            if (currTotal == targetTotal) break;
            yield return null;
        }

        canvas.gameObject.SetActive(false);
        progressRoutine = null;
    }
}