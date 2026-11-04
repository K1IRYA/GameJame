using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeSelector : MonoBehaviour
{
    public GameObject cube;
    public LayerMask layer;
    private Camera _cam;
    private GameObject _cubeSelection;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000f, layer))
            {
                Instantiate(cube, new Vector3(hit.point.x, 1, hit.point.z), Quaternion.identity);
            }


        }
    }
}
