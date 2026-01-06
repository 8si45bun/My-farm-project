using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    [Header("Floor")]
    public Transform gridParent;
    public GameObject floorPrefeb;
    public float floorOffset = -5f;

    [Header("Camera")]
    public CameraController cameraController;

    [Header("FadeInAndOut")]
    //public UIFadeInOutAnimation UIFadeInOutAnimation;

    private List<GameObject> floors = new List<GameObject>();
    private int floorCount = 0;
    private void Start()
    {
        floors.Add(gridParent.GetChild(0).gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            AddNewFloor();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            StartCoroutine(MoveCamera(floorCount + 1));
            //Debug.Log("Ä«¸Þ¶ó Å¸°Ù Ãþ : " + floorCount + 1);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            StartCoroutine(MoveCamera(floorCount - 1));
        }
    }

    private void AddNewFloor()
    {
        Vector3 newPos = new Vector3(0, floorOffset * floors.Count, 0);
        GameObject newFloor = Instantiate(floorPrefeb, newPos, Quaternion.identity, gridParent);
        newFloor.name = $"Floor_{floors.Count}";
        floors.Add (newFloor);

        foreach (GameObject floor in floors)
        {
           Debug.Log(" »õ·Î »ý±ä Ãþ : " +floor.name);
        }
    }

    private IEnumerator MoveCamera(int index)
    {
        if(index < 0 || index >= floors.Count) yield break;
        
        floorCount = index; // ÇöÀçÃþ °»½Å
        Vector3 TargetPos = floors[index].transform.position;
        //Debug.Log("Ä«¸Þ¶ó Å¸°Ù Ãþ : " + index);

        //yield return StartCoroutine(UIFadeInOutAnimation.FadeOut());
        cameraController.MoveTo(TargetPos);
        //yield return StartCoroutine(UIFadeInOutAnimation.FadeIn());
    }
}
