using UnityEngine;
using UnityEngine.UI;

public class TownHallUIManager : MonoBehaviour
{
    public static TownHallUIManager Instance { get; private set; }

    [Header("Панель меню найма")]
    [SerializeField] private GameObject townHallPanel;

    [Header("Кнопка найма")]
    [SerializeField] private Button trainBuilderButton;

    [Header("Префаб строителя (Dwarf_builder)")]
    [SerializeField] private GameObject builderPrefab;

    [Header("Точка появления (необязательно)")]
    [SerializeField] private Transform customSpawnPoint;
    private BuildingTypeInfo currentSelectedTownHall;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (trainBuilderButton != null)
        {
            trainBuilderButton.onClick.AddListener(OnTrainBuilderClicked);
        }

        SetVisible(false);
    }

    public void SetVisible(bool isVisible, BuildingTypeInfo townHall = null)
    {
        currentSelectedTownHall = townHall;

        if (townHallPanel != null)
        {
            townHallPanel.SetActive(isVisible);
        }
    }

    private void OnTrainBuilderClicked()
    {
        // 1. Проверяем лимит населения
        if (PopulationManager.Instance != null && !PopulationManager.Instance.CanTrainBuilder())
        {
            Debug.LogWarning("[TownHallUI] Достигнут лимит строителей! Постройте дом для расширения лимита.");
            return;
        }

        if (builderPrefab == null)
        {
            Debug.LogError("[TownHallUI] Не назначен builderPrefab в инспекторе!");
            return;
        }

        // 2. Ищем точку спавна на построенной базе
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (currentSelectedTownHall != null)
        {
            // Ищем дочерний объект SpawnPoint внутри построенной базы
            Transform internalSpawnPoint = currentSelectedTownHall.transform.Find("SpawnPoint");
            if (internalSpawnPoint != null)
            {
                spawnPos = internalSpawnPoint.position;
                spawnRot = internalSpawnPoint.rotation;
            }
            else
            {
                // Резерв: вычисляем позицию перед фасадом здания
                spawnPos = currentSelectedTownHall.transform.position + currentSelectedTownHall.transform.forward * 8f;
                spawnRot = currentSelectedTownHall.transform.rotation;
            }
        }
        else if (TownHallRegister.IsBaseRegistered)
        {
            spawnPos = TownHallRegister.BasePosition + Vector3.forward * 8f;
        }

        if (spawnPos == Vector3.zero)
        {
            Debug.LogError("[TownHallUI] Не удалось определить точку спавна у построенной базы!");
            return;
        }

        // Проецируем точку на NavMesh, чтобы рабочий не застревал
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        // 3. Спавним строителя
        GameObject newBuilder = Instantiate(builderPrefab, spawnPos, spawnRot);
        Debug.Log($"[TownHallUI] Строитель нанят у построенной базы в точке: {spawnPos}");

        if (UnitSelectionUIManager.Instance != null)
        {
            UnitSelectionUIManager.Instance.UpdateTotalCount();
        }
    }
}
