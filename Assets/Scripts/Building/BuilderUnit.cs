using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BuilderUnit : MonoBehaviour
{
    public bool isSelected = false; // Управляется вашей системой выделения (CubeSelector/RTSController)
    public float buildDistance = 3.5f;
    public float buildSpeed = 20f; // Единиц прогресса в секунду

    private NavMeshAgent agent;
    private ConstructionSite targetSite;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (targetSite == null) return;

        // Если стройка завершена кем-то другим или отменена
        if (targetSite.IsCompleted)
        {
            targetSite = null;
            return;
        }

        // Проверяем дистанцию до объекта стройки
        float distance = Vector3.Distance(transform.position, targetSite.transform.position);

        if (distance <= buildDistance)
        {
            // Подошли — останавливаемся и строим
            agent.isStopped = true;
            targetSite.AddProgress(buildSpeed * Time.deltaTime);
        }
        else
        {
            // Идем к объекту
            agent.isStopped = false;
            agent.SetDestination(targetSite.transform.position);
        }
    }

    public void AssignBuildTask(ConstructionSite site)
    {
        targetSite = site;
        if (agent != null && site != null)
        {
            agent.isStopped = false;
            agent.SetDestination(site.transform.position);
        }
    }
}
