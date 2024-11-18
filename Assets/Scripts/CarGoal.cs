using UnityEngine;

public class CarGoal : MonoBehaviour
{
    private CarAgent agent = null;

    [SerializeField]
    private GoalType goalType = GoalType.Milestone;

    [SerializeField]
    private float goalReward = 0.8f;

    [SerializeField]
    private bool enforceGoalMinRotation = false;

    [SerializeField]
    private float goalMinRotation = 10.0f;

    // to avoid AI from cheating ;)
    public bool HasCarUsedIt { get; set; } = false;

    public enum GoalType
    {
        Milestone,
        FinalDestination
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.transform.tag.ToLower() == "player" && !HasCarUsedIt)
        {
            agent = transform.parent.GetComponentInChildren<CarAgent>();
            Vector3 goalPosition = transform.position;
            float distanceBetweenGoal = Vector3.Distance(goalPosition, agent.nearestGoal);

            // Calcola e applica la penalità basata sulla distanza Test per il livello 3
            float penalty = CalculatePenalty(distanceBetweenGoal);
            agent.AddReward(penalty);

            if (penalty < 0f)
            {
                Debug.Log($"Penalità applicata: {penalty} perchè la distanza è: {distanceBetweenGoal}");
            }
            else
            {
                Debug.Log("Parcheggio corretto, bonus applicato di:" + penalty);
            }

            if (goalType == GoalType.Milestone)
            {
                HasCarUsedIt = true;
                agent.GivePoints(goalReward);
                Debug.Log("Sono una milestone");
            }
            else
            {
                float isProperlyAligned = IsCarProperlyAligned(goalPosition, agent.transform, goalMinRotation);
                // this will ensure the car tries to align when parking
                if(isProperlyAligned <= goalMinRotation || !enforceGoalMinRotation)
                {
                    HasCarUsedIt = true;
                    // Bonus per parcheggio perfetto
                    if (isProperlyAligned <= 5f)
                    {
                        agent.GivePoints(goalReward + 0.5f, true, true);
                    } else {
                        agent.GivePoints(goalReward, true, true);
                    }
                }
                else
                {
                    if(isProperlyAligned > 45f){
                        agent.GivePoints((goalReward - 1.5f), true, false);
                    }else
                    {
                        float alignmentPenalty = Mathf.Clamp01((isProperlyAligned - 15f) / (45f - 15f));
                        float alignmentReward = Mathf.Lerp(goalReward -0.2f, goalReward - 1f, alignmentPenalty); // Ricompensa decrescente
                        Debug.Log("Non ho parcheggiato in modo da essere allineato. Penalità:" + alignmentReward);
                        agent.GivePoints(alignmentReward, true, false);
                    }
                }
            }
        }
    }

    private float CalculatePenalty(float distance)
    {
        float closeDistanceThreshold = 4f;
        float maxPenaltyDistance = 30f;
        float maxPenalty = -1.0f;

        // Bonus se parcheggia dove deve parcheggiare
        if (distance <= 0.5f)
        {
            return 0.2f;
        }

        // Penalità lieve per parcheggi adiacenti
        if (distance <= closeDistanceThreshold)
        {
            return -0.1f;
        }

        // Penalità crescente per parcheggi più lontani
        float normalizedDistance = Mathf.Clamp01((distance - closeDistanceThreshold) / (maxPenaltyDistance - closeDistanceThreshold));
        return Mathf.Lerp(-0.1f, maxPenalty, normalizedDistance); //Questa funzione Lerp restituisce minimo 0.1 e massimo 0.5
    }

    float IsCarProperlyAligned(Vector3 goalPosition, Transform carTransform, float angleThreshold)
    {
       // Direzione della macchina (vettore forward) nel piano XZ
        Vector3 carForward = carTransform.forward;
        carForward.y = 0; // Ignora l'asse Y
        carForward.Normalize();

        // Direzione dal goal alla macchina (vettore GoalToCar) nel piano XZ
        Vector3 goalToCar = goalPosition - carTransform.position;
        goalToCar.y = 0; // Ignora l'asse Y
        goalToCar.Normalize();

        // Calcolo dell'angolo tra i due vettori
        float angle = Mathf.Min(Vector3.Angle(carForward, goalToCar), Vector3.Angle(-carForward, goalToCar));

        return angle;
    }


}