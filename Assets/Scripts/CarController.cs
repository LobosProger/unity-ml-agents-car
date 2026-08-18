using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    [SerializeField] WheelCollider _wheelColliderFrontLeft;
    [SerializeField] WheelCollider _wheelColliderFrontRight;
    [SerializeField] WheelCollider _wheelColliderRearLeft;
    [SerializeField] WheelCollider _wheelColliderRearRight;

    [Header("Wheel Visual Meshes")]
    [SerializeField] Transform _wheelMeshFrontLeft;
    [SerializeField] Transform _wheelMeshFrontRight;
    [SerializeField] Transform _wheelMeshRearLeft;
    [SerializeField] Transform _wheelMeshRearRight;

    [Header("Driving Parameters")]
    [SerializeField, Range(10f, 60f)] float _maxSteerAngleInDegrees = 30f;
    [SerializeField, Range(100f, 4000f)] float _maxMotorTorque = 1500f;
    [SerializeField, Range(100f, 8000f)] float _maxBrakeTorque = 3000f;

    [Header("Drivetrain")]
    [SerializeField] bool _isAllWheelDrive = false;

    [Header("Stability")]
    [SerializeField] Vector3 _centerOfMassOffset = new Vector3(0f, -0.4f, 0f);
    [SerializeField, Range(0.05f, 5f)] float _forwardSpeedThresholdToTreatAsForward = 0.3f;
    [SerializeField, Range(0f, 90f)] float _maxAngleFromUpToConsiderUpright = 60f;

    Rigidbody _rigidbodyOfCar;
    float _currentSteeringInput;
    float _currentMotorInput;

    public float CurrentSpeedAlongForwardAxis => Vector3.Dot(_rigidbodyOfCar.linearVelocity, transform.forward);
    public Vector3 CurrentVelocityInLocalSpace => transform.InverseTransformDirection(_rigidbodyOfCar.linearVelocity);
    public bool IsCarFlippedOverAndStuck => Vector3.Angle(transform.up, Vector3.up) > _maxAngleFromUpToConsiderUpright;

    void Awake()
    {
        CacheRigidbodyAndLowerCenterOfMass();
    }

    void CacheRigidbodyAndLowerCenterOfMass()
    {
        _rigidbodyOfCar = GetComponent<Rigidbody>();
        _rigidbodyOfCar.centerOfMass = _centerOfMassOffset;
    }

    public void SetSteeringInput(float steeringInputInMinusOneToOne)
    {
        _currentSteeringInput = Mathf.Clamp(steeringInputInMinusOneToOne, -1f, 1f);
    }

    public void SetMotorInput(float motorInputInMinusOneToOne)
    {
        _currentMotorInput = Mathf.Clamp(motorInputInMinusOneToOne, -1f, 1f);
    }

    void FixedUpdate()
    {
        ApplySteerAngleToFrontWheels();
        ApplyMotorAndBrakeTorquesToWheels();
    }

    void ApplySteerAngleToFrontWheels()
    {
        float steerAngleInDegrees = _currentSteeringInput * _maxSteerAngleInDegrees;
        _wheelColliderFrontLeft.steerAngle = steerAngleInDegrees;
        _wheelColliderFrontRight.steerAngle = steerAngleInDegrees;
    }

    void ApplyMotorAndBrakeTorquesToWheels()
    {
        if (IsRequestingBrakeBecauseMovingForwardButInputNegative())
        {
            ApplyBrakeTorqueAndZeroMotor();
            return;
        }
        ApplyMotorTorqueAndReleaseBrakes();
    }

    bool IsRequestingBrakeBecauseMovingForwardButInputNegative()
    {
        bool isInputBraking = _currentMotorInput < 0f;
        bool isCarMovingForward = CurrentSpeedAlongForwardAxis > _forwardSpeedThresholdToTreatAsForward;
        return isInputBraking && isCarMovingForward;
    }

    void ApplyBrakeTorqueAndZeroMotor()
    {
        float brakeForceFromInput = Mathf.Abs(_currentMotorInput) * _maxBrakeTorque;
        SetMotorTorqueOnDrivenWheels(0f);
        SetBrakeTorqueOnAllWheels(brakeForceFromInput);
    }

    void ApplyMotorTorqueAndReleaseBrakes()
    {
        SetMotorTorqueOnDrivenWheels(_currentMotorInput * _maxMotorTorque);
        SetBrakeTorqueOnAllWheels(0f);
    }

    void SetMotorTorqueOnDrivenWheels(float motorTorque)
    {
        _wheelColliderRearLeft.motorTorque = motorTorque;
        _wheelColliderRearRight.motorTorque = motorTorque;
        if (!_isAllWheelDrive) return;
        _wheelColliderFrontLeft.motorTorque = motorTorque;
        _wheelColliderFrontRight.motorTorque = motorTorque;
    }

    void SetBrakeTorqueOnAllWheels(float brakeTorque)
    {
        _wheelColliderFrontLeft.brakeTorque = brakeTorque;
        _wheelColliderFrontRight.brakeTorque = brakeTorque;
        _wheelColliderRearLeft.brakeTorque = brakeTorque;
        _wheelColliderRearRight.brakeTorque = brakeTorque;
    }

    void Update()
    {
        SyncEachWheelMeshWithItsCollider();
    }

    void SyncEachWheelMeshWithItsCollider()
    {
        SyncSingleWheelMeshWithCollider(_wheelColliderFrontLeft, _wheelMeshFrontLeft);
        SyncSingleWheelMeshWithCollider(_wheelColliderFrontRight, _wheelMeshFrontRight);
        SyncSingleWheelMeshWithCollider(_wheelColliderRearLeft, _wheelMeshRearLeft);
        SyncSingleWheelMeshWithCollider(_wheelColliderRearRight, _wheelMeshRearRight);
    }

    void SyncSingleWheelMeshWithCollider(WheelCollider wheelCollider, Transform wheelMesh)
    {
        if (wheelCollider == null || wheelMesh == null) return;
        wheelCollider.GetWorldPose(out Vector3 wheelWorldPosition, out Quaternion wheelWorldRotation);
        wheelMesh.SetPositionAndRotation(wheelWorldPosition, wheelWorldRotation);
    }

    public void ResetCarPhysicsToPose(Vector3 worldPosition, Quaternion worldRotation)
    {
        _rigidbodyOfCar.linearVelocity = Vector3.zero;
        _rigidbodyOfCar.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(worldPosition, worldRotation);
        ZeroAllSteeringAndTorques();
    }

    void ZeroAllSteeringAndTorques()
    {
        _currentSteeringInput = 0f;
        _currentMotorInput = 0f;
        SetMotorTorqueOnDrivenWheels(0f);
        SetBrakeTorqueOnAllWheels(0f);
        ApplySteerAngleToFrontWheels();
    }
}
