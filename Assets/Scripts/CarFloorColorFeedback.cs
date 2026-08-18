using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class CarFloorColorFeedback : MonoBehaviour
{
    [Header("Agent To Observe")]
    [SerializeField] CarAgent _carAgentOfThisEnvironment;

    [Header("Feedback Materials")]
    [SerializeField] Material _materialOfFloorOnSuccess;
    [SerializeField] Material _materialOfFloorOnFailure;

    [Header("Reset Settings")]
    [SerializeField, Range(0f, 5f)] float _durationToKeepFeedbackMaterial = 0.5f;

    Renderer _rendererOfFloor;
    Material _materialOfFloorAtStart;
    Coroutine _coroutineOfResetMaterial;

    void Awake()
    {
        _rendererOfFloor = GetComponent<Renderer>();
        _materialOfFloorAtStart = _rendererOfFloor.sharedMaterial;
    }

    void OnEnable()
    {
        SubscribeToAgentEpisodeResultEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromAgentEpisodeResultEvents();
    }

    void SubscribeToAgentEpisodeResultEvents()
    {
        if (_carAgentOfThisEnvironment == null) return;
        _carAgentOfThisEnvironment.OnAgentReachedTarget += OnAgentReachedTargetSuccessfully;
        _carAgentOfThisEnvironment.OnAgentHitWall += OnAgentHitWallAndFailed;
        _carAgentOfThisEnvironment.OnAgentFlippedOver += OnAgentFlippedOverAndFailed;
    }

    void UnsubscribeFromAgentEpisodeResultEvents()
    {
        if (_carAgentOfThisEnvironment == null) return;
        _carAgentOfThisEnvironment.OnAgentReachedTarget -= OnAgentReachedTargetSuccessfully;
        _carAgentOfThisEnvironment.OnAgentHitWall -= OnAgentHitWallAndFailed;
        _carAgentOfThisEnvironment.OnAgentFlippedOver -= OnAgentFlippedOverAndFailed;
    }

    void OnAgentReachedTargetSuccessfully()
    {
        ApplyFeedbackMaterialAndScheduleReset(_materialOfFloorOnSuccess);
    }

    void OnAgentHitWallAndFailed()
    {
        ApplyFeedbackMaterialAndScheduleReset(_materialOfFloorOnFailure);
    }

    void OnAgentFlippedOverAndFailed()
    {
        ApplyFeedbackMaterialAndScheduleReset(_materialOfFloorOnFailure);
    }

    void ApplyFeedbackMaterialAndScheduleReset(Material materialToApply)
    {
        if (materialToApply == null) return;
        _rendererOfFloor.sharedMaterial = materialToApply;
        RestartCoroutineOfResetMaterial();
    }

    void RestartCoroutineOfResetMaterial()
    {
        if (_coroutineOfResetMaterial != null) StopCoroutine(_coroutineOfResetMaterial);
        _coroutineOfResetMaterial = StartCoroutine(ResetMaterialOfFloorAfterDelay());
    }

    System.Collections.IEnumerator ResetMaterialOfFloorAfterDelay()
    {
        yield return new WaitForSeconds(_durationToKeepFeedbackMaterial);
        _rendererOfFloor.sharedMaterial = _materialOfFloorAtStart;
        _coroutineOfResetMaterial = null;
    }
}
