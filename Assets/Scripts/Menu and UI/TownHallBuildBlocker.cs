using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TownHallBuildBlocker : MonoBehaviour
{
    private Button buildButton;

    private void Awake()
    {
        buildButton = GetComponent<Button>();
    }

    private void Update()
    {
        // Проверяем наличие готового хаба или его стройплощадки
        bool isBlocked = IsTownHallOrSiteExists();

        if (buildButton.interactable == isBlocked)
        {
            buildButton.interactable = !isBlocked;
        }
    }

    private bool IsTownHallOrSiteExists()
    {
        // 1. Проверяем готовый хаб
        if (TownHallRegister.IsBaseRegistered) return true;

        BuildingTypeInfo[] allBuildings = Object.FindObjectsByType<BuildingTypeInfo>(FindObjectsSortMode.None);
        foreach (var b in allBuildings)
        {
            if (b != null && b.Type == BuildingTypeInfo.BuildingType.TownHall)
                return true;
        }

        // 2. Проверяем наличие стройплощадки хаба
        ConstructionSite[] sites = Object.FindObjectsByType<ConstructionSite>(FindObjectsSortMode.None);
        foreach (var site in sites)
        {
            if (site != null && !site.IsCompleted)
            {
                BuildingTypeInfo info = site.GetComponent<BuildingTypeInfo>() ?? site.GetComponentInChildren<BuildingTypeInfo>();
                if (info != null && info.Type == BuildingTypeInfo.BuildingType.TownHall)
                    return true;

                if (site.name.ToLower().Contains("base") || site.name.ToLower().Contains("townhall"))
                    return true;
            }
        }

        return false;
    }
}
