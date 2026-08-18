using UnityEngine;

public class TargetRandomPositionOnCircle : MonoBehaviour
{
    [Header("Circle Settings")]
    [SerializeField, Range(0.1f, 100f)] float _radiusOfCircle = 5f;

    [Header("Gizmo Settings")]
    [SerializeField] Color _colorOfCircleGizmo = Color.yellow;
    [SerializeField, Range(8, 256)] int _segmentsOfCircleGizmo = 64;

    static readonly Vector3 k_centerOfCircle = Vector3.zero;

    void OnEnable()
    {
        SetRandomPositionOnCircleForTarget();
    }

    public void SetRandomPositionOnCircleForTarget()
    {
        var nextSeed = Random.seed + 1;
        Random.InitState(nextSeed);

        Vector2 randomPointOnUnitCircle = Random.insideUnitCircle.normalized;
        float localPositionX = k_centerOfCircle.x + randomPointOnUnitCircle.x * _radiusOfCircle;
        float localPositionZ = k_centerOfCircle.z + randomPointOnUnitCircle.y * _radiusOfCircle;

        var currentLocalPosition = transform.localPosition;
        transform.localPosition = new Vector3(localPositionX, currentLocalPosition.y, localPositionZ);
    }

    [ContextMenu("Randomize Position On Circle")]
    void RandomizePositionFromContextMenu()
    {
        SetRandomPositionOnCircleForTarget();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        DrawCircleGizmoOnXZPlaneAroundParent();
    }

    void DrawCircleGizmoOnXZPlaneAroundParent()
    {
        Matrix4x4 previousMatrixOfGizmos = Gizmos.matrix;
        Gizmos.matrix = GetMatrixForGizmosInLocalSpaceOfParent();
        Gizmos.color = _colorOfCircleGizmo;

        float angleStepBetweenSegments = 360f / _segmentsOfCircleGizmo;
        Vector3 previousPointOnCircle = GetPointOnCircleByAngle(0f);

        for (int indexOfSegment = 1; indexOfSegment <= _segmentsOfCircleGizmo; indexOfSegment++)
        {
            float currentAngleInDegrees = angleStepBetweenSegments * indexOfSegment;
            Vector3 currentPointOnCircle = GetPointOnCircleByAngle(currentAngleInDegrees);
            Gizmos.DrawLine(previousPointOnCircle, currentPointOnCircle);
            previousPointOnCircle = currentPointOnCircle;
        }

        Gizmos.matrix = previousMatrixOfGizmos;
    }

    Matrix4x4 GetMatrixForGizmosInLocalSpaceOfParent()
    {
        if (transform.parent == null) return Matrix4x4.identity;
        return transform.parent.localToWorldMatrix;
    }

    Vector3 GetPointOnCircleByAngle(float angleInDegrees)
    {
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
        float pointX = k_centerOfCircle.x + Mathf.Cos(angleInRadians) * _radiusOfCircle;
        float pointZ = k_centerOfCircle.z + Mathf.Sin(angleInRadians) * _radiusOfCircle;
        return new Vector3(pointX, k_centerOfCircle.y, pointZ);
    }
#endif
}
