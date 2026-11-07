using UnityEngine;

public class TownHallRegister : MonoBehaviour
{
    // Глобальная статическая ссылка на координаты Ратуши, доступная из любого скрипта проекта
    public static Vector3 BasePosition { get; private set; } = Vector3.zero;
    public static Health BaseHealth { get; private set; } = null;
    public static bool IsBaseRegistered => BaseHealth != null;

    private void Awake()
    {
        // Намертво запоминаем позицию Ратуши при старте сцены
        BasePosition = transform.position;

        // Кэшируем компонент здоровья Ратуши
        BaseHealth = GetComponent<Health>() ?? GetComponentInChildren<Health>() ?? gameObject.AddComponent<Health>();

        Debug.Log($"[TownHallRegister] Ратуша успешно зарегистрирована в системе! Координаты: {BasePosition}");
    }
}
