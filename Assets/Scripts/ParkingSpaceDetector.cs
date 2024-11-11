using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;

public class ParkingSpaceDetector : MonoBehaviour
{
    public LayerMask layerMask;
    public int raysPerDirection; // Numero di raggi da ogni lato
    public float maxRayDegrees; // Gradi massimi dei raggi
    public float startVerticalOffset; // Se disponibile
    public float endVerticalOffsetBase;
    public int numberOfSensor;
    public bool rayVisible = false;
    public float sphereCastRadius = 0.5f; // Raggio dello SphereCast

    public List<GameObject> DetectLineObjectsWithSensors(List<RayPerceptionSensorComponent3D> raySensors)
    {
        List<GameObject> detectedLineObjects = new List<GameObject>();
        Debug.Log("Inizio rilevamento linee di parcheggio tramite sensori");

        float index = 0;
        float rayLength = 0f; // Lunghezza dei raggi
        float angleStep = 0f; // Passo angolare tra i raggi
        float endVerticalOffset = 0f;
        float raysPerDirectionCircle = 0f;

        foreach (var sensor in raySensors)
        {
            for (int j = 0; j < numberOfSensor; j++)
            {
                if (sensor.SensorName.Contains("TopSensor"))
                {
                    // Ottieni la configurazione dei raggi dal sensore
                    rayLength = sensor.RayLength; // Lunghezza dei raggi
                    endVerticalOffset = endVerticalOffsetBase - (index / 2);
                    raysPerDirectionCircle = raysPerDirection - (2 * index);
                    angleStep = maxRayDegrees / raysPerDirectionCircle;

                    if (index == (numberOfSensor - 1))
                    {
                        index = 0;
                    }
                    else
                    {
                        index++;
                    }
                }

                for (int i = -raysPerDirection; i <= raysPerDirection; i++)
                {
                    // Calcola la direzione del raggio
                    Quaternion rotation = Quaternion.Euler(0, angleStep * i, 0);
                    Vector3 rayDirection = rotation * sensor.transform.forward;

                    // Calcola l'inizio e la fine del raggio usando gli offset verticali
                    Vector3 rayStart = sensor.transform.position + Vector3.up * startVerticalOffset;
                    Vector3 rayEnd = sensor.transform.position + rayDirection * rayLength + Vector3.up * endVerticalOffset;

                    // Disegna il raggio con Debug.DrawLine per visualizzare il raggio inclinato
                    if (rayVisible)
                    {
                        Debug.DrawLine(rayStart, rayEnd, Color.blue);
                    }

                    // Esegui lo SphereCast dalla posizione iniziale calcolata
                    Ray ray = new Ray(rayStart, (rayEnd - rayStart).normalized);
                    if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, rayLength, layerMask))
                    {
                        if (hit.collider.CompareTag("line") && !detectedLineObjects.Contains(hit.collider.gameObject))
                        {
                            detectedLineObjects.Add(hit.collider.gameObject);
                            Debug.Log($"Rilevato oggetto linea di parcheggio: {hit.collider.gameObject.name}");
                        }
                    }
                }
            }
        }

        Debug.Log($"Numero totale di linee di parcheggio rilevate: {detectedLineObjects.Count}");
        return detectedLineObjects;
    }

    public HashSet<(Vector3, int)> DetectRectangularParkingSpaces(List<GameObject> allDetectedLineObjects)
    {

        HashSet<(Vector3, int)> validRectangleCenters = new HashSet<(Vector3, int)>();

        if (allDetectedLineObjects.Count < 3)
        {
            Debug.Log("Non ci sono abbastanza linee per identificare un rettangolo.");
            return validRectangleCenters;
        }

        HashSet<(GameObject, GameObject)> processedPairs = new HashSet<(GameObject, GameObject)>();

        foreach (var lineObjectA in allDetectedLineObjects)
        {
            foreach (var lineObjectB in allDetectedLineObjects)
            {
                if (lineObjectA == lineObjectB || processedPairs.Contains((lineObjectA, lineObjectB)) || processedPairs.Contains((lineObjectB, lineObjectA)))
                    continue;

                Vector3 centerA = lineObjectA.transform.position;
                Vector3 centerB = lineObjectB.transform.position;
                float distance = Vector3.Distance(centerA, centerB);

                Vector3 directionA = lineObjectA.transform.forward;
                Vector3 directionB = lineObjectB.transform.forward;
                float angle = Vector3.Angle(directionA, directionB);

                if ((Mathf.Abs(angle - 90f) < 5f) && (distance < 4.0f))
                {

                    foreach (var lineObjectC in allDetectedLineObjects)
                    {
                        if (lineObjectC == lineObjectA || lineObjectC == lineObjectB || processedPairs.Contains((lineObjectC, lineObjectA)))
                            continue;

                        Vector3 centerC = lineObjectC.transform.position;
                        float newDistance = Vector3.Distance(centerA, centerC);

                        Vector3 directionC = lineObjectC.transform.forward;
                        float newAngle = Vector3.Angle(directionA, directionC);

                        if (Mathf.Abs(newAngle - 90f) < 5f && newDistance <= distance + 0.5f)
                        {
                            Vector3 rectangleCenter = (centerA + centerB + centerC) / 3;

                            Vector3 boxSize = new Vector3(distance / 2, 1f, newDistance / 2);
                            Collider[] colliders = Physics.OverlapBox(rectangleCenter, boxSize, Quaternion.identity, LayerMask.GetMask("ParkingGroup"));

                            bool containsCar = false;
                            foreach (var collider in colliders)
                            {
                                if (collider.CompareTag("car"))
                                {
                                    containsCar = true;
                                    Debug.Log("Rettangolo contiene un'auto.");
                                    break;
                                }
                            }

                            if (!containsCar)
                            {
                                validRectangleCenters.Add((rectangleCenter, 90));
                                Debug.Log("Rettangolo valido aggiunto alla lista.");
                            }
                        }
                    }
                }
            }
        }
        return validRectangleCenters;
    }

    // Funzione per rilevare parcheggi parallelepipedi
    public HashSet<(Vector3, int)> DetectParallelepipedParkingSpaces(List<GameObject> allDetectedLineObjects)
    {

        HashSet<(Vector3, int)> validParallelepipedCenters = new HashSet<(Vector3, int)>();

        if (allDetectedLineObjects.Count < 3)
        {
            Debug.Log("Non ci sono abbastanza linee per identificare un parallelepipedo.");
            return validParallelepipedCenters;
        }

        HashSet<(GameObject, GameObject)> processedPairs = new HashSet<(GameObject, GameObject)>();

        foreach (var lineObjectA in allDetectedLineObjects)
        {
            if (!Mathf.Approximately(lineObjectA.transform.eulerAngles.y, 0f))
                continue;

            foreach (var lineObjectB in allDetectedLineObjects)
            {
                if (lineObjectA == lineObjectB || processedPairs.Contains((lineObjectA, lineObjectB)) || processedPairs.Contains((lineObjectB, lineObjectA)))
                    continue;

                if (!Mathf.Approximately(lineObjectB.transform.eulerAngles.y, 45f) && !Mathf.Approximately(lineObjectB.transform.eulerAngles.y, 135f))
                    continue;

                Vector3 centerA = lineObjectA.transform.position;
                Vector3 centerB = lineObjectB.transform.position;
                float distance = Vector3.Distance(centerA, centerB);

                Vector3 directionA = lineObjectA.transform.forward;
                Vector3 directionB = lineObjectB.transform.forward;
                float angle = Vector3.Angle(directionA, directionB);

                bool isValidAngle = Mathf.Abs(angle - 45f) < 5f || Mathf.Abs(angle - 135f) < 5f;
                if (isValidAngle && (distance < 4.0f))
                {
                    //processedPairs.Add((lineObjectA, lineObjectB));

                    foreach (var lineObjectC in allDetectedLineObjects)
                    {
                        if (lineObjectC == lineObjectA || lineObjectC == lineObjectB || processedPairs.Contains((lineObjectC, lineObjectA)))
                            continue;

                        if (!Mathf.Approximately(lineObjectC.transform.eulerAngles.y, 45f) && !Mathf.Approximately(lineObjectC.transform.eulerAngles.y, 135f))
                            continue;

                        Vector3 centerC = lineObjectC.transform.position;
                        float newDistance = Vector3.Distance(centerA, centerC);

                        Vector3 directionC = lineObjectC.transform.forward;
                        float newAngle = Vector3.Angle(directionA, directionC);

                        bool isAngleCValid = Mathf.Abs(newAngle - 45f) < 5f || Mathf.Abs(newAngle - 135f) < 5f;
                        if (isAngleCValid && newDistance <= distance + 0.5f)
                        {
                            Vector3 parallelepipedCenter = (centerA + centerB + centerC) / 3;

                            Vector3 boxSize = new Vector3(distance / 2, 1f, newDistance / 2);
                            Collider[] colliders = Physics.OverlapBox(parallelepipedCenter, boxSize, Quaternion.identity, LayerMask.GetMask("ParkingGroup"));

                            bool containsCar = false;
                            foreach (var collider in colliders)
                            {
                                if (collider.CompareTag("car"))
                                {
                                    containsCar = true;
                                    Debug.Log("Parallelepipedo contiene un'auto.");
                                    break;
                                }
                            }

                            if (!containsCar)
                            {
                                validParallelepipedCenters.Add((parallelepipedCenter, 45));
                                Debug.Log("Parallelepipedo valido aggiunto alla lista.");
                            }
                        }
                    }
                }
            }
        }
        return validParallelepipedCenters;
    }
}
