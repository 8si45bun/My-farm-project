using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static event Action OnMinuteChanged;
    public static event Action OnHourChanged;

    public static int Minute { get; private set; }
    public static int Hour { get; private set; }

    [SerializeField] private float secondsPerGameMinute = 0.5f;
    [SerializeField] private bool wrapAtMidnight = true;

    private float timer;
    private bool paused;

    void Start()
    {
        SetTime(10, 0);
        timer = secondsPerGameMinute;
    }

    void Update()
    {
        if (paused) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer += secondsPerGameMinute; // 드리프트 보정
        Minute++;
        OnMinuteChanged?.Invoke();

        if (Minute >= 60)
        {
            Minute = 0;
            Hour++;
            OnHourChanged?.Invoke();

            if (wrapAtMidnight && Hour >= 24) Hour = 0;
        }
    }

    public static void SetTime(int hour, int minute)
    {
        Hour = Mathf.Clamp(hour, 0, 23);
        Minute = Mathf.Clamp(minute, 0, 59);
    }

    // 게임 '분' 단위 대기 (코루틴)
    public static System.Collections.IEnumerator WaitGameMinutes(int minutes)
    {
        if (minutes <= 0) yield break;

        int start = Hour * 60 + Minute;
        int target = (start + minutes) % (24 * 60);

        // 분 변화만 감지하며 대기
        int lastTotal = start;
        while (true)
        {
            int total = Hour * 60 + Minute;
            if (total != lastTotal)
            {
                lastTotal = total;
                if (total == target) break;
            }
            yield return null;
        }
    }

    public void PauseTime(bool pause) => paused = pause;
    public void SetSecondsPerGameMinute(float seconds) => secondsPerGameMinute = Mathf.Max(0.01f, seconds);
}
