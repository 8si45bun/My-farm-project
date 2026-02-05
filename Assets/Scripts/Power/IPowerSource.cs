using UnityEngine;

public interface IPowerSource
{
    Vector3 GetPosition(); // 위치 반환
    float GetRadius(); // 전력 공급 범위 반환
}
