using UnityEngine;
using UnityEngine.EventSystems;

public class ClickManager : MonoBehaviour
{
    public Camera Camera;

    void Update()
    {
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 p = Camera.ScreenToWorldPoint(Input.mousePosition);
            var col = Physics2D.OverlapPoint(p);

            var thing = col.GetComponentInParent<Thing>();
            var craft = col.GetComponentInParent<CraftingStation>();

            // ThingStatus
            var status = ThingStatus.Instance;
            if (status != null)
            {
                if (thing != null) status.Show(thing);
                else status.Hide();
            }

            // BasicCreaterPanel
            var panel = BasicCreaterPanel.Instance;
            if (panel != null)
            {
                if (thing != null && craft != null) panel.Show(craft);
                else panel.Hide();
            }
        }
    }
}
