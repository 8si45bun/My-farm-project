using UnityEngine;
using UnityEngine.UI;

public class MobileUIController : MonoBehaviour
{
    public static MobileUIController Instance;

    [Header("UI")]
    public GameObject priorityPanel;
    public Toggle mineToggle;
    public Toggle haulToggle;
    public Toggle buildToggle;
    public Toggle repairToggle;

    private RobotAgent selectedRobot;

    private void Awake()
    {
        Instance = this;
        if (priorityPanel != null)
            priorityPanel.SetActive(false);
    }

    public void SelectRobot(RobotAgent robot)
    {
        // 이전 리스너 제거
        RemoveListeners();

        selectedRobot = robot;

        if (priorityPanel != null)
            priorityPanel.SetActive(true);

        // 현재 값 반영
        if (mineToggle != null) mineToggle.isOn = robot.canMine;
        if (haulToggle != null) haulToggle.isOn = robot.canHaul;
        if (buildToggle != null) buildToggle.isOn = robot.canBuild;
        if (repairToggle != null) repairToggle.isOn = robot.canRepair;

        // 리스너 연결
        if (mineToggle != null) mineToggle.onValueChanged.AddListener(OnMineChanged);
        if (haulToggle != null) haulToggle.onValueChanged.AddListener(OnHaulChanged);
        if (buildToggle != null) buildToggle.onValueChanged.AddListener(OnBuildChanged);
        if (repairToggle != null) repairToggle.onValueChanged.AddListener(OnRepairChanged);
    }

    public void Deselect()
    {
        RemoveListeners();
        selectedRobot = null;

        if (priorityPanel != null)
            priorityPanel.SetActive(false);
    }

    private void RemoveListeners()
    {
        if (mineToggle != null) mineToggle.onValueChanged.RemoveListener(OnMineChanged);
        if (haulToggle != null) haulToggle.onValueChanged.RemoveListener(OnHaulChanged);
        if (buildToggle != null) buildToggle.onValueChanged.RemoveListener(OnBuildChanged);
        if (repairToggle != null) repairToggle.onValueChanged.RemoveListener(OnRepairChanged);
    }

    private void OnMineChanged(bool val) { if (selectedRobot != null) selectedRobot.canMine = val; }
    private void OnHaulChanged(bool val) { if (selectedRobot != null) selectedRobot.canHaul = val; }
    private void OnBuildChanged(bool val) { if (selectedRobot != null) selectedRobot.canBuild = val; }
    private void OnRepairChanged(bool val) { if (selectedRobot != null) selectedRobot.canRepair = val; }
}
