using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;

public class Level4Detection : MonoBehaviour
{
    public LayerMask layerMask; // Layer per il rilevamento
    public int raysPerDirection; // Numero di raggi per lato
    public float maxRayDegrees; // Massimo angolo dei raggi
    public float sphereCastRadius = 0.5f; // Raggio dello SphereCast
    public bool rayVisible = false; // Visualizza i raggi
    private RayPerceptionSensorComponent3D straightSensor;
    private CarAgent agent = null;

    private HashSet<GameObject> detectedPedestrians = new HashSet<GameObject>(); // Pedoni rilevati
    private Dictionary<GameObject, float> pedestrianDistanceSum = new Dictionary<GameObject, float>(); // Somma delle distanze per pedone
    private Dictionary<GameObject, int> pedestrianDetectionCount = new Dictionary<GameObject, int>(); // Numero di rilevamenti per pedone

    private bool hasCriticalDistance = false; // Flag per sapere se la distanza è mai scesa sotto 7
    private float criticalDistanceSum = 0f; // Somma delle distanze critiche (<7)
    private int criticalDistanceCount = 0; // Numero di rilevamenti con distanza <7

    public void Start()
    {
        // Trova il sensore "Pivot"
        foreach (var sensor in GetComponentsInChildren<RayPerceptionSensorComponent3D>())
        {
            if (sensor.SensorName.Contains("PivotSensor")) // Identifica il sensore Pivot
            {
                straightSensor = sensor;
                Debug.Log("Trovato il sensore Pivot");
                break;
            }
        }

        if (straightSensor == null)
        {
            Debug.LogError("Nessun sensore Pivot trovato!");
        }

        agent = transform.parent.GetComponentInChildren<CarAgent>();
    }

    public void OnEpisodeBegin()
    {
        // Resetta tutti i dati relativi ai pedoni
        detectedPedestrians.Clear();
        pedestrianDistanceSum.Clear();
        pedestrianDetectionCount.Clear();
        hasCriticalDistance = false;
        criticalDistanceSum = 0f;
        criticalDistanceCount = 0;
        Debug.Log("Reset dei dati relativi ai pedoni completato.");
    }

    public List<GameObject> DetectObjectsWithSensors(RayPerceptionSensorComponent3D sensor)
    {
        List<GameObject> pedestrians = new List<GameObject>();
        Debug.Log("Inizio rilevamento pedoni tramite sensore Pivot");

        float rayLength = sensor.RayLength; // Lunghezza dei raggi
        float angleStep = maxRayDegrees / raysPerDirection; // Calcolo dell'angolo tra i raggi

        float startVerticalOffset = straightSensor.StartVerticalOffset;
        float endVerticalOffset = straightSensor.EndVerticalOffset;

        for (int i = -raysPerDirection; i <= raysPerDirection; i++)
        {
            // Calcola la direzione del raggio
            Quaternion rotation = Quaternion.Euler(0, angleStep * i, 0);
            Vector3 rayDirection = rotation * sensor.transform.forward;

            // Calcola l'inizio e la fine del raggio usando gli offset verticali
            Vector3 rayStart = sensor.transform.position + Vector3.up * startVerticalOffset;
            Vector3 rayEnd = sensor.transform.position + rayDirection * rayLength + Vector3.up * endVerticalOffset;

            // Disegna il raggio con Debug.DrawLine per visualizzare il raggio
            if (rayVisible)
            {
                Debug.DrawLine(rayStart, rayEnd, Color.green);
            }

            // Esegui lo SphereCast
            Ray ray = new Ray(rayStart, (rayEnd - rayStart).normalized);
            if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, rayLength, layerMask))
            {
                // Controlla se è un pedone
                if (hit.collider.CompareTag("pedestrian"))
                {
                    GameObject pedestrian = hit.collider.gameObject;

                    // Aggiungi il pedone alla lista
                    if (!pedestrians.Contains(pedestrian))
                    {
                        pedestrians.Add(pedestrian);
                    }
                }else
                {
                    Debug.Log($"Oggetto con tag diverso rilevato: {hit.collider.tag}");
                }
            }
        }

        return pedestrians;
    }

    private void Update()
    {
        if (straightSensor == null)
        {
            return;
        }

        // Rileva i pedoni usando il sensore
        List<GameObject> newlyDetectedPedestrians = DetectObjectsWithSensors(straightSensor);

        // Aggiungi nuovi pedoni rilevati alla lista
        foreach (var pedestrian in newlyDetectedPedestrians)
        {
            if (detectedPedestrians.Add(pedestrian))
            {
                if (!pedestrianDistanceSum.ContainsKey(pedestrian))
                {
                    pedestrianDistanceSum[pedestrian] = 0f;
                    pedestrianDetectionCount[pedestrian] = 0;
                }
            }
        }

        // Calcola la distanza tra i pedoni rilevati e la macchina
        foreach (var pedestrian in detectedPedestrians)
        {
            if (pedestrian != null)
            {
                float distance = Vector3.Distance(transform.position, pedestrian.transform.position);

                // Se la distanza è critica (<7), accumula i dati
                if (distance < 7.0f)
                {
                    hasCriticalDistance = true;
                    criticalDistanceSum += distance; // Somma delle distanze critiche
                    criticalDistanceCount++; // Conteggio delle rilevazioni critiche
                }
            }
        }
    }

    public void FinalizeEpisode()
    {

        if (hasCriticalDistance)
        {
            // Normalizza la distanza media critica
            float normalizedCriticalDistance = criticalDistanceSum / criticalDistanceCount;

            // Calcola penalità incrementale tra -0.1 e -0.3
            float penalty = Mathf.Lerp(-0.1f, -0.4f, (7.0f - normalizedCriticalDistance) / 7.0f);
            agent.AddReward(penalty);

            Debug.Log($"Penalità applicata: {penalty} (Distanza Critica Media: {normalizedCriticalDistance})");
        }
        else
        {
            agent.AddReward(0.3f);

            Debug.Log($"Bonus applicato (Mai sceso sotto 7)");
        }
    }
}
