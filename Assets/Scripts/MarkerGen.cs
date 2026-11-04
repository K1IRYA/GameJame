using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MarkerGen : MonoBehaviour
{
    private void Awake()
    {
        // Настраиваем внешний круг
        LineRenderer outerCircle = GetComponent<LineRenderer>();
        SetupCircle(outerCircle, 0.6f, 0.06f, Color.green);

        // Создаем дочерний объект для центральной точки
        GameObject dotObj = new GameObject("CenterDot");
        dotObj.transform.SetParent(transform);
        dotObj.transform.localPosition = Vector3.zero;

        // Настраиваем точку внутри
        LineRenderer centerDot = dotObj.AddComponent<LineRenderer>();
        SetupCircle(centerDot, 0.08f, 0.16f, Color.green);
    }

    private void SetupCircle(LineRenderer lr, float radius, float width, Color color)
    {
        int segments = 36;
        lr.positionCount = segments + 1;
        lr.useWorldSpace = false;

        // Принудительно устанавливаем правильную толщину линии в коде
        lr.startWidth = width;
        lr.endWidth = width;

        // Исправленный поиск шейдера: используем универсальный спрайтовый шейдер,
        // который работает и в Built-in, и в URP, предотвращая розовый экран.
        Shader shader = Shader.Find("Sprites/Default");
        Material mat = new Material(shader);

        // Включаем поддержку цвета
        mat.color = color;
        lr.material = mat;

        lr.startColor = color;
        lr.endColor = color;

        // Высчитываем точки по окружности
        float deltaTheta = (2f * Mathf.PI) / segments;
        float theta = 0f;

        for (int i = 0; i <= segments; i++)
        {
            float x = radius * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(theta);
            lr.SetPosition(i, new Vector3(x, 0f, z));
            theta += deltaTheta;
        }
    }
}
