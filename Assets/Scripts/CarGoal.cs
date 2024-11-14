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

            if (distanceBetweenGoal > 0.5f)
            {
                agent.AddReward(-0.4f);
                Debug.Log("Ho sbagliato il punto di arrivo, non ho scelto il goal più vicino");
            }

            if (goalType == GoalType.Milestone)
            {
                HasCarUsedIt = true;
                agent.GivePoints(goalReward);
            }
            else
            {
                // this will ensure the car tries to align when parking
                if(Mathf.Abs(agent.transform.rotation.y) <= goalMinRotation || !enforceGoalMinRotation)
                {
                    HasCarUsedIt = true;
                    agent.GivePoints(goalReward, true);
                }
                else
                {
                    Debug.Log("Non ho parcheggiato in modo da essere allineato");
                    agent.TakeAwayPoints();
                }
            }
        }
    }
}