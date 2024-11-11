using UnityEngine;

public class CarObstacle : MonoBehaviour
{
    public enum CarObstacleType
    { 
        Barrier,
        Tree,
        Car,
        Ground,
        Grass
    }

    [SerializeField]
    private CarObstacleType carObstacleType = CarObstacleType.Barrier;

    public CarObstacleType CarObstacleTypeValue { get { return this.carObstacleType; } }

    private CarAgent agent = null;

    void Awake()
    {
        // cache agent
        agent = transform.parent.parent.GetComponentInChildren<CarAgent>();
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.transform.tag.ToLower() == "player")
        {
            agent.TakeAwayPoints();
        }
    }

    void OnCollisionEnter(Collision other) 
    {
        if (other.transform.tag.ToLower() == "player")
        {
            agent.TakeAwayPoints(-0.25f);
            Debug.Log("Collided with: " + other.transform.tag.ToLower());
        }
    }
}
