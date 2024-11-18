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
    private GameObject waypointPrefab = null; // Prefab per il waypoint delle auto

    [SerializeField]
    private int howManyCarsToHide = 1;

    private IEnumerable<CarObstacle> parkedCars;
    private Dictionary<int, CachedCar> cachedParkedCars = new Dictionary<int, CachedCar>();

    public List<Vector3> RemovedCarPositions { get; private set; } = new List<Vector3>();
    public List<Quaternion> RemovedCarRotations { get; private set; } = new List<Quaternion>();

    public GameObject CarGoalPrefab => carGoalPrefab;
    public GameObject YellowRectanglePrefab => yellowRectanglePrefab;
    public GameObject WaypointPrefab => waypointPrefab;
    public CarGoal CarGoal { get; private set; }

    public bool autoChanger = false;

    private List<GameObject> activeWaypoints = new List<GameObject>();
    private List<GameObject> activeMovingCars = new List<GameObject>();
    private bool carMovementAssigned = false;
    private GameObject selectedCar;

    private CarMovement carMovementClass;

    public void Awake()
    {
        if (carGoalPrefab == null || yellowRectanglePrefab == null || waypointPrefab == null)
        {
            Debug.LogError("Prefab non assegnati correttamente in CarSpots!");
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

    public void ResetEpisodeState()
    {
        carMovementAssigned = false; // Reset del flag
        selectedCar = null;          // Reset della macchina selezionata
        ClearWaypoints();            // Pulisce i waypoints
        Debug.Log("Stato episodio resettato.");
    }

    private List<int> GetRandomNumsToHideCars(int howMany)
    {
        List<int> carsToHide = new List<int>();

        if (parkedCars == null || !parkedCars.Any())
        {
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
            // Ricarica parkedCars ogni volta che viene chiamato Setup()
            parkedCars = GetComponentsInChildren<CarObstacle>(true)
                .Where(c => c.CarObstacleTypeValue == CarObstacleType.Car);
                
                if (parkedCars == null || !parkedCars.Any())
                {
                    Debug.LogError("Nessuna auto parcheggiata trovata per la configurazione in Setup!");
                    return;
                }
        }

        int howManyCarsToHideRandom = Random.Range(2, howManyCarsToHide);

        List<int> carsToHide = GetRandomNumsToHideCars(howManyCarsToHideRandom);
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
            }
            else
            {
                car.gameObject.SetActive(true);
            }

            carCounter++;
        }
        // Avvia le funzioni successive con un ritardo
        if(autoChanger)
        {
            Invoke(nameof(AssignCarMovementAndWaypoint), 0.3f);
        }
    }

    private void AssignCarMovementAndWaypoint()
    {
        if (carMovementAssigned) return; // Evita esecuzioni multiple
        carMovementAssigned = true;
        // Rimuovi CarMovement da tutte le auto attive
        foreach (var car in parkedCars)
        {
            if (car.gameObject.activeSelf)
            {
                var carMovement = car.GetComponent<CarMovement>();
                if (carMovement != null)
                {
                    carMovement.ResetFixedWaypoints();
                    carMovement.enabled = false;
                    Debug.Log($"Disabilitato CarMovement da: {car.name}");
                }
            }
        }

        // Seleziona un'auto attiva casualmente
        var activeCars = parkedCars.Where(car => car.gameObject.activeSelf).ToList();
        Debug.Log("Auto attive:" + activeCars.Count);
        if (activeCars.Count > 0)
        {
            int randomIndex = Random.Range(0, activeCars.Count);
            selectedCar = activeCars[randomIndex].gameObject;
            Debug.Log("Auto selezionata:" + randomIndex);

            // Assegna CarMovement
            var carMovement = selectedCar.GetComponent<CarMovement>();
            if (carMovement != null)
            {
                carMovement.enabled = true; // Abilita il componente
                Debug.Log($"Abilitato CarMovement alla macchina: {selectedCar.name}");
            }
            else
            {
                Debug.LogError($"CarMovement non trovato su: {selectedCar.name}");
            }
        }

        // Assegna un waypoint a una posizione vuota casuale
        if (RemovedCarPositions.Count > 0)
        {
            int randomIndex = Random.Range(0, RemovedCarPositions.Count);
            Vector3 waypointPosition = RemovedCarPositions[randomIndex];

            waypointPosition.z += 1.20f;

            // Crea il waypoint
            GameObject waypoint = Instantiate(waypointPrefab, waypointPosition, Quaternion.identity);
            activeWaypoints.Add(waypoint);
            var carMovement = selectedCar.GetComponent<CarMovement>();
            if (carMovement != null)
            {
                carMovement.dynamicWaypoint = waypoint.transform;
                
            }
            Debug.Log($"Waypoint creato in posizione: {waypointPosition}");

            // Rimuovi la posizione dalla lista (opzionale)
            RemovedCarPositions.RemoveAt(randomIndex);
        }
    }

    public void RemoveGoalAndRectangleAt(Vector3 position)
    {
        // Raggio di controllo per l'overlap
        float overlapRadius = 1.0f;

        // Trova tutti gli oggetti nel raggio specificato
        Collider[] colliders = Physics.OverlapSphere(position, overlapRadius);

        foreach (Collider collider in colliders)
        {
            GameObject obj = collider.gameObject;

            // Controlla se l'oggetto ha il tag "goal" e disattivalo
            if (obj.CompareTag("goal"))
            {
                obj.SetActive(false);
                Debug.Log($"Disattivato goal in posizione: {obj.transform.position}");
            }

            // Controlla se l'oggetto ha il tag "yellowRectangle" e disattivalo
            if (obj.CompareTag("yellowRectangle"))
            {
                obj.SetActive(false);
                Debug.Log($"Disattivato rettangolo giallo in posizione: {obj.transform.position}");
            }
        }

        Debug.Log($"Disattivati tutti i goals e i rettangoli gialli sovrapposti a posizione: {position}");
    }


    private void ClearWaypoints()
    {
        foreach (var waypoint in activeWaypoints)
        {
            if (waypoint != null)
            {
                Destroy(waypoint);
            }
        }

        activeWaypoints.Clear();
    }

}