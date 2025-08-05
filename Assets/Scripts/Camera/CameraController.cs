using UnityEngine;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    [Header("Camera Setting")]
    public float cameraSpeed = 5f;

    private Vector3 targetpos;
    private bool isMoving = false;

    private void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, targetpos, Time.deltaTime * cameraSpeed);

            if (Vector3.Distance(transform.position, targetpos) < 0.001f)
            {
                transform.position = targetpos;
                isMoving = false;
            }
        }
    }

    public void MoveTo(Vector3 pos)
    {
        targetpos = new Vector3(pos.x, pos.y, transform.position.z);
        isMoving = true;
    }
}
