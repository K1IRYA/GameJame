using UnityEngine;
using UnityEngine.EventSystems;

public class Building : MonoBehaviour
{
    public GameObject[] builds;
    public bool done = false;

    private bool canPlace = true;
    private Renderer ghostRenderer;
    private LayerMask groundLayer;
    private int buildingLayer;

    void Start()
    {
        groundLayer = LayerMask.GetMask("Ground");
        if (groundLayer == 0) groundLayer = ~LayerMask.GetMask("Ignore Raycast");

        buildingLayer = LayerMask.NameToLayer("Building");
        if (buildingLayer == -1) buildingLayer = 0;

        SetLayer(gameObject, 2);

        if (!GetComponent<Rigidbody>())
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (builds.Length > 0 && builds[0] != null)
        {
            ghostRenderer = builds[0].GetComponent<Renderer>();
            if (ghostRenderer != null)
            {
                ghostRenderer.material.color = Color.green;
            }
        }
    }

    void Update()
    {
        if (done) return;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Destroy(gameObject);
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            transform.position = hit.point;
        }

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(0) && canPlace && !overUI)
        {
            done = true;
            builds[0].SetActive(false);
            builds[1].SetActive(true);

            SetLayer(gameObject, buildingLayer);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (done) return;
        canPlace = false;
        if (ghostRenderer != null) ghostRenderer.material.color = Color.red;
    }

    private void OnTriggerExit(Collider other)
    {
        if (done) return;
        canPlace = true;
        if (ghostRenderer != null) ghostRenderer.material.color = Color.green;
    }

    private void SetLayer(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayer(child.gameObject, layer);
        }
    }
}
