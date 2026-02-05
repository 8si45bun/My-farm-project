using UnityEngine;

public class Thing : MonoBehaviour
{
    [Header("Basic Info")]
    public string thingName;
    public string thingId;
    public BuildStage stage;

    private SpriteRenderer spriteRenderer;

    public void Init(string name, BuildStage initialStage)
    {
        thingName = name;
        spriteRenderer = GetComponent<SpriteRenderer>(); 
        Setstage(initialStage);
    }

    public void Setstage(BuildStage newStage)
    {
        stage = newStage;

        bool isComplete = (stage == BuildStage.Finished);

        UpdateVisuals(isComplete);
        ToggleComponents(isComplete);
    }

    // 시각 효과
    private void UpdateVisuals(bool isComplete)
    {
        if (spriteRenderer != null)
        {          
            float alpha = isComplete ? 1f : 0.5f;
            spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    // 기능 자동 ON/OFF 
    private void ToggleComponents(bool isComplete)
    {
        // A. 내 자식들에 있는 모든 스크립트(MonoBehaviour)를 뒤진다
        MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>(true);

        foreach (var script in scripts)
        {
            // 나 자신(Thing)은 끄면 안 됨! (관리해야 하니까)
            if (script == this) continue;

            // 건물 기능 스크립트들(발전기, 전봇대 등) 켜기/끄기
            script.enabled = isComplete;
        }

        // B. UI 캔버스(체력바, 연료게이지 등) 뒤져서 켜기/끄기
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var canvas in canvases)
        {
            canvas.gameObject.SetActive(isComplete);
        }

        // C. 충돌체(Collider) 끄기 (선택사항: 건설 중에는 로봇이 통과하게 하려면)
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (var col in colliders)
        {
            col.enabled = isComplete;
        }
    }
}