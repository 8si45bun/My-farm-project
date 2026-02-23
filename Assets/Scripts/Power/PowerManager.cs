using System.Collections.Generic;
using UnityEngine;

public class PowerManager : MonoBehaviour
{
    public static PowerManager Instance;

    public List<PowerPylon> pylons = new List<PowerPylon>();
    public List<BioFuelGenerator> generator = new List<BioFuelGenerator>();

    private void Awake() => Instance = this;

    // --- 전력량 용량 예약 모델 ---
    public float TotalCapacity { get; private set; } = 0f;
    public float TotalConsumed { get; private set; } = 0f;
    public float RemainingPower => Mathf.Max(0f, TotalCapacity - TotalConsumed);

    public void AddCapacity(float amount) => TotalCapacity += amount;
    public void RemoveCapacity(float amount) => TotalCapacity = Mathf.Max(0f, TotalCapacity - amount);

    public bool TryAddConsumer(float demand)
    {
        if (RemainingPower >= demand) { TotalConsumed += demand; return true; }
        return false;
    }
    public void RemoveConsumer(float demand) => TotalConsumed = Mathf.Max(0f, TotalConsumed - demand);

    // --- ���/���� ---
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

    // --- ���¸� ��� ---
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

    // --- ��ƿ��Ƽ �Լ� ---
    public bool IsIInPowerRange(Vector3 position)
    {
        // ������ üũ (�Ųٷ� ���鼭 ���� �� ����)
        for (int i = generator.Count - 1; i >= 0; i--)
        {
            // ���� ���� �ذ�: �ǹ��� �ı��Ǿ�����(null) ����Ʈ���� ��������
            if (generator[i] == null)
            {
                generator.RemoveAt(i);
                continue;
            }

            // ����ִ� �ǹ��� �Ÿ� üũ
            if (Vector3.Distance(position, generator[i].GetPosition()) <= generator[i].GetRadius())
                return true;
        }

        // 2. ������ üũ (�Ųٷ� ���鼭 ���� �� ����)
        for (int i = pylons.Count - 1; i >= 0; i--)
        {
            // ���� ���� �ذ�: �����밡 �ı��Ǿ�����(null) ����Ʈ���� ��������
            if (pylons[i] == null)
            {
                pylons.RemoveAt(i);
                continue;
            }

            // ����ְ� + ����� �����븸 �Ÿ� üũ
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