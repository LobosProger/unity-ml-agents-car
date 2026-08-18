using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(CarController))]
public class CarAgent : Agent
{
    [Header("References")]
    [SerializeField] Transform _targetTransform;
    [SerializeField] CarController _carController;

    [Header("Episode Settings")]
    [SerializeField] Vector3 _startingLocalPositionOfCar = Vector3.zero;
    [SerializeField] Vector3 _startingLocalEulerAnglesOfCar = Vector3.zero;
    [SerializeField, Range(0f, 180f)] float _randomYawRangeAtEpisodeStartInDegrees = 0f;

    [Header("Reward Settings")]
    [SerializeField, Range(0f, 0.01f)] float _penaltyPerDecisionStep = 0.001f;
    [SerializeField, Range(0f, 0.1f)] float _bonusPerUnitOfProgressTowardTarget = 0.02f;
    [SerializeField, Range(0f, 1f)] float _rewardOnReachingTarget = 1f;
    [SerializeField, Range(-1f, 0f)] float _penaltyOnHittingWall = -1f;
    [SerializeField, Range(-1f, 0f)] float _penaltyOnFlippingOver = -0.5f;

    public event Action OnAgentReachedTarget;
    public event Action OnAgentHitWall;
    public event Action OnAgentFlippedOver;

    TargetRandomPositionInRectangle _targetRandomPositionInRectangle;
    float _previousDistanceToTargetForShaping;

    const string k_wallTag = "Wall";
    const string k_targetTag = "Target";

    void Awake()
    {
        CacheCarControllerIfNotAssigned();
        CacheRandomPositionComponentFromTarget();
    }

    void CacheCarControllerIfNotAssigned()
    {
        if (_carController != null) return;
        TryGetComponent(out _carController);
    }

    void CacheRandomPositionComponentFromTarget()
    {
        if (_targetTransform == null) return;
        _targetTransform.TryGetComponent(out _targetRandomPositionInRectangle);
    }

    public override void OnEpisodeBegin()
    {
        ResetCarToStartingPoseWithOptionalRandomYaw();
        RefreshPositionOfTargetInRectangle();
        CacheInitialDistanceToTargetForRewardShaping();
    }

    void ResetCarToStartingPoseWithOptionalRandomYaw()
    {
        Vector3 worldStartingPosition = ConvertStartingLocalPositionToWorld();
        Quaternion worldStartingRotation = ComputeStartingRotationWithOptionalRandomYaw();
        _carController.ResetCarPhysicsToPose(worldStartingPosition, worldStartingRotation);
    }

    Vector3 ConvertStartingLocalPositionToWorld()
    {
        if (transform.parent == null) return _startingLocalPositionOfCar;
        return transform.parent.TransformPoint(_startingLocalPositionOfCar);
    }

    Quaternion ComputeStartingRotationWithOptionalRandomYaw()
    {
        float randomYawOffsetInDegrees = UnityEngine.Random.Range(-_randomYawRangeAtEpisodeStartInDegrees, _randomYawRangeAtEpisodeStartInDegrees);
        Vector3 startingEulerWithRandomYaw = _startingLocalEulerAnglesOfCar + new Vector3(0f, randomYawOffsetInDegrees, 0f);
        return Quaternion.Euler(startingEulerWithRandomYaw);
    }

    void RefreshPositionOfTargetInRectangle()
    {
        if (_targetRandomPositionInRectangle == null) return;
        _targetRandomPositionInRectangle.SetRandomPositionInRectangleForTarget();
    }

    void CacheInitialDistanceToTargetForRewardShaping()
    {
        _previousDistanceToTargetForShaping = ComputeHorizontalDistanceFromCarToTarget();
    }

    float ComputeHorizontalDistanceFromCarToTarget()
    {
        if (_targetTransform == null) return 0f;
        Vector3 carToTargetIgnoringHeight = _targetTransform.position - transform.position;
        carToTargetIgnoringHeight.y = 0f;
        return carToTargetIgnoringHeight.magnitude;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        AddRelativeTargetPositionInLocalSpaceToObservations(sensor);
        AddCarLocalVelocityToObservations(sensor);
        AddCarForwardOrientationToObservations(sensor);
        AddForwardSpeedToObservations(sensor);
    }

    void AddRelativeTargetPositionInLocalSpaceToObservations(VectorSensor sensor)
    {
        Vector3 targetInLocalSpace = transform.InverseTransformPoint(_targetTransform.position);
        sensor.AddObservation(targetInLocalSpace.x);
        sensor.AddObservation(targetInLocalSpace.z);
    }

    void AddCarLocalVelocityToObservations(VectorSensor sensor)
    {
        Vector3 localVelocity = _carController.CurrentVelocityInLocalSpace;
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
    }

    void AddCarForwardOrientationToObservations(VectorSensor sensor)
    {
        // Yaw expressed as sin/cos avoids the -180/180 discontinuity in raw euler angles
        float yawInRadians = transform.eulerAngles.y * Mathf.Deg2Rad;
        sensor.AddObservation(Mathf.Sin(yawInRadians));
        sensor.AddObservation(Mathf.Cos(yawInRadians));
    }

    void AddForwardSpeedToObservations(VectorSensor sensor)
    {
        sensor.AddObservation(_carController.CurrentSpeedAlongForwardAxis);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        ForwardActionsToCarController(actions);
        ApplyPerStepPenaltyAndProgressShaping();
        CheckIfCarIsFlippedOverAndEndEpisodeIfNeeded();
    }

    void ForwardActionsToCarController(ActionBuffers actions)
    {
        float steeringInput = actions.ContinuousActions[0];
        float motorInput = actions.ContinuousActions[1];
        _carController.SetSteeringInput(steeringInput);
        _carController.SetMotorInput(motorInput);
    }

    void ApplyPerStepPenaltyAndProgressShaping()
    {
        AddReward(-_penaltyPerDecisionStep);
        AddDeltaProgressRewardSinceLastDecisionStep();
    }

    void AddDeltaProgressRewardSinceLastDecisionStep()
    {
        float currentDistance = ComputeHorizontalDistanceFromCarToTarget();
        float distanceReductionSinceLastStep = _previousDistanceToTargetForShaping - currentDistance;
        AddReward(distanceReductionSinceLastStep * _bonusPerUnitOfProgressTowardTarget);
        _previousDistanceToTargetForShaping = currentDistance;
    }

    void CheckIfCarIsFlippedOverAndEndEpisodeIfNeeded()
    {
        if (!_carController.IsCarFlippedOverAndStuck) return;
        AddReward(_penaltyOnFlippingOver);
        OnAgentFlippedOver?.Invoke();
        EndEpisode();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag(k_wallTag)) return;
        RegisterPenaltyForCollisionWithWall();
    }

    void RegisterPenaltyForCollisionWithWall()
    {
        AddReward(_penaltyOnHittingWall);
        OnAgentHitWall?.Invoke();
        EndEpisode();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(k_targetTag)) return;
        RegisterRewardForReachingTarget();
    }

    void RegisterRewardForReachingTarget()
    {
        AddReward(_rewardOnReachingTarget);
        OnAgentReachedTarget?.Invoke();
        EndEpisode();
    }
}
