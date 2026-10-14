using System.Collections;
using UnityEngine;

public enum FakeCollectibleMode
{
    VanishOnApproach,
    DisappearOnInteract
}

/// <summary>
/// Deterministic collectible troll. Colliders and any linked collectible are
/// authored on the scene object; this component only changes their state.
/// </summary>
public class FakeCollectible : MonoBehaviour
{
    [SerializeField] private RageLevelRuntime runtime;
    [SerializeField] private Collectible linkedCollectible;
    [SerializeField] private FakeCollectibleMode mode;
    [SerializeField] private Vector3 relocation;
    [SerializeField] private string approachMessage = "It was right there.";
    [SerializeField] private Collider approachTrigger;

    private bool triggered;
    private Collider mainCollider;

    public void Configure(
        RageLevelRuntime levelRuntime,
        FakeCollectibleMode collectibleMode,
        Collectible realCollectible = null,
        Vector3 relocationPosition = default,
        string message = "It was right there.")
    {
        runtime = levelRuntime;
        mode = collectibleMode;
        linkedCollectible = realCollectible;
        relocation = relocationPosition;
        approachMessage = message;
    }

    public void SetApproachTrigger(Collider trigger)
    {
        approachTrigger = trigger;
    }

    private void Awake()
    {
        CacheColliders();
    }

    private void CacheColliders()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].isTrigger)
            {
                mainCollider = colliders[i];
                break;
            }
        }

        if (approachTrigger == null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].isTrigger)
                {
                    approachTrigger = colliders[i];
                    break;
                }
            }
        }
    }

    public void Interact()
    {
        if (triggered)
        {
            if (linkedCollectible != null)
            {
                linkedCollectible.Interact();
            }
            return;
        }

        if (mode == FakeCollectibleMode.DisappearOnInteract)
        {
            triggered = true;
            HideForever();
            ResolveRuntime();
            if (runtime != null)
            {
                string msg = string.IsNullOrWhiteSpace(approachMessage)
                    ? "RED DECOY TRAP! That fake item cost you a life."
                    : approachMessage + " Lost a life!";
                runtime.FailPlayer(msg);
            }
            return;
        }

        if (linkedCollectible != null)
        {
            linkedCollectible.Interact();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (mode != FakeCollectibleMode.VanishOnApproach || triggered ||
            other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        triggered = true;
        StartCoroutine(VanishAndRelocate());
    }

    private IEnumerator VanishAndRelocate()
    {
        SetVisibleAndCollidable(false);
        ResolveRuntime();
        if (runtime != null)
        {
            runtime.ShowRageMessage(approachMessage, 1.8f);
        }

        yield return new WaitForSeconds(1.15f);
        if (runtime != null && runtime.levelNumber == 4)
        {
            transform.position = runtime.GetRandomRelicDisplacementPosition(transform.position);
        }
        else
        {
            transform.position = relocation;
        }
        SetVisibleAndCollidable(true);
        if (runtime != null)
        {
            runtime.ShowRageMessage("The relic displaced to an unpredictable spot! Good luck tracking it.", 2.1f);
        }
    }

    private void ResolveRuntime()
    {
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }
    }

    private void HideForever()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }

        foreach (Collider collider in GetComponents<Collider>())
        {
            collider.enabled = false;
        }
    }

    private void SetVisibleAndCollidable(bool visible)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = visible;
        }

        foreach (Collider collider in GetComponents<Collider>())
        {
            if (collider != approachTrigger)
            {
                collider.enabled = visible;
            }
        }

        if (mainCollider != null)
        {
            mainCollider.enabled = visible;
        }
    }
}
