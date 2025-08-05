using UnityEngine;

public class RobotDetect : MonoBehaviour
{
    [HideInInspector]
    public bool inSoftGround = false;
    private int softLayer;

    private void Awake()
    {
        softLayer = LayerMask.NameToLayer("SoftGround");
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("ºÎµå·¯¿î¶¥");
        if(collision.gameObject.layer == softLayer) 
            inSoftGround = true;
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log("ÀÏ¹Ý ¶¥");
        if (collision.gameObject.layer == softLayer)
            inSoftGround = false;   
    }
}
