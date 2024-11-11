using System.Collections;
using System.Collections.Generic;
using DilmerGames.Core;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField]
    private float speed = 1.0f;

    [SerializeField]
    private float torque = 1.0f;

    [SerializeField]
    private float minSpeedBeforeTorque = 0.3f;

    [SerializeField]
    private float minSpeedBeforeIdle = 0.2f;

    [SerializeField]
    private float maxSpeed = 2.0f; // Velocità massima

    private float currentMaxSpeed;

    [SerializeField]
    private Animator carAnimator;

    private CarAgent agent = null;

    public Direction CurrentDirection { get; set; } = Direction.Idle;

    public bool IsAutonomous { get; set; } = false;

    private Rigidbody carRigidBody;

    public enum Direction
    {
        Idle,
        MoveForward,
        MoveBackward,
        TurnLeft,
        TurnRight
    }

    void Awake() 
    {
        carRigidBody = GetComponent<Rigidbody>();
    }

    void start()
    {
        agent = transform.parent.GetComponentInChildren<CarAgent>();
        currentMaxSpeed = maxSpeed;
    }

    void Update() 
    {

        if (carRigidBody.velocity.magnitude <= minSpeedBeforeIdle)
        {
            CurrentDirection = Direction.Idle;
            ApplyAnimatorState(Direction.Idle);
        }

        if (agent.currentState == CarAgent.AgentState.SearchingForParking)
        {
            currentMaxSpeed = maxSpeed / 2;
        }
        else
        {
            currentMaxSpeed = maxSpeed;
        }
    }

    void FixedUpdate()
    {
        ApplyMovement();
        agent = transform.parent.GetComponentInChildren<CarAgent>();

        // Verifica se la velocità dell'auto supera la soglia consentita
        if (carRigidBody.velocity.magnitude > currentMaxSpeed && currentMaxSpeed < maxSpeed) // Imposta maxSpeed come soglia massima desiderata
        {
            //agent.AddReward(-0.05f); // Penalità per velocità eccessiva
            carRigidBody.velocity = carRigidBody.velocity.normalized * currentMaxSpeed;
            Debug.Log($"Velocità attuale: {carRigidBody.velocity.magnitude}");
        }
        else
        {
            Debug.Log($"Velocità attuale: {carRigidBody.velocity.magnitude}");
        }
    }

    public void ApplyMovement()
    {
        if ((Input.GetKey(KeyCode.UpArrow) || (CurrentDirection == Direction.MoveForward && IsAutonomous)) && carRigidBody.velocity.magnitude < currentMaxSpeed) // Limite di velocità
        {
            ApplyAnimatorState(Direction.MoveForward);
            carRigidBody.AddForce(transform.forward * speed, ForceMode.VelocityChange);
        }

        if ((Input.GetKey(KeyCode.DownArrow) || (CurrentDirection == Direction.MoveBackward && IsAutonomous)) &&
           carRigidBody.velocity.magnitude < currentMaxSpeed) // Limite di velocità
        {
            ApplyAnimatorState(Direction.MoveBackward);
            carRigidBody.AddForce(-transform.forward * speed, ForceMode.VelocityChange);
        }

        if((Input.GetKey(KeyCode.LeftArrow) && canApplyTorque()) || (CurrentDirection == Direction.TurnLeft && IsAutonomous))
        {
            ApplyAnimatorState(Direction.TurnLeft);
            carRigidBody.AddTorque(transform.up * -torque);
        }

        if(Input.GetKey(KeyCode.RightArrow) && canApplyTorque() || (CurrentDirection == Direction.TurnRight && IsAutonomous))
        {
            ApplyAnimatorState(Direction.TurnRight);
            carRigidBody.AddTorque(transform.up * torque);
        }
    }

    void ApplyAnimatorState(Direction direction)
    {   
        carAnimator.SetBool(direction.ToString(), true);

        switch(direction)
        {
            case Direction.Idle:
                carAnimator.SetBool(Direction.MoveBackward.ToString(), false);
                carAnimator.SetBool(Direction.MoveForward.ToString(), false);
                carAnimator.SetBool(Direction.TurnLeft.ToString(), false);
                carAnimator.SetBool(Direction.TurnRight.ToString(), false);
            break;
            case Direction.MoveForward:
                carAnimator.SetBool(Direction.Idle.ToString(), false);
                carAnimator.SetBool(Direction.MoveBackward.ToString(), false);
                carAnimator.SetBool(Direction.TurnLeft.ToString(), false);
                carAnimator.SetBool(Direction.TurnRight.ToString(), false);
            break;
            case Direction.MoveBackward:
                carAnimator.SetBool(Direction.Idle.ToString(), false);
                carAnimator.SetBool(Direction.MoveForward.ToString(), false);
                carAnimator.SetBool(Direction.TurnLeft.ToString(), false);
                carAnimator.SetBool(Direction.TurnRight.ToString(), false);
            break;
            case Direction.TurnLeft:
                carAnimator.SetBool(Direction.TurnRight.ToString(), false);
            break;
            case Direction.TurnRight:
                carAnimator.SetBool(Direction.TurnLeft.ToString(), false);
            break;
        }
    }

    public bool canApplyTorque()
    {
        Vector3 velocity = carRigidBody.velocity;
        return Mathf.Abs(velocity.x) >= minSpeedBeforeTorque || Mathf.Abs(velocity.z) >= minSpeedBeforeTorque;
    }
}
