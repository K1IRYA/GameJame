using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class QuickOutline : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.green;
    [SerializeField, Range(0.01f, 0.1f)] private float outlineWidth = 0.03f;

    private Renderer[] modelRenderers;
    private Material outlineMaterial;
    private List<Material[]> originalMaterials = new List<Material[]>();

    private void Awake()
    {
        modelRenderers = GetComponentsInChildren<Renderer>();
        CreateOutlineMaterial();

        // Сохраняем исходные массивы материалов
        foreach (var rend in modelRenderers)
        {
            originalMaterials.Add(rend.sharedMaterials);
        }

        // По умолчанию обводка отключена
        enabled = false;
    }

    private void CreateOutlineMaterial()
    {
        // Базовый шейдер с поддержкой контура или стандартный unlit для совместимости
        Shader shader = Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Sprites/Default");
        outlineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        outlineMaterial.SetColor("_Color", outlineColor);
    }

    private void OnEnable()
    {
        for (int i = 0; i < modelRenderers.Length; i++)
        {
            var rend = modelRenderers[i];
            var mats = new List<Material>(rend.sharedMaterials) { outlineMaterial };
            rend.materials = mats.ToArray();
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] != null && i < originalMaterials.Count)
            {
                modelRenderers[i].materials = originalMaterials[i];
            }
        }
    }

    private void OnDestroy()
    {
        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
        }
    }
}
