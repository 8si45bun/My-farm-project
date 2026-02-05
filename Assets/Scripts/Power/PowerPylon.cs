using UnityEngine;

public class PowerPylon : MonoBehaviour, IPowerSource
{
    public float supplyRadius = 5f;
    
    public bool IsLinked { get; set; } = false;

    private void Start()
    {
        if(PowerManager.Instance != null)
        {
            PowerManager.Instance.RegisterPylon(this);
            PowerManager.Instance.RecalculateGrid();
        }
    }

    private void OnDestroy()
    {
        if(PowerManager.Instance != null)
        {
            PowerManager.Instance.UnRegisterPylon(this);
            PowerManager.Instance.RecalculateGrid();
        }
    }

    public Vector3 GetPosition() => transform.position;
    public float GetRadius() => supplyRadius;

    private void OnDrawGizmos()
    {
        Gizmos.color = IsLinked ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, supplyRadius);
    }
}

