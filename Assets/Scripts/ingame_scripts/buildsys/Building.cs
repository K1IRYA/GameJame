using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Building : MonoBehaviour
{
    public GameObject[] builds;
    public bool done = false;

    private bool canPlace = true;
    private Renderer[] ghostRenderers;
    private LayerMask groundLayer;
    private int buildingLayer;
    private BoxCollider autoRootCollider;
    private readonly HashSet<Collider> overlappingColliders = new HashSet<Collider>();

    void Start()
    {
        groundLayer = LayerMask.GetMask("Ground");
        if (groundLayer == 0) groundLayer = ~LayerMask.GetMask("Ignore Raycast");

        buildingLayer = LayerMask.NameToLayer("Building");
        if (buildingLayer == -1) buildingLayer = 0;

        SetLayer(gameObject, 2); // Слой Ignore Raycast на время прицеливания

        if (!GetComponent<Rigidbody>())
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (builds != null && builds.Length > 0 && builds[0] != null)
        {
            // Находим рендереры во всех дочерних объектах составной 3D-модели
            ghostRenderers = builds[0].GetComponentsInChildren<Renderer>(true);
            SetGhostColor(Color.green);
        }

        // Если у модели нет коллайдеров, создаем автоматический BoxCollider
        EnsureColliders();
    }

    void Update()
    {
        if (done) return;

        // Отмена постройки
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Destroy(gameObject);
            return;
        }

        // Поворот постройки на клавиши Q, E или R
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R))
        {
            transform.Rotate(0f, 45f, 0f);
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            transform.Rotate(0f, -45f, 0f);
        }

        // Перемещение за курсором мыши по слою земли
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            transform.position = hit.point;
        }

        // Установка по клику ЛКМ
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(0) && canPlace && !overUI)
        {
            PlaceBuilding();
        }
    }

    private void PlaceBuilding()
    {
        done = true;
        overlappingColliders.Clear();

        if (builds != null && builds.Length >= 2)
        {
            if (builds[0] != null) builds[0].SetActive(false);
            if (builds[1] != null) builds[1].SetActive(true);
        }

        // Если был авто-коллайдер корня, делаем его твердым для готового здания
        if (autoRootCollider != null)
        {
            autoRootCollider.isTrigger = false;
        }
        else
        {
            EnsureSolidColliderOnBuilt();
        }

        SetLayer(gameObject, buildingLayer);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (done || IsIgnored(other)) return;
        overlappingColliders.Add(other);
        UpdatePlacementState();
    }

    private void OnTriggerStay(Collider other)
    {
        if (done || IsIgnored(other)) return;
        if (overlappingColliders.Add(other))
        {
            UpdatePlacementState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (done || IsIgnored(other)) return;
        overlappingColliders.Remove(other);
        UpdatePlacementState();
    }

    private bool IsIgnored(Collider other)
    {
        if (other == null) return true;
        if (other.transform.IsChildOf(transform)) return true; // свои собственные части
        if (other.isTrigger) return true; // чужие триггеры игнорируем

        // Игнорируем столкновения с землей (Ground)
        if (((1 << other.gameObject.layer) & groundLayer) != 0) return true;

        return false;
    }

    private void UpdatePlacementState()
    {
        overlappingColliders.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy);
        canPlace = (overlappingColliders.Count == 0);
        SetGhostColor(canPlace ? Color.green : Color.red);
    }

    private void SetGhostColor(Color color)
    {
        if (ghostRenderers == null) return;

        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            Renderer r = ghostRenderers[i];
            if (r == null) continue;

            Material[] mats = r.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null) continue;

                Color c = color;
                float currentAlpha = mat.HasProperty("_Color") ? mat.color.a : 1f;
                c.a = (currentAlpha < 0.95f && currentAlpha > 0.05f) ? currentAlpha : 0.65f;

                if (mat.HasProperty("_Color"))
                {
                    mat.color = c;
                }
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", c);
                }
            }
        }
    }

    private void EnsureColliders()
    {
        // Проверяем наличие любого коллайдера на объекте
        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        if (allColliders.Length == 0)
        {
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            if (allRenderers.Length > 0)
            {
                Bounds b = allRenderers[0].bounds;
                for (int i = 1; i < allRenderers.Length; i++)
                {
                    b.Encapsulate(allRenderers[i].bounds);
                }

                autoRootCollider = gameObject.AddComponent<BoxCollider>();
                autoRootCollider.isTrigger = true;
                autoRootCollider.center = transform.InverseTransformPoint(b.center) + Vector3.up * 0.1f;
                autoRootCollider.size = new Vector3(b.size.x, Mathf.Max(0.5f, b.size.y - 0.2f), b.size.z);
            }
        }
    }

    private void EnsureSolidColliderOnBuilt()
    {
        if (builds != null && builds.Length >= 2 && builds[1] != null)
        {
            Collider[] colliders = builds[1].GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
            {
                Renderer[] rends = builds[1].GetComponentsInChildren<Renderer>(true);
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++)
                    {
                        b.Encapsulate(rends[i].bounds);
                    }

                    BoxCollider box = builds[1].AddComponent<BoxCollider>();
                    box.isTrigger = false;
                    box.center = builds[1].transform.InverseTransformPoint(b.center);
                    box.size = b.size;
                }
            }
        }
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