using System.Collections.Generic;
using UnityEngine;

public class PowerRangeVisualizer : MonoBehaviour
{
    public static PowerRangeVisualizer Instance;

    [Header("Settings")]
    public Color generatorColor = new Color(1f, 0.84f, 0f, 0.8f); // 금색
    public Color pylonColor = new Color(0.53f, 0.81f, 0.98f, 0.8f); // 하늘색
    public float lineWidth = 0.05f;
    public int segments = 64;

    private List<GameObject> circleObjects = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 발전기의 전력 네트워크 범위를 시각화
    /// </summary>
    public void ShowNetwork(BioFuelGenerator generator)
    {
        HideAll();

        if (generator == null || PowerManager.Instance == null) return;

        // 발전기 범위 원 (금색)
        CreateCircle(generator.GetPosition(), generator.GetRadius(), generatorColor);

        // BFS로 연결된 파일런 탐색 (PowerManager.RecalculateGrid와 동일 로직)
        var pylons = PowerManager.Instance.pylons;
        var generators = PowerManager.Instance.generator;

        Queue<IPowerSource> q = new Queue<IPowerSource>();
        HashSet<IPowerSource> visited = new HashSet<IPowerSource>();

        q.Enqueue(generator);
        visited.Add(generator);

        while (q.Count > 0)
        {
            IPowerSource current = q.Dequeue();

            foreach (var pylon in pylons)
            {
                if (pylon == null) continue;
                if (visited.Contains(pylon)) continue;

                float d = Vector3.Distance(current.GetPosition(), pylon.GetPosition());
                if (d <= current.GetRadius())
                {
                    visited.Add(pylon);
                    q.Enqueue(pylon);

                    // 파일런 범위 원 (하늘색)
                    CreateCircle(pylon.GetPosition(), pylon.supplyRadius, pylonColor);
                }
            }
        }
    }

    /// <summary>
    /// 모든 범위 원 제거
    /// </summary>
    public void HideAll()
    {
        foreach (var obj in circleObjects)
        {
            if (obj != null) Destroy(obj);
        }
        circleObjects.Clear();
    }

    private void CreateCircle(Vector3 center, float radius, Color color)
    {
        var go = new GameObject("PowerRangeCircle");
        go.transform.position = center;

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = segments;
        lr.sortingOrder = 10;

        // 기본 머티리얼 설정
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;

        // 원 그리기
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 360f * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }

        circleObjects.Add(go);
    }
}
