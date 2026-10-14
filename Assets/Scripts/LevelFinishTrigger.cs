using TMPro;
using UnityEngine;

public enum FinishRequirement
{
    Always,
    Level2FinishTroll,
    Level4Relics
}

/// <summary>
/// Serialized finish volume. Requirements and the visual bait response are
/// scene data; the level runtime owns the state transition.
/// </summary>
public class LevelFinishTrigger : MonoBehaviour
{
    [SerializeField] private RageLevelRuntime runtime;
    [SerializeField] private FinishRequirement requirement = FinishRequirement.Always;
    [SerializeField] private string deniedMessage = "Not yet. The game kept one tiny condition.";
    [SerializeField] private Renderer baitRenderer;
    [SerializeField] private Material unlockedMaterial;
    [SerializeField] private TextMeshPro baitLabel;
    [SerializeField] private string unlockedLabel = "OKAY, NOW FINISH";

    private bool deniedShown;
    private bool unlocked;

    public FinishRequirement Requirement => requirement;
    public string DeniedMessage => deniedMessage;
    public bool IsUnlocked => unlocked;

    public void Configure(
        RageLevelRuntime levelRuntime,
        FinishRequirement finishRequirement,
        string message = "Not yet. The game kept one tiny condition.")
    {
        runtime = levelRuntime;
        requirement = finishRequirement;
        deniedMessage = message;
    }

    public void ConfigureBaitVisuals(Renderer renderer, Material material, TextMeshPro label, string labelAfterUnlock)
    {
        baitRenderer = renderer;
        unlockedMaterial = material;
        baitLabel = label;
        unlockedLabel = labelAfterUnlock;
    }

    public void UnlockVisuals()
    {
        unlocked = true;
        if (baitRenderer != null && unlockedMaterial != null)
        {
            baitRenderer.sharedMaterial = unlockedMaterial;
        }

        if (baitLabel != null && !string.IsNullOrWhiteSpace(unlockedLabel))
        {
            baitLabel.text = unlockedLabel;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            TryFinish();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            TryFinish();
        }
    }

    private void TryFinish()
    {
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }

        if (runtime == null || runtime.IsTransitioning)
        {
            return;
        }

        if (runtime.HandleFinish(this))
        {
            return;
        }

        if (!deniedShown)
        {
            deniedShown = true;
            runtime.ShowRageMessage(deniedMessage, 2.1f);
        }
    }
}
