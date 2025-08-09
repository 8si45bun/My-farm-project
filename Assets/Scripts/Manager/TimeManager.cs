using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static Action OnMinuteChanged;
    public static Action OnHourChanged;

    public static int Minute {  get; private set; } // 내부에서만 변경 가능하도록
    public static int Hour { get; private set; }

    private float minuteToRealTime = 0.5f;
    private float timer;

    void Start()
    {
        Minute = 0;
        Hour = 10;
        timer = minuteToRealTime;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            Minute++;
            OnMinuteChanged?.Invoke();
            if(Minute >= 60)
            {
                Hour++;
                Minute = 0;
                OnHourChanged?.Invoke();
            }

            timer = minuteToRealTime;
        }
    }
}
