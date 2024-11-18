using UnityEngine;

public class PedestrianTrigger : MonoBehaviour
{
    public PedestrianController pedestrian; // Riferimento al pedone
    private CarAgent agent;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // L'agente ha il tag 'Player'
        {
            Debug.Log("Pedone rilevato dal trigger, penalità applicata.");
            agent = FindObjectOfType<CarAgent>();
            // Penalità per contatto con il trigger del pedone
            if (agent != null)
            {
                agent.AddReward(-0.5f); // Applica la penalità
                Debug.Log("Penalità di -0.4 applicata per contatto con il pedone.");
            } else{
                Debug.Log("Agent è null");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) // L'agente ha il tag 'Player'
        {
            Debug.Log("Pedone fuori dal trigger, nessuna azione ulteriore.");
        }
    }
}
