using UnityEngine;

public class Thing : MonoBehaviour
{
    public string thingId;
    public BuildStage stage;

    SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if(sr == null ) sr = GetComponent<SpriteRenderer>();
    }

    public void Init(string id, BuildStage b)
    {
        thingId = id;
        Setstage(b);
    }

    public void Setstage(BuildStage b)
    {
        stage = b;

        switch (stage)
        {
            case BuildStage.BulePrint:
                sr.color = new Color(1f, 1f, 1f, 0.6f);
                gameObject.layer = LayerMask.NameToLayer("Ghost");
                break;

            case BuildStage.Finished:
                sr.color = Color.white;
                gameObject.layer = LayerMask.NameToLayer("Default");
                break;
        }

    }
}
