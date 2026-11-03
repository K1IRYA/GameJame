using UnityEngine;
using UnityEngine.EventSystems;

public class Building : MonoBehaviour
{
    // [0] = PreBuild (призрак), [1] = Build (настоящее здание)
    public GameObject[] builds;

    [Header("Слой земли (Ground)")]
    public LayerMask groundLayer;

    public bool done = false;
    private bool canPlace = true;
    private Renderer ghostRenderer;

    void Start()
    {
        // Если слой не выбран руками в инспекторе, ищем слой "Ground"
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
            if (groundLayer == 0) groundLayer = ~LayerMask.GetMask("Ignore Raycast");
        }

        // Переводим кубик в слой 2 (Ignore Raycast), чтобы луч мыши не попадал в сам кубик и не тянул его в экран!
        SetLayerRecursively(gameObject, 2);

        // Находим рендерер призрака и сразу красим его в зеленый цвет
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
        // Если уже построено — ничего не делаем
        if (done) return;

        // Отмена стройки по правой кнопке мыши или Escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Destroy(gameObject);
            return;
        }

        // Пускаем луч ТОЛЬКО в землю (игнорируя сам кубик)
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            transform.position = hit.point;
        }

        // Клик ЛКМ: строим, если место свободно и мышка не на UI
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(0) && canPlace && !overUI)
        {
            done = true;
            builds[0].SetActive(false); // выключаем призрака
            builds[1].SetActive(true);  // включаем настоящий дом

            // Возвращаем обычный слой (Default) построенному зданию
            SetLayerRecursively(gameObject, 0);
        }
    }

    // Если наехали на другое здание — красим в красный и запрещаем строить
    private void OnTriggerStay(Collider other)
    {
        if (done) return;
        canPlace = false;
        if (ghostRenderer != null) ghostRenderer.material.color = Color.red;
    }

    // Если вышли из чужого коллайдера — возвращаем зеленый
    private void OnTriggerExit(Collider other)
    {
        if (done) return;
        canPlace = true;
        if (ghostRenderer != null) ghostRenderer.material.color = Color.green;
    }

    // Меняет слой у объекта и всех его дочерних элементов
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
