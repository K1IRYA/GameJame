using UnityEngine;

public class BuildingTypeInfo : MonoBehaviour
{
    public enum BuildingType { TownHall, Tower, Wall, OtherBuilding }

    [SerializeField] private BuildingType buildingType;
    public BuildingType Type => buildingType;

    [Header("Данные постройки (ScriptableObject)")]
    [SerializeField] private UnitData buildingData;
    public UnitData Data => buildingData;
}
