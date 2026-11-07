using UnityEngine;

public class EnemyHighlight : MonoBehaviour
{
    [Header("Кольцо под ногами врага")]
    [Tooltip("Дочерний объект с MeshRenderer/Projector кольца")]
    [SerializeField] private GameObject highlightRing;

    [Header("Красный материал")]
    [SerializeField] private Material redHighlightMaterial;

    [Header("Режим отображения")]
    [Tooltip("Показывать всегда или только при наведении курсора мыши")]
    [SerializeField] private bool alwaysVisible = true;

    private Renderer ringRenderer;

    private void Awake()
    {
        // Если объект кольца не перетащили вручную, ищем дочерний объект с похожим именем
        if (highlightRing == null)
        {
            Transform t = transform.Find("SelectionCircle") ?? transform.Find("SelectRing");
            if (t != null) highlightRing = t.gameObject;
        }

        if (highlightRing != null)
        {
            ringRenderer = highlightRing.GetComponent<Renderer>();
            if (ringRenderer != null && redHighlightMaterial != null)
            {
                ringRenderer.material = redHighlightMaterial;
            }

            highlightRing.SetActive(alwaysVisible);
        }
    }

    // Если нужно включать подсветку при наведении мыши (как в RTS)
    private void OnMouseEnter()
    {
        if (!alwaysVisible && highlightRing != null)
        {
            highlightRing.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        if (!alwaysVisible && highlightRing != null)
        {
            highlightRing.SetActive(false);
        }
    }

    public void SetHighlight(bool active)
    {
        if (highlightRing != null)
        {
            highlightRing.SetActive(active);
        }
    }
}
