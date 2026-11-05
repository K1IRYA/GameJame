using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class BuildingGhost : MonoBehaviour
{
    [Header("Префаб стройплощадки (ConstructionSite)")]
    public GameObject constructionSitePrefab;

    [Header("Материалы")]
    public Material validMaterial;
    public Material invalidMaterial;
    public LayerMask groundLayer;

    private List<Renderer> ghostRenderers = new List<Renderer>();
    private HashSet<Collider> overlappingColliders = new HashSet<Collider>();
    private Camera mainCamera;
    private bool canBuild = true;

    private void Awake()
    {
        mainCamera = Camera.main;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        ghostRenderers.AddRange(GetComponentsInChildren<Renderer>());
        foreach (var col in GetComponentsInChildren<Collider>()) col.isTrigger = true;
    }

    private void Update()
    {
        FollowMouse();

        if (Input.GetKeyDown(KeyCode.R))
            transform.Rotate(Vector3.up, 45f);

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(0) && canBuild && !overUI)
        {
            PlaceConstructionSite();
        }
    }

    private void FollowMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            transform.position = hit.point;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsIgnored(other)) return;
        overlappingColliders.Add(other);
        UpdateVisuals();
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsIgnored(other)) return;
        overlappingColliders.Remove(other);
        UpdateVisuals();
    }

    private bool IsIgnored(Collider other)
    {
        if (other == null || other.isTrigger || other.transform.IsChildOf(transform)) return true;
        if (((1 << other.gameObject.layer) & groundLayer) != 0) return true;
        return false;
    }

    private void UpdateVisuals()
    {
        canBuild = (overlappingColliders.Count == 0);
        Material mat = canBuild ? validMaterial : invalidMaterial;
        if (mat == null) return;

        foreach (var r in ghostRenderers)
        {
            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.materials = mats;
        }
    }

    private void PlaceConstructionSite()
    {
        if (constructionSitePrefab != null)
        {
            GameObject siteObj = Instantiate(constructionSitePrefab, transform.position, transform.rotation);
            ConstructionSite site = siteObj.GetComponent<ConstructionSite>();

            // Отправляем выбранного рабочего на стройку
            SendSelectedBuilderTo(site);
        }

        if (BuildSystem.Instance != null)
            BuildSystem.Instance.ClearCurrentGhost();

        Destroy(gameObject);
    }

    private void SendSelectedBuilderTo(ConstructionSite site)
    {
        // Ищем активного строителя среди выделенных юнитов
        BuilderUnit[] allBuilders = FindObjectsOfType<BuilderUnit>();
        foreach (var builder in allBuilders)
        {
            if (builder.isSelected)
            {
                builder.AssignBuildTask(site);
                break; // Отправляем одного рабочего (или уберите break, если строить могут сразу несколько)
            }
        }
    }
}
