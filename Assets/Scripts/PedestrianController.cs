using UnityEngine;

public class PedestrianController : MonoBehaviour
{
    public Transform[] waypoints; // Array di waypoints
    private int currentWaypointIndex = 0; // Indice del waypoint attuale
    public float speed = 2f; // Velocità del movimento
    public float stopDistance = 0.5f; // Distanza per considerare raggiunto il waypoint
    private bool isReversing = false; // Flag per il movimento inverso

    void Update()
    {
        if (waypoints.Length == 0) return; // Esci se non ci sono waypoint

        // Ottieni il waypoint attuale
        Transform target = waypoints[currentWaypointIndex];
        Vector3 direction = (target.position - transform.position).normalized;

        // Muovi l'agente verso il waypoint
        transform.position += direction * speed * Time.deltaTime;

        // Controlla se ha raggiunto il waypoint
        if (Vector3.Distance(transform.position, target.position) < stopDistance)
        {
            // Se non stai tornando indietro
            if (!isReversing)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Length)
                {
                    isReversing = true; // Cambia direzione
                    currentWaypointIndex -= 2; // Torna al waypoint precedente
                }
            }
            else
            {
                currentWaypointIndex--;
                if (currentWaypointIndex < 0)
                {
                    isReversing = false; // Torna al movimento in avanti
                    currentWaypointIndex = 1; // Vai al secondo waypoint
                }
            }
        }
    }
}
