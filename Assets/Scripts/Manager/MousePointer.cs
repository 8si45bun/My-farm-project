using Unity.VisualScripting;
using UnityEngine;

public class MousePointer : MonoBehaviour
{
    [HideInInspector]
    public Vector2Int mouseGridPos;

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            mouseGridPos = Vector2Int.CeilToInt(
                (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)
                );

            Debug.Log("mousePointerPos : " + mouseGridPos);
        }
    }
}
