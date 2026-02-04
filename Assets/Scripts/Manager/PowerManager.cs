using System.Collections.Generic;
using UnityEngine;

public class PowerManager : MonoBehaviour
{
    public static PowerManager Instance; 

    // 맵에 있는 모든 발전기 목록 
    public List<BioFuelGenerator> generators = new List<BioFuelGenerator>();

    private void Awake()
    {
        Instance = this;
    }

    // 발전기가 생기면 목록에 추가
    public void RegisterGenerator(BioFuelGenerator gen)
    {
        if (!generators.Contains(gen))
        {
            generators.Add(gen);
        }
    }

    // 발전기가 파괴되면 목록에서 제거 
    public void UnregisterGenerator(BioFuelGenerator gen)
    {
        if (generators.Contains(gen))
        {
            generators.Remove(gen);
        }
    }

    public Transform GetClosestCharger(Vector3 robotPos)
    {
        BioFuelGenerator closest = null;
        float bestDist = float.MaxValue;

        foreach (var gen in generators)
        {
            if (gen == null) continue;

            float d = Vector3.Distance(robotPos, gen.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                closest = gen;
            }
        }

        return closest != null ? closest.transform : null;
    }
}