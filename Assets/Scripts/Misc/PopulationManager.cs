using UnityEngine;
using System;

public class PopulationManager : MonoBehaviour
{
    public static PopulationManager Instance { get; private set; }

    public int MaxBuilders { get; private set; } = 0;
    public int CurrentBuilders { get; private set; } = 0;

    public static event Action<int, int> OnPopulationChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void AddMaxBuilders(int amount)
    {
        MaxBuilders += amount;
        NotifyChange();
        Debug.Log($"[PopulationManager] +{amount} к лимиту. Доступно мест: {CurrentBuilders}/{MaxBuilders}");
    }

    public void RemoveMaxBuilders(int amount)
    {
        MaxBuilders = Mathf.Max(0, MaxBuilders - amount);
        NotifyChange();
        Debug.Log($"[PopulationManager] -{amount} от лимита. Доступно мест: {CurrentBuilders}/{MaxBuilders}");
    }

    public bool CanTrainBuilder()
    {
        return CurrentBuilders < MaxBuilders;
    }

    public void RegisterBuilder()
    {
        CurrentBuilders++;
        NotifyChange();
        Debug.Log($"[PopulationManager] Зарегистрирован строитель. Текущее население: {CurrentBuilders}/{MaxBuilders}");
    }

    public void UnregisterBuilder()
    {
        CurrentBuilders = Mathf.Max(0, CurrentBuilders - 1);
        NotifyChange();
        Debug.Log($"[PopulationManager] Строитель погиб. Текущее население: {CurrentBuilders}/{MaxBuilders}");
    }

    private void NotifyChange()
    {
        OnPopulationChanged?.Invoke(CurrentBuilders, MaxBuilders);
    }
}