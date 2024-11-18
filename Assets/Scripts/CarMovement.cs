using UnityEngine;
using System.Collections.Generic;

public class CarMovement : MonoBehaviour
{
    public Transform dynamicWaypoint = null; // Waypoint dinamico passato alla macchina
    public List<Transform> fixedWaypoints = new List<Transform>(); // Lista dinamica dei waypoint fissi
    public float speed = 5f;

    private CarSpots carSpots;
    private bool dynamicWaypointReached = false; // Flag per controllare se il waypoint dinamico è stato raggiunto
    private List<Transform> originalFixedWaypoints = new List<Transform>(); // Per ripristinare i waypoint all'inizio di un nuovo episodio
    private float waypointReachThreshold = 0.5f; // Distanza minima per considerare il waypoint raggiunto


    private enum MovementState
    {
        WaitingForWaypoint,
        MoveForwardOnly,
        MoveToClosestFixedWaypoint,
        DecideNextWaypoint,
        MoveToDynamicWaypoint,
        Finished
    }

    private MovementState currentState = MovementState.WaitingForWaypoint;
    private float initialForwardDistance = 5f; // Distanza da percorrere solo in avanti
    private float distanceCovered = 0f; // Distanza percorsa in avanti
    private Transform currentTargetWaypoint;

    private void Start()
    {
        // Copia i waypoint originali
        originalFixedWaypoints.AddRange(fixedWaypoints);
    }

    private void Update()
    {

        if (dynamicWaypoint == null || dynamicWaypointReached)
        {
            currentState = MovementState.WaitingForWaypoint;
            return;
        }

        switch (currentState)
        {
            case MovementState.WaitingForWaypoint:
                currentState = MovementState.MoveForwardOnly;
                break;

            case MovementState.MoveForwardOnly:
                MoveForwardOnly();
                break;

            case MovementState.MoveToClosestFixedWaypoint:
                MoveToClosestFixedWaypoint();
                break;

            case MovementState.DecideNextWaypoint:
                DecideNextWaypoint();
                break;

            case MovementState.MoveToDynamicWaypoint:
                MoveToDynamicWaypoint();
                break;

            case MovementState.Finished:
                break;
        }
    }

    private void MoveForwardOnly()
    {
        Vector3 forward = transform.forward;
        transform.position += forward * speed * Time.deltaTime;

        distanceCovered += speed * Time.deltaTime;

        if (distanceCovered >= initialForwardDistance)
        {
            currentState = MovementState.MoveToClosestFixedWaypoint;
            distanceCovered = 0f;
        }
    }

    private void MoveToClosestFixedWaypoint()
    {
        currentTargetWaypoint = GetClosestWaypoint();
        if (currentTargetWaypoint == null)
        {
            currentState = MovementState.DecideNextWaypoint;
            return;
        }

        MoveTowardsTarget(currentTargetWaypoint);

        if (HasReachedTarget(currentTargetWaypoint))
        {
            Debug.Log($"Raggiunto il waypoint fisso: {currentTargetWaypoint.name}");
            currentTargetWaypoint.gameObject.SetActive(false); // Disattiva il waypoint
            fixedWaypoints.Remove(currentTargetWaypoint); // Rimuovi dall'elenco attivo
            currentState = MovementState.DecideNextWaypoint;
        }
    }

    private void DecideNextWaypoint()
    {
        if (dynamicWaypointReached)
        {
            currentState = MovementState.Finished;
            return;
        }

        Transform closestFixedWaypoint = GetClosestWaypoint();
        float distanceToDynamic = Vector3.Distance(transform.position, dynamicWaypoint.position);
        float distanceToClosestFixed = closestFixedWaypoint != null
            ? Vector3.Distance(transform.position, closestFixedWaypoint.position)
            : Mathf.Infinity;

        if (distanceToDynamic < distanceToClosestFixed)
        {
            currentState = MovementState.MoveToDynamicWaypoint;
        }
        else if (closestFixedWaypoint != null)
        {
            currentTargetWaypoint = closestFixedWaypoint;
            currentState = MovementState.MoveToClosestFixedWaypoint;
        }
        else
        {
            currentState = MovementState.Finished;
        }
    }

    private void MoveToDynamicWaypoint()
    {
        MoveTowardsTarget(dynamicWaypoint);

        if (HasReachedTarget(dynamicWaypoint))
        {
            Debug.Log("Raggiunto il waypoint dinamico.");
            currentState = MovementState.Finished;
            dynamicWaypointReached = true;

            carSpots = FindObjectOfType<CarSpots>();
            if (carSpots != null)
            {
                carSpots.RemoveGoalAndRectangleAt(dynamicWaypoint.position);
            }
        }
    }

    private Transform GetClosestWaypoint()
    {
        float closestDistance = Mathf.Infinity;
        Transform closestWaypoint = null;

        foreach (Transform waypoint in fixedWaypoints)
        {
            if (waypoint == null || !waypoint.gameObject.activeSelf) continue;

            float distance = Vector3.Distance(transform.position, waypoint.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestWaypoint = waypoint;
            }
        }

        return closestWaypoint;
    }

    private void MoveTowardsTarget(Transform target)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        transform.position += direction * speed * Time.deltaTime;
    }

    private bool HasReachedTarget(Transform target)
    {
        return Vector3.Distance(transform.position, target.position) < 1.0f;
    }

    public void ResetFixedWaypoints()
    {
        fixedWaypoints.Clear();
        dynamicWaypointReached = false;
        foreach (Transform waypoint in originalFixedWaypoints)
        {
            if (waypoint != null)
            {
                waypoint.gameObject.SetActive(true);
                fixedWaypoints.Add(waypoint);
            }
        }
    }
}
