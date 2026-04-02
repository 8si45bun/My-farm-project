using UnityEngine;

public class Turret : MonoBehaviour
{
    [Header("Turret Settings")]
    public float range = 5f;
    public float fireInterval = 2f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    private float fireTimer;

    private void Update()
    {
        // 전력 범위 내에서만 작동
        if (PowerManager.Instance == null) return;
        if (!PowerManager.Instance.IsIInPowerRange(transform.position)) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            fireTimer = fireInterval;
            TryFire();
        }
    }

    private void TryFire()
    {
        EnemyAgent closest = null;
        float minDist = float.MaxValue;

        foreach (var enemy in EnemyAgent.All)
        {
            if (enemy == null) continue;
            if (enemy.currentState == EnemyState.Dead) continue;

            float d = Vector2.Distance(transform.position, enemy.transform.position);
            if (d <= range && d < minDist)
            {
                minDist = d;
                closest = enemy;
            }
        }

        if (closest == null) return;

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        var go = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
        var proj = go.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Init(closest.transform);
        }
    }
}
