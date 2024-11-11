using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CarObstacle;

public class CachedCar
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
}

public class CarSpots : MonoBehaviour
{
    [SerializeField]
    private GameObject carGoalPrefab = null;  // Prefab per il goal

    [SerializeField]
    private GameObject yellowRectanglePrefab = null;  // Prefab per il rettangolo giallo

    [SerializeField]
    private int howManyCarsToHide = 1;

    private IEnumerable<CarObstacle> parkedCars;
    private Dictionary<int, CachedCar> cachedParkedCars = new Dictionary<int, CachedCar>();

    public List<Vector3> RemovedCarPositions { get; private set; } = new List<Vector3>();
    public List<Quaternion> RemovedCarRotations { get; private set; } = new List<Quaternion>();

    public GameObject CarGoalPrefab => carGoalPrefab;
    public GameObject YellowRectanglePrefab => yellowRectanglePrefab;
    public CarGoal CarGoal { get; private set; }

    public void Awake()
    {
        if (carGoalPrefab == null)
        {
            Debug.LogError("carGoalPrefab non assegnato in CarSpots!");
        }

        if (yellowRectanglePrefab == null)
        {
            Debug.LogError("yellowRectanglePrefab non assegnato in CarSpots!");
        }

        parkedCars = GetComponentsInChildren<CarObstacle>(true)
            .Where(c => c.CarObstacleTypeValue == CarObstacleType.Car);

        foreach (CarObstacle obstacle in parkedCars)
        {
            cachedParkedCars.Add(obstacle.GetInstanceID(), new CachedCar
            {
                Position = obstacle.transform.position,
                Rotation = obstacle.transform.rotation
            });
        }

        Debug.Log($"Numero di auto parcheggiate trovate: {parkedCars.Count()}");
    }

    private List<int> GetRandomNumsToHideCars(int howMany)
    {
        List<int> carsToHide = new List<int>();

        if (parkedCars == null || !parkedCars.Any())
        {
            Debug.LogError("Nessuna auto parcheggiata trovata per nascondere in CarSpots!");
            return new List<int>();
        }

        while (carsToHide.Count < howMany)
        {
            int carToHide = Random.Range(0, parkedCars.Count());
            if (!carsToHide.Contains(carToHide))
            {
                carsToHide.Add(carToHide);
            }
        }

        Debug.Log($"Auto selezionate per essere nascoste: {carsToHide.Count}");
        return carsToHide;
    }

    public void Setup()
    {
        RemovedCarPositions.Clear();
        RemovedCarRotations.Clear();

        if (parkedCars == null || !parkedCars.Any())
        {
            Debug.LogError("Nessuna auto parcheggiata trovata per la configurazione in Setup!");
            return;
        }

        List<int> carsToHide = GetRandomNumsToHideCars(howManyCarsToHide);
        int carCounter = 0;

        foreach (var car in parkedCars)
        {
            if (cachedParkedCars.ContainsKey(car.GetInstanceID()))
            {
                var cachedParkedCar = cachedParkedCars[car.GetInstanceID()];
                car.GetComponent<Rigidbody>().velocity = Vector3.zero;
                car.transform.SetPositionAndRotation(cachedParkedCar.Position, cachedParkedCar.Rotation);
            }

            if (carsToHide.Contains(carCounter))
            {
                car.gameObject.SetActive(false);
                RemovedCarPositions.Add(car.transform.position);
                RemovedCarRotations.Add(car.transform.rotation);
                Debug.Log($"Auto nascosta alla posizione: {car.transform.position}");
            }
            else
            {
                car.gameObject.SetActive(true);
            }

            carCounter++;
        }
    }

}