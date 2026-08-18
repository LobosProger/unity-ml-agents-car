using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MoveToTargetAgent : Agent
{
    [Header("References")]
    [SerializeField] Transform target;

    [Header("Reward Settings")]
    [SerializeField, Range(0f, 0.05f)] float _penaltyPerDecisionStep = 0.001f;

    public event Action OnAgentReachedTarget;
    public event Action OnAgentHitWall;

    TargetRandomPositionOnCircle _targetRandomPositionOnCircle;

    const string k_wallTag = "Wall";
    const string k_targetTag = "Target";

    void Awake()
    {
        CacheRandomPositionComponentFromTarget();
    }

    void CacheRandomPositionComponentFromTarget()
    {
        if (target == null) return;
        target.TryGetComponent(out _targetRandomPositionOnCircle);
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = Vector3.zero;
        RefreshPositionOfTargetOnCircle();
    }

    void RefreshPositionOfTargetOnCircle()
    {
        if (_targetRandomPositionOnCircle == null) return;
        _targetRandomPositionOnCircle.SetRandomPositionOnCircleForTarget();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(target.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        ApplyPenaltyForElapsedDecisionStep();

        var moveX = actions.ContinuousActions[0];
        var moveZ = actions.ContinuousActions[1];

        var speed = 5f;
        transform.localPosition += new Vector3(moveX, 0, moveZ) * Time.deltaTime * speed;
    }

    void ApplyPenaltyForElapsedDecisionStep()
    {
        AddReward(-_penaltyPerDecisionStep);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(k_wallTag))
        {
            SetReward(-1f);
            OnAgentHitWall?.Invoke();
            EndEpisode();
        }
        else if (other.CompareTag(k_targetTag))
        {
            SetReward(1f);
            OnAgentReachedTarget?.Invoke();
            EndEpisode();
        }
    }
}
