using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class BuildingGhost : MonoBehaviour
{
    [Header("Префаб стройплощадки (ConstructionSite)")]
    public GameObject constructionSitePrefab;

    [Header("Материалы индикации")]
    public Material validMaterial;
    public Material invalidMaterial;

    [Header("Слой земли")]
    public LayerMask groundLayer;

    private List<Renderer> ghostRenderers = new List<Renderer>();
    private HashSet<Collider> overlappingColliders = new HashSet<Collider>();
    private Camera mainCamera;
    private bool canBuild = true;

    // ДОБАВЛЕНО: Ссылка на интерфейс стоимости кнопки
    private BuildingCostUI originCostUI;

    // ДОБАВЛЕНО: Метод инициализации стоимости
    public void SetOriginCostUI(BuildingCostUI costUI)
    {
        originCostUI = costUI;
    }

    private void Awake()
    {
        mainCamera = Camera.main;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        ghostRenderers.AddRange(GetComponentsInChildren<Renderer>());

        Collider[] cols = GetComponentsInChildren<Collider>();
        if (cols.Length == 0)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
        }
        else
        {
            foreach (var col in cols)
            {
                col.isTrigger = true;
            }
        }
    }

    private void Update()
    {
        FollowMouse();
        HandleRotation();

        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(0) && canBuild && !isOverUI)
        {
            PlaceConstructionSite();
        }
    }

    private void FollowMouse()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            transform.position = hit.point;
        }
    }

    private void HandleRotation()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.Rotate(Vector3.up, 45f);
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
        if (other == null) return true;
        if (other.isTrigger) return true;
        if (other.transform.IsChildOf(transform)) return true;
        if (((1 << other.gameObject.layer) & groundLayer) != 0) return true;

        return false;
    }

    private void UpdateVisuals()
    {
        canBuild = (overlappingColliders.Count == 0);
        Material matToApply = canBuild ? validMaterial : invalidMaterial;

        if (matToApply == null) return;

        foreach (var rend in ghostRenderers)
        {
            if (rend == null) continue;
            Material[] mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = matToApply;
            rend.materials = mats;
        }
    }

    private void PlaceConstructionSite()
    {
        // Проверяем стоимость: если кнопка была привязана, пытаемся списать ресурсы
        if (originCostUI != null)
        {
            bool success = originCostUI.TrySpendResources();
            if (!success)
            {
                Debug.LogWarning("[BuildingGhost] Недостаточно ресурсов для списания фундамента!");
                return; // Прерываем установку, если ресурсов на складе уже нет
            }
        }

        if (constructionSitePrefab != null)
        {
            GameObject siteObj = Instantiate(constructionSitePrefab, transform.position, transform.rotation);
            ConstructionSite site = siteObj.GetComponent<ConstructionSite>();

            if (site != null)
            {
                // Очищенный от решёток вызов для стабильности версий
                BuilderUnit[] builders = FindObjectsOfType<BuilderUnit>();
                foreach (var builder in builders)
                {
                    if (builder.IsSelected)
                    {
                        builder.AssignBuildTask(site);
                        break;
                    }
                }
            }
        }

        if (BuildSystem.Instance != null)
        {
            BuildSystem.Instance.ClearCurrentGhost();
        }

        Destroy(gameObject);
    }
}
