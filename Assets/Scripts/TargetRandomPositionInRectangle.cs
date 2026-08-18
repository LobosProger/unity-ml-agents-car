using UnityEngine;

public class TargetRandomPositionInRectangle : MonoBehaviour
{
    [Header("Rectangle Bounds (Local Space, XZ)")]
    [SerializeField, Range(1f, 100f)] float _halfWidthAlongX = 18f;
    [SerializeField, Range(1f, 100f)] float _halfDepthAlongZ = 18f;
    [SerializeField, Range(0f, 20f)] float _minimumDistanceFromOriginToAvoidSpawningOnCar = 6f;

    [Header("Gizmo Settings")]
    [SerializeField] Color _colorOfRectangleGizmo = Color.yellow;

    const int k_maxResampleAttempts = 16;

    void OnEnable()
    {
        SetRandomPositionInRectangleForTarget();
    }

    public void SetRandomPositionInRectangleForTarget()
    {
        Vector3 sampledLocalPositionOnPlane = SampleLocalPositionAvoidingCenter();
        float preservedYInCurrentLocalPosition = transform.localPosition.y;
        transform.localPosition = new Vector3(sampledLocalPositionOnPlane.x, preservedYInCurrentLocalPosition, sampledLocalPositionOnPlane.z);
    }

    Vector3 SampleLocalPositionAvoidingCenter()
    {
        for (int indexOfAttempt = 0; indexOfAttempt < k_maxResampleAttempts; indexOfAttempt++)
        {
            Vector3 candidateLocalPosition = SampleSingleCandidateLocalPosition();
            if (IsCandidateFarEnoughFromOrigin(candidateLocalPosition)) return candidateLocalPosition;
        }
        return GetFallbackPositionOnMinimumDistanceRing();
    }

    Vector3 SampleSingleCandidateLocalPosition()
    {
        float candidateX = Random.Range(-_halfWidthAlongX, _halfWidthAlongX);
        float candidateZ = Random.Range(-_halfDepthAlongZ, _halfDepthAlongZ);
        return new Vector3(candidateX, 0f, candidateZ);
    }

    bool IsCandidateFarEnoughFromOrigin(Vector3 candidateLocalPosition)
    {
        float distanceFromOriginOnPlane = new Vector2(candidateLocalPosition.x, candidateLocalPosition.z).magnitude;
        return distanceFromOriginOnPlane >= _minimumDistanceFromOriginToAvoidSpawningOnCar;
    }

    Vector3 GetFallbackPositionOnMinimumDistanceRing()
    {
        Vector2 fallbackDirectionOnPlane = Random.insideUnitCircle.normalized;
        float fallbackX = fallbackDirectionOnPlane.x * _minimumDistanceFromOriginToAvoidSpawningOnCar;
        float fallbackZ = fallbackDirectionOnPlane.y * _minimumDistanceFromOriginToAvoidSpawningOnCar;
        return new Vector3(fallbackX, 0f, fallbackZ);
    }

    [ContextMenu("Randomize Position In Rectangle")]
    void RandomizePositionFromContextMenu()
    {
        SetRandomPositionInRectangleForTarget();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        DrawRectangleGizmoOnXZPlaneAroundParent();
    }

    void DrawRectangleGizmoOnXZPlaneAroundParent()
    {
        Matrix4x4 previousMatrixOfGizmos = Gizmos.matrix;
        Gizmos.matrix = GetMatrixForGizmosInLocalSpaceOfParent();
        Gizmos.color = _colorOfRectangleGizmo;

        Vector3 sizeOfRectangleGizmo = new Vector3(_halfWidthAlongX * 2f, 0.01f, _halfDepthAlongZ * 2f);
        Gizmos.DrawWireCube(Vector3.zero, sizeOfRectangleGizmo);

        Gizmos.matrix = previousMatrixOfGizmos;
    }

    Matrix4x4 GetMatrixForGizmosInLocalSpaceOfParent()
    {
        if (transform.parent == null) return Matrix4x4.identity;
        return transform.parent.localToWorldMatrix;
    }
#endif
}
