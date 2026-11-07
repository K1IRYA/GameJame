using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "RTS/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("General Info")]
    [Tooltip("Это здание?")]
    public bool IsBuild = false;
    public string unitName = "DWAAAARF";
    public Faction faction = Faction.Player;
    public Sprite portraitIcon;
    [TextArea] public string description = "Добывает ресурсы и строит здания.";


    [Header("Movement")]
    [Tooltip("Скорость передвижения юнита")]
    public float moveSpeed = 4f;
    [Tooltip("Скорость поворота (градусы в секунду)")]
    public float angularSpeed = 360f;
    [Tooltip("Ускорение NavMeshAgent")]
    public float acceleration = 8f;

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Combat (Optional)")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1.2f;

    [Header("Economy / Production")]
    public int Cost = 50;
    public float buildTime = 5f;
}
