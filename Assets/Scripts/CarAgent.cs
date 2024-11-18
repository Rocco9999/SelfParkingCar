using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using static CarController;

public class CarAgent : BaseAgent
{
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private BehaviorParameters behaviorParameters;
    private CarController carController;
    private Rigidbody carControllerRigidBody;
    private CarSpots carSpots;
    private ParkingSpaceDetector parkingSpaceDetector;
    private List<RayPerceptionSensorComponent3D> angledSensors;
    private List<GameObject> placedGoals = new List<GameObject>();
    private List<GameObject> placedYellowRectangle = new List<GameObject>();
    private List<HashSet<(Vector3, int)>> allValidCenters;
    private List<GameObject> allDetectedLineObjects;
    private List<GameObject> allDetectedPedestrian;
    private float timeStationary;
    private float minDistance;
    public Vector3 nearestGoal;
    private const float stationaryThreshold = 1.5f; // Soglia di tempo per considerare l'auto ferma
    private Vector3 lastPosition;
    private float timeSinceLastDistanceCheck = 0f;
    private float totalDistanceCovered = 0f;
    private const float distanceThreshold = 6f; // Soglia di distanza minima
    private const float checkInterval = 2.0f; // Intervallo di 2 secondi per il controllo
    private Direction lastDirection = Direction.Idle; // Direzione precedente
    private int directionForwardCount = 0; // Contatore dei cambi di direzione
    private int directionBackwardCount = 0; // Contatore dei cambi di direzione

    public override void Initialize()
    {
        Debug.Log("Inizializzazione dell'agente CarAgent");

        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        behaviorParameters = GetComponent<BehaviorParameters>();
        carController = GetComponent<CarController>();
        carControllerRigidBody = carController.GetComponent<Rigidbody>();
        carSpots = transform.parent.GetComponentInChildren<CarSpots>();

        parkingSpaceDetector = GetComponent<ParkingSpaceDetector>();
        angledSensors = new List<RayPerceptionSensorComponent3D>();

        foreach (var sensor in GetComponentsInChildren<RayPerceptionSensorComponent3D>())
        {
            if (sensor.SensorName.Contains("TopSensor"))  // o qualsiasi criterio per identificare i sensori inclinati
            {
                angledSensors.Add(sensor);
                Debug.Log("Trovato il top");
            }
        }

        ResetParkingLotArea();
    }
    public enum AgentState
    {
        SearchingForParking, // Fase di ricerca del parcheggio
        Parking, // Fase di parcheggio
        EpisodeEnded         // Stato finale, episodio terminato TEST
    }

    public AgentState currentState;


    public override void OnEpisodeBegin()
    {
        Debug.Log("Inizio di un nuovo episodio");
        timeStationary = 0f; // Reset del timer
        minDistance = float.MaxValue;
        nearestGoal = Vector3.zero;

        // Imposta lo stato iniziale su ricerca parcheggio
        currentState = AgentState.SearchingForParking;

        var maneuverCounter = GetComponent<ManeuverCounter>();
        if (maneuverCounter != null)
        {
            maneuverCounter.ResetManeuverCount();
        }

        allValidCenters = new List<HashSet<(Vector3, int)>>();
        allDetectedLineObjects = new List<GameObject>();
        allDetectedPedestrian = new List<GameObject>();

        timeSinceLastDistanceCheck = 0f;
        totalDistanceCovered = 0f;
        lastPosition = transform.position;
        
        ResetGoalsAndRectangles();
        ResetParkingLotArea();
    }

    private void ResetGoalsAndRectangles()
    {
        // Assicura la distruzione di tutti i goal
        if (placedGoals != null && placedGoals.Count > 0)
        {
            foreach (var goal in placedGoals)
            {
                if (goal != null)
                {
                    Destroy(goal);
                }
            }
            placedGoals.Clear();
        }
        else
        {
            Debug.LogWarning("No goals to reset.");
        }

        // Assicura la distruzione di tutti i rettangoli gialli
        if (placedYellowRectangle != null && placedYellowRectangle.Count > 0)
        {
            foreach (var yellowRectangle in placedYellowRectangle)
            {
                if (yellowRectangle != null)
                {
                    Destroy(yellowRectangle);
                }
            }
            placedYellowRectangle.Clear();
        }
        else
        {
            Debug.LogWarning("No yellow rectangles to reset.");
        }

        // Forza la pulizia delle liste in caso ci siano riferimenti null residui
        placedGoals.RemoveAll(item => item == null);
        placedYellowRectangle.RemoveAll(item => item == null);

        Debug.Log("Reset goals and yellow rectangles complete.");
    }


    private void ResetParkingLotArea()
    {
        Debug.Log("Reset dell'area parcheggio");

        carController.IsAutonomous = true;
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        carControllerRigidBody.velocity = Vector3.zero;
        carControllerRigidBody.angularVelocity = Vector3.zero;
        carSpots.Setup();
        carSpots.ResetEpisodeState();
    }

    void Update()
    {
        if (transform.localPosition.y <= 0)
        {
            Debug.Log("Auto fuori dal piano, penalità");
            TakeAwayPoints(-0.5f);
        }

        var maneuverCounter = GetComponent<ManeuverCounter>();
        if (maneuverCounter != null)
        {
            maneuverCounter.ManeuverBackRecall();
        }
        
        AnalyzeAndPlaceGoalInNearestEmptyParking();
    }

    private void AnalyzeAndPlaceGoalInNearestEmptyParking()
    {
        Debug.Log("Inizio dell'analisi dei parcheggi rettangolari e parallelepipedi");

        List<GameObject> angledLineObjects = parkingSpaceDetector.DetectLineObjectsWithSensors(angledSensors);

        foreach (var lineObject in angledLineObjects)
        {
            if (!allDetectedLineObjects.Contains(lineObject))
            {
                allDetectedLineObjects.Add(lineObject);
            }
        }

        HashSet<(Vector3, int)> rectangleCenters = parkingSpaceDetector.DetectRectangularParkingSpaces(allDetectedLineObjects);

        HashSet<(Vector3, int)> parallelepipedCenters = parkingSpaceDetector.DetectParallelepipedParkingSpaces(allDetectedLineObjects);

        // Aggiungi i centri solo se non sono già presenti in allValidCenters
        if (rectangleCenters.Count > 0 && !allValidCenters.Contains(rectangleCenters))
        {
            allValidCenters.Add(rectangleCenters);
        }

        if (parallelepipedCenters.Count > 0 && !allValidCenters.Contains(parallelepipedCenters))
        {
            allValidCenters.Add(parallelepipedCenters);
        }

        bool parkingDetected = allValidCenters.Count > 0;

        if (parkingDetected)
        {
            PlaceGoalsAtRectangleCenters(allValidCenters);
            Debug.Log("Parcheggio rilevato, cambio stato a 'Parking'.");
        }
    }


    private void PlaceGoalsAtRectangleCenters(List<HashSet<(Vector3, int)>> rectangleCenters, float minDistanceBetweenGoals = 2.0f)
    {
        foreach (var centerSet in rectangleCenters)
        {
            foreach (var (position, angle) in centerSet)
            {
                bool isPositionOccupied = false;

                // Controlla se un goal è già presente nell'intorno del centro attuale
                foreach (var goal in placedGoals)
                {
                    if (goal == null) continue; // Salta i goal già distrutti
                    if (Vector3.Distance(goal.transform.position, position) < minDistanceBetweenGoals)
                    {
                        isPositionOccupied = true;
                        break;
                    }
                }

                // Se non c'è un goal nelle vicinanze, posiziona un nuovo goal e rettangolo giallo
                if (!isPositionOccupied)
                {
                    if (carSpots != null && carSpots.CarGoalPrefab != null && carSpots.YellowRectanglePrefab != null)
                    {
                        // Imposta l'orientamento in base all'angolo: 45 gradi per parallelepipedi, 90 gradi per rettangoli
                        float rotationAngle = (angle == 45) ? 45f : 0f;
                        Quaternion goalRotation = Quaternion.Euler(0, rotationAngle, 0);

                        // Instanzia il goal
                        var newGoal = Instantiate(carSpots.CarGoalPrefab, position + Vector3.up * 0.1f, goalRotation);
                        newGoal.tag = "goal";
                        newGoal.transform.parent = transform.parent;
                        placedGoals.Add(newGoal); // Aggiungi il nuovo goal alla lista

                        // Instanzia il rettangolo giallo
                        var newYellowRectangle = Instantiate(carSpots.YellowRectanglePrefab, new Vector3(position.x, -0.0496f, position.z), goalRotation);
                        newYellowRectangle.tag = "yellowRectangle";
                        newYellowRectangle.transform.parent = transform.parent;
                        placedYellowRectangle.Add(newYellowRectangle);
                    }
                }
                else
                {
                    Debug.Log("Posizione ignorata poiché un goal è già presente nell'intorno.");
                }
            }
        }
        // Rimuovi riferimenti null dalla lista
        placedGoals.RemoveAll(goal => goal == null);
        placedYellowRectangle.RemoveAll(rectangle => rectangle == null);
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("Raccolta osservazioni dell'agente");

        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.rotation);

        if (carSpots != null && carSpots.CarGoal != null)
        {
            sensor.AddObservation(carSpots.CarGoal.transform.position);
            sensor.AddObservation(carSpots.CarGoal.transform.rotation);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Quaternion.identity);
        }

        sensor.AddObservation(carControllerRigidBody.velocity);
    }

    private bool HasDetectedParking()
    {
        // Logica per verificare se un parcheggio è stato rilevato
        return allValidCenters != null && allValidCenters.Count > 0;
    }

    private void FindNearestGoal()
    {
        foreach (var centerSet in allValidCenters)
        {
            foreach (var (position, _) in centerSet)
            {
                float distance = Vector3.Distance(transform.position, position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestGoal = position;
                }
            }
        }
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        if (currentState == AgentState.EpisodeEnded)
        {
            // Interrompe qualsiasi azione quando lo stato è EpisodeEnded
            return;
        }
        var direction = Mathf.FloorToInt(vectorAction[0]);

        // Logica per le due fasi
        if (currentState == AgentState.SearchingForParking)
        {
            // Imposta la direzione solo su "MoveForward" durante la fase di ricerca
            carController.CurrentDirection = Direction.MoveForward;
            carController.IsAutonomous = true;

            if (HasDetectedParking())
            {
                // Ricompensa unica per il rilevamento del parcheggio
                AddReward(0.2f);
                Debug.Log("Ricompensa per rilevamento del parcheggio.");
                FindNearestGoal();
                currentState = AgentState.Parking;
            }
        }
        else if (currentState == AgentState.Parking)
        {
            // Controllo della velocità per rilevare se l'auto è ferma
            if (carController.carRigidBody.velocity.magnitude < 0.2f)
            {
                timeStationary += Time.deltaTime;
                if (timeStationary >= stationaryThreshold)
                {
                    // Penalità per essere rimasti fermi troppo a lungo
                    TakeAwayPoints(-0.2f);
                    Debug.Log("Penalità per essere rimasti fermi per troppo tempo.");
                }
            }
            else
            {
                timeStationary = 0f; // Reset del timer se l'auto si muove
            }

            timeSinceLastDistanceCheck += Time.deltaTime;

            // Calcola la distanza percorsa dall'ultima posizione salvata
            float distanceCovered = Vector3.Distance(transform.position, lastPosition);
            totalDistanceCovered += distanceCovered;
            lastPosition = transform.position;

            // Controllo se l'intervallo di tempo ha raggiunto il limite di 2 secondi
            if (timeSinceLastDistanceCheck >= checkInterval)
            {
                if (totalDistanceCovered < distanceThreshold)
                {
                    TakeAwayPoints(-0.2f);
                    Debug.Log("Penalità per distanza percorsa insufficiente in 2 secondi.");
                }

                // Penalità per troppi cambi di direzione avanti/indietro
                if (directionForwardCount >= 6 && directionBackwardCount >= 6) // Soglia configurabile
                {
                    int totalChanges = directionForwardCount + directionBackwardCount;
                    TakeAwayPoints(-0.05f * totalChanges); // Penalità proporzionale ai cambi di direzione
                    Debug.Log($"Penalità per {totalChanges} cambi di direzione avanti/indietro in 2 secondi.");
                }

                // Reset dei contatori per il prossimo intervallo di 2 secondi
                timeSinceLastDistanceCheck = 0f;
                totalDistanceCovered = 0f;
                directionForwardCount = 0;
                directionBackwardCount = 0;
            }

            // Logica per rilevare i cambi di direzione
            if ((carController.CurrentDirection == Direction.MoveForward && lastDirection != Direction.MoveForward))
            {
                directionForwardCount++;
                Debug.Log($"Cambio di direzione rilevato: {directionForwardCount} cambi finora.");
            }else if ((carController.CurrentDirection == Direction.MoveBackward && lastDirection != Direction.MoveBackward))
            {
                directionBackwardCount++;
                Debug.Log($"Cambio di direzione rilevato: {directionBackwardCount} cambi finora.");
            }

            lastDirection = carController.CurrentDirection;

            // Logica standard per la fase di parcheggio: utilizza il vettore d'azione normalmente
            switch (direction)
            {
                case 0:
                    carController.CurrentDirection = Direction.Idle;
                    break;
                case 1:
                    carController.CurrentDirection = Direction.MoveForward;
                    break;
                case 2:
                    carController.CurrentDirection = Direction.MoveBackward;
                    break;
                case 3:
                    carController.CurrentDirection = Direction.TurnLeft;
                    break;
                case 4:
                    carController.CurrentDirection = Direction.TurnRight;
                    break;
            }

            AddReward(-1f / MaxStep);
        }
    }
   

    public void GivePoints(float amount = 1.0f, bool isFinal = false, bool isAligned = false)
    {
        AddReward(amount);

        var maneuverCounter = GetComponent<ManeuverCounter>();
        if (maneuverCounter != null)
        {
            maneuverCounter.EvaluateManeuverCount(isFinal);
        } else if (isFinal)
        {
            AddReward(0.8f);
        }

        if (isFinal && isAligned)
        {
            currentState = AgentState.EpisodeEnded;
            Debug.Log($"Ricompensa aggiunta: {amount}. Ricompensa cumulativa: {GetCumulativeReward()}");
            StartCoroutine(HandleEpisodeEnd(successMaterial, true));
            EndEpisode();
        } else if (isFinal && !isAligned)
        {
            currentState = AgentState.EpisodeEnded;
            Debug.Log($"Ricompensa aggiunta: {amount}. Ricompensa cumulativa: {GetCumulativeReward()}");
            StartCoroutine(HandleEpisodeEnd(successPartialMaterial, true));
            EndEpisode();
        }
    }

    public void TakeAwayPoints(float amount = -0.01f)
    {
        Debug.Log($"Tolgo punti: {amount}. Ricompensa cumulativa: {GetCumulativeReward()}");
        AddReward(amount);
        currentState = AgentState.EpisodeEnded;
        StartCoroutine(HandleEpisodeEnd(failureMaterial));
        EndEpisode();
    }

    private IEnumerator HandleEpisodeEnd(Material material, bool calledFromGivePoints = false)
    {
        Debug.Log("Episodio terminato");

        if (calledFromGivePoints)
        {
            // Continua a muovere l'auto verso il goal
            FindNearestGoal();
            float elapsedTime = 0f;
            if (nearestGoal != Vector3.zero)
            {
                while (Vector3.Distance(transform.position, nearestGoal) > 1.0f && elapsedTime < 2.0f)
                {
                    elapsedTime += Time.deltaTime;
                    
                    
                }

                if (elapsedTime >= 2.0f)
                {
                    Debug.LogWarning("Timeout raggiunto durante il movimento verso il goal.");
                }
            }
        }

        // Ferma tutti i movimenti dell'auto
        carController.CurrentDirection = Direction.Idle;
        carControllerRigidBody.velocity = Vector3.zero;
        carControllerRigidBody.angularVelocity = Vector3.zero;

        // Cambia il materiale del terreno per indicare successo/fallimento
        if (material != null)
        {
            StartCoroutine(SwapGroundMaterial(material, 0.5f));
        }

        yield return new WaitForSeconds(0.1f); // Attendi un secondo prima del reset
        Level4Detection detection = GetComponent<Level4Detection>();
        if (detection != null)
        {
            detection.FinalizeEpisode();
        }
    }

    public override void Heuristic(float[] actionsOut)
    {
        actionsOut[0] = 0;

        if (Input.GetKey(KeyCode.UpArrow)) actionsOut[0] = 1;
        if (Input.GetKey(KeyCode.DownArrow)) actionsOut[0] = 2;
        if (Input.GetKey(KeyCode.LeftArrow) && carController.canApplyTorque()) actionsOut[0] = 3;
        if (Input.GetKey(KeyCode.RightArrow) && carController.canApplyTorque()) actionsOut[0] = 4;

        Debug.Log("Modalità Heuristic attivata, azioni simulate: " + actionsOut[0]);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collisione rilevata con: " + other.tag);

        switch (other.tag.ToLower())
        {
            case "barrier":
                AddReward(-0.2f);
                Debug.Log("Penalità per collisione con una barriera");
                break;
            case "tree":
                AddReward(-0.1f);
                Debug.Log("Penalità per collisione con un albero");
                break;
            case "grass":
                AddReward(-0.08f);
                Debug.Log("Penalità per movimento sull'erba");
                break;
            default:
                Debug.Log("Collisione con oggetto sconosciuto");
                break;
        }
    }
}
