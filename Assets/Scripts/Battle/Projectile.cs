using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Настройки полета")]
    [SerializeField] private float speed = 15f;

    private Health target;
    private float damage;

    // Метод инициализации, который вызывается из RangedAttack
    public void MoveTo(Health targetEnemy, float damageValue)
    {
        target = targetEnemy;
        damage = damageValue;
    }

    private void Update()
    {
        // Если цель погибла или пропала, пока снаряд летел, уничтожаем его
        if (target == null || target.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        // Летим в центр врага (приподнимаем точку попадания на 0.5 метра, чтобы снаряд не летел в ноги)
        Vector3 targetPoint = target.transform.position + Vector3.up * 0.5f;
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

        // Поворачиваем снаряд по направлению полета для визуала
        Vector3 moveDir = (targetPoint - transform.position).normalized;
        if (moveDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDir);
        }

        // Дистанция попадания
        if (Vector3.Distance(transform.position, targetPoint) < 0.2f)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
