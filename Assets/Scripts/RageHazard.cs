using UnityEngine;

/// <summary>
/// Deterministic hazard behavior for a scene-authored collider.
/// </summary>
public class RageHazard : MonoBehaviour
{
    [SerializeField] private RageLevelRuntime runtime;
    [SerializeField] private string message = "That looked safe.";
    [SerializeField] private bool oneShot;

    private bool hasTriggered;

    public void Configure(RageLevelRuntime levelRuntime, string rageMessage, bool triggerOnce = false)
    {
        runtime = levelRuntime;
        message = string.IsNullOrWhiteSpace(rageMessage) ? "That looked safe." : rageMessage;
        oneShot = triggerOnce;
    }

    public void HitPlayer(Component player)
    {
        if (player == null || (oneShot && hasTriggered))
        {
            return;
        }

        hasTriggered = true;
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }

        if (runtime != null)
        {
            runtime.FailPlayer(message);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            HitPlayer(other);
        }
    }
}
