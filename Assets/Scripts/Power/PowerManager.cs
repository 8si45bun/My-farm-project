using System.Collections.Generic;
using UnityEngine;

public class PowerManager : MonoBehaviour
{
    public static PowerManager Instance;

    public List<PowerPylon> pylons = new List<PowerPylon>();
    public List<BioFuelGenerator> generator = new List<BioFuelGenerator>();

    private void Awake() => Instance = this;

    // --- 등록/해제 ---
    public void RegisterSource(BioFuelGenerator source)
    {
        if (!generator.Contains(source)) { generator.Add(source); RecalculateGrid(); }
    }
    public void UnRegisterSource(BioFuelGenerator source)
    {
        if (generator.Contains(source)) { generator.Remove(source); RecalculateGrid(); }
    }
    public void RegisterPylon(PowerPylon source)
    {
        if (!pylons.Contains(source)) { pylons.Add(source); RecalculateGrid(); }
    }
    public void UnRegisterPylon(PowerPylon source)
    {
        if (pylons.Contains(source)) { pylons.Remove(source); RecalculateGrid(); }
    }

    // --- 전력망 계산 ---
    public void RecalculateGrid()
    {
        pylons.RemoveAll(p => p == null);
        generator.RemoveAll(g => g == null);

        foreach (var pylon in pylons) pylon.IsLinked = false;

        Queue<IPowerSource> q = new Queue<IPowerSource>();
        HashSet<IPowerSource> visited = new HashSet<IPowerSource>();

        foreach (var gen in generator)
        {
            q.Enqueue(gen);
            visited.Add(gen);
        }

        while (q.Count > 0)
        {
            IPowerSource current = q.Dequeue();

            foreach (var pylon in pylons)
            {
                if (visited.Contains(pylon)) continue;

                float d = Vector3.Distance(current.GetPosition(), pylon.GetPosition());
                if (d <= current.GetRadius())
                {
                    pylon.IsLinked = true;
                    q.Enqueue(pylon);
                    visited.Add(pylon);
                }
            }
        }
    }

    // --- 유틸리티 함수 ---
    public bool IsIInPowerRange(Vector3 position)
    {
        // 발전기 체크 (거꾸로 돌면서 죽은 놈 삭제)
        for (int i = generator.Count - 1; i >= 0; i--)
        {
            // 에러 원인 해결: 건물이 파괴되었으면(null) 리스트에서 지워버림
            if (generator[i] == null)
            {
                generator.RemoveAt(i);
                continue;
            }

            // 살아있는 건물만 거리 체크
            if (Vector3.Distance(position, generator[i].GetPosition()) <= generator[i].GetRadius())
                return true;
        }

        // 2. 전봇대 체크 (거꾸로 돌면서 죽은 놈 삭제)
        for (int i = pylons.Count - 1; i >= 0; i--)
        {
            // 에러 원인 해결: 전봇대가 파괴되었으면(null) 리스트에서 지워버림
            if (pylons[i] == null)
            {
                pylons.RemoveAt(i);
                continue;
            }

            // 살아있고 + 연결된 전봇대만 거리 체크
            if (pylons[i].IsLinked && Vector3.Distance(position, pylons[i].transform.position) <= pylons[i].supplyRadius)
                return true;
        }

        return false;
    }

    public Transform GetClosestPowerSource(Vector3 position)
    {
        IPowerSource closest = null;
        float minDist = float.MaxValue; 

        foreach (var gen in generator)
        {
            float d = Vector3.Distance(position, gen.GetPosition());
            if (d < minDist)
            {
                minDist = d;
                closest = gen; 
            }
        }

        foreach (var pylon in pylons)
        {
            if (!pylon.IsLinked) continue;

            float d = Vector3.Distance(position, pylon.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = pylon;
            }
        }
        MonoBehaviour target = closest as MonoBehaviour;
        if (target != null)
        {
            return target.transform;
        }
        else
        {
            return null;
        }
    }
}