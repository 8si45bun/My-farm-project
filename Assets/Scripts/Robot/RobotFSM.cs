using UnityEngine;
using UnityEngine.AI;


public class RobotFSM : MonoBehaviour
{
    private Transform target;
    private NavMeshAgent navMeshAgent;

    public void Setup(Transform target)
    {
        this.target = target;

        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;
    }

    private void Update()
    {
        navMeshAgent.SetDestination(target.position);
    }

}

