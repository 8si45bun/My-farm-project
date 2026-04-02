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
            if (col == null) return;

            var thing = col.GetComponentInParent<Thing>();
            var craft = col.GetComponentInParent<CraftingStation>();
            var generator = col.GetComponentInParent<BioFuelGenerator>();
            var robotAgent = col.GetComponentInParent<RobotAgent>();

            // ThingStatus
            var status = ThingStatus.Instance;
            if (status != null)
            {
                if (thing != null) status.Show(thing);
                else status.Hide();
            }

            // CreaterPanel (���۴� Ŭ���� ����)
            var createrPanel = BasicCreaterPanel.Instance;
            if (createrPanel != null)
            {
                if (thing != null && craft != null) createrPanel.Show(craft);
                else createrPanel.Hide();
            }

            // BioFuelGeneratorPanel
            var genPanel = BioFuelGeneratorPanel.Instance;
            if (genPanel != null)
            {
                if (generator != null) genPanel.Show(generator);
                else genPanel.Hide();
            }

            // MobileUIController (로봇 클릭 시 작업 우선순위 패널)
            var mobileUI = MobileUIController.Instance;
            if (mobileUI != null)
            {
                if (robotAgent != null) mobileUI.SelectRobot(robotAgent);
                else mobileUI.Deselect();
            }
        }
    }
}
