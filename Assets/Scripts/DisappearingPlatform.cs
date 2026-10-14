using System.Collections;
using UnityEngine;

public enum DisappearingPlatformMode
{
    FallAway,
    BlinkAway
}

/// <summary>
/// Trigger-side behavior for a platform already serialized in the level.
/// </summary>
public class DisappearingPlatform : MonoBehaviour
{
    [SerializeField] private RageLevelRuntime runtime;
    [SerializeField] private GameObject targetPlatform;
    [SerializeField] private DisappearingPlatformMode mode;
    [SerializeField] private float delay = 0.65f;
    [SerializeField] private float returnAfter = 2.5f;
    [SerializeField] private float fallDistance = 2.8f;
    [SerializeField] private string message = "The platform changed its mind.";

    private bool sequenceStarted;

    public void Configure(
        RageLevelRuntime levelRuntime,
        GameObject platform,
        DisappearingPlatformMode platformMode,
        float disappearDelay,
        float reappearAfter,
        float dropDistance,
        string rageMessage)
    {
        runtime = levelRuntime;
        targetPlatform = platform;
        mode = platformMode;
        delay = disappearDelay;
        returnAfter = reappearAfter;
        fallDistance = dropDistance;
        message = rageMessage;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (sequenceStarted || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        sequenceStarted = true;
        StartCoroutine(PlatformSequence());
    }

    private IEnumerator PlatformSequence()
    {
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }

        if (runtime != null)
        {
            runtime.ShowRageMessage(message, delay + 0.8f);
        }

        yield return new WaitForSeconds(delay);
        if (targetPlatform == null)
        {
            yield break;
        }

        Vector3 originalPosition = targetPlatform.transform.position;
        SetVisibleAndCollidable(false);
        if (mode == DisappearingPlatformMode.FallAway)
        {
            targetPlatform.transform.position = originalPosition + Vector3.down * fallDistance;
        }

        yield return new WaitForSeconds(returnAfter);
        if (targetPlatform == null)
        {
            yield break;
        }

        targetPlatform.transform.position = originalPosition;
        SetVisibleAndCollidable(true);
        sequenceStarted = false;
    }

    private void SetVisibleAndCollidable(bool visible)
    {
        if (targetPlatform == null)
        {
            return;
        }

        foreach (Renderer renderer in targetPlatform.GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = visible;
        }

        foreach (Collider collider in targetPlatform.GetComponentsInChildren<Collider>())
        {
            collider.enabled = visible;
        }
    }
}
