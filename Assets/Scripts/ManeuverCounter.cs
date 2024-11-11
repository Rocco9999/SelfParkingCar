using UnityEngine;

public class ManeuverCounter : MonoBehaviour
{
    private CarController carController;
    private CarAgent agent;

    private int maneuverCount = 0;
    private const int maneuverLimit = 2; // Limite di manovre prima della penalità
    private Vector3 lastForwardVector; // Direzione dell'auto al precedente update
    private Vector3 lastForwardVector45;
    private Vector3 reverseStartPosition; // Posizione iniziale della retromarcia

    private float totalRotationSameDirection = 0f;
    private int lastRotationDirection = 0; // -1 per sinistra, +1 per destra, 0 per nessuna rotazione
    private float oppositeRotationAccumulated = 0f;

    private const float directionChangeThreshold = 45f; // Soglia per considerare un cambio di direzione significativo
    private const float minReverseDistance = 1.0f; // Distanza minima per considerare una retromarcia significativa
    private const float penaltyInterval = 0.5f; // Penalità ogni 0.5 secondi
    private float lastPenaltyTime = 0f;

    private bool noParkingDetected = false;

    void Awake()
    {
        carController = GetComponent<CarController>();
        agent = GetComponent<CarAgent>();
        lastForwardVector = transform.forward; // Inizializza la direzione attuale
        lastForwardVector45 = transform.forward; // per i 45 gradi
    }

    void Update()
    {
        TrackManeuver();
        TrackOverFlat();
    }

    public void SetNoParkingDetected(bool noParking)
    {
        noParkingDetected = noParking;
    }


    private void TrackManeuver()
    {
        CarController.Direction currentDirection = carController.CurrentDirection;
        Vector3 currentForwardVector = transform.forward;

        // Calcola l'angolo firmato tra la direzione corrente e quella precedente rispetto all'asse Y (su)
        float signedAngleDifference = Vector3.SignedAngle(lastForwardVector45, currentForwardVector, Vector3.up);
        bool isModerateTurn = Mathf.Abs(signedAngleDifference) >= 45f;

        if (isModerateTurn)
        {
            maneuverCount++;
            Debug.Log("Numero di manovre: " + maneuverCount);

            lastForwardVector45 = currentForwardVector;

            // Penalizza se la macchina effettua manovre senza parcheggi rilevati
            if (noParkingDetected && maneuverCount > maneuverLimit)
            {
                agent.AddReward(-0.1f);
                Debug.Log("Penalità per manovre eccessive senza parcheggi rilevati.");
            }
        }
    }

    private void TrackOverFlat()
    {
        Vector3 currentForwardVector = transform.forward;
        float signedAngleDifference = Vector3.SignedAngle(lastForwardVector, currentForwardVector, Vector3.up);

        // Ignora piccole oscillazioni
        if (Mathf.Abs(signedAngleDifference) < 0.5f)
        {
            signedAngleDifference = 0f;
        }

        // Determina la direzione della rotazione
        int rotationDirection = (signedAngleDifference > 0) ? +1 : -1;
        float angleDifference = Mathf.Abs(signedAngleDifference);

        if (rotationDirection == lastRotationDirection)
        {
            // Stessa direzione, accumula la rotazione
            totalRotationSameDirection += angleDifference;
            oppositeRotationAccumulated = 0f; // Reset dell'accumulo in direzione opposta
        }
        else
        {
            // Condizione limite per evitare oscillazioni continue
            if (oppositeRotationAccumulated + angleDifference <= directionChangeThreshold)
            {
                // Se l'accumulo opposto non supera la soglia, resetta accumuli
                oppositeRotationAccumulated = 0f;
                totalRotationSameDirection = 0f;
            }
            else
            {
                // Direzione opposta con accumulo significativo
                oppositeRotationAccumulated += angleDifference;

                // Cambio di direzione significativo, resetta l'accumulo totale
                if (oppositeRotationAccumulated >= directionChangeThreshold)
                {
                    totalRotationSameDirection = oppositeRotationAccumulated;
                    lastRotationDirection = rotationDirection;
                    oppositeRotationAccumulated = 0f;
                }
            }
        }

        // Controlla se la rotazione totale in un unico senso supera 180 gradi
        if (Mathf.Abs(totalRotationSameDirection) >= 230f)
        {
            agent.TakeAwayPoints(-0.4f);
            Debug.Log("L'auto ha girato su se stessa, episodio terminato");
            totalRotationSameDirection = 0f;
            oppositeRotationAccumulated = 0f;
        }

        // Aggiorna il vettore per il prossimo calcolo
        lastForwardVector = currentForwardVector;
        lastRotationDirection = rotationDirection;
    }


    public void EvaluateManeuverCount(bool isParkedCorrectly)
    {
        // Ricompensa per parcheggio corretto
        if ((maneuverCount <= maneuverLimit) && isParkedCorrectly)
        {
            // Ricompensa per mantenere le manovre sotto il limite
            float reward = 1.2f; // Ricompensa base
            agent.AddReward(reward);
            Debug.Log($"Ricompensa assegnata per mantenere le manovre sotto il limite: {reward}");
            Debug.Log($"Numero di manovre: {maneuverCount}");
        }
        else if (isParkedCorrectly)
        {
            float reward = 0.8f; // Ricompensa extra per parcheggio corretto
            agent.AddReward(reward);
            Debug.Log($"Ricompensa assegnata per parcheggio corretto: {reward}");
            Debug.Log($"Numero di manovre: {maneuverCount}");
        }
    }

    public void ManeuverBackRecall()
    {
        // Verifica se è trascorso abbastanza tempo dall'ultima penalità
        if (Time.time - lastPenaltyTime < penaltyInterval)
        {
            return;
        }

        if (maneuverCount > maneuverLimit + 3)
        {
            agent.TakeAwayPoints(-0.2f); // Penalità per molte manovre
            Debug.Log("Penalità assegnata per troppe manovre");
        }
        else if (maneuverCount > maneuverLimit + 1)
        {
            agent.AddReward(-0.1f); // Penalità per manovre in eccesso
            Debug.Log("Penalità assegnata per manovre in eccesso");
        }
        else if (maneuverCount > maneuverLimit)
        {
            agent.AddReward(-0.05f); // Penalità lieve per superamento lieve del limite
            Debug.Log("Penalità lieve assegnata");
        }

        lastPenaltyTime = Time.time; // Aggiorna il tempo dell'ultima penalità
    }


    public void ResetManeuverCount()
    {
        maneuverCount = 0;
        lastForwardVector = transform.forward;
        lastForwardVector45 = transform.forward;
        totalRotationSameDirection = 0f;
        oppositeRotationAccumulated = 0f;
        lastRotationDirection = 0;
        Debug.Log("Resetto il numero di manovre e la rotazione");
    }
}
