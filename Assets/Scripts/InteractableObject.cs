using UnityEngine;

public enum InteractableAction
{
    Collectible,
    ApproachRelic,
    MovingRelic,
    FinalRelic,
    FakeCollectible,
    ClueChest,
    EscapeKey1,
    EscapeKey2,
    EscapeDrawer,
    EscapeFakeItem,
    EscapeKey3,
    EscapeLever,
    EscapeButton,
    ApparentDoor,
    RealDoor
}

/// <summary>
/// Serialized interaction metadata. The level runtime interprets the action;
/// this component never creates an interaction target or callback at runtime.
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [SerializeField] private RageLevelRuntime runtime;
    [SerializeField] private string interactionPrompt = "Interact";
    [SerializeField] private float interactionRange = 4f;
    [SerializeField] private InteractableAction action = InteractableAction.Collectible;
    [SerializeField] private Transform relocationTarget;
    [SerializeField] private string movedPrompt = "Collect";
    [SerializeField] private bool canInteract = true;

    private bool actionCompleted;

    public string InteractionPrompt => string.IsNullOrWhiteSpace(interactionPrompt) ? "Interact" : interactionPrompt;
    public float InteractionRange => Mathf.Max(0.5f, interactionRange);
    public bool CanInteract => canInteract;
    public InteractableAction Action => action;
    public Transform RelocationTarget => relocationTarget;
    public string MovedPrompt => movedPrompt;
    public bool ActionCompleted => actionCompleted;

    public void Configure(
        RageLevelRuntime levelRuntime,
        string prompt,
        InteractableAction interactionAction,
        float range = 4f,
        Transform moveTarget = null,
        string promptAfterMove = "Collect")
    {
        runtime = levelRuntime;
        interactionPrompt = string.IsNullOrWhiteSpace(prompt) ? "Interact" : prompt;
        action = interactionAction;
        interactionRange = range;
        relocationTarget = moveTarget;
        movedPrompt = promptAfterMove;
    }

    public void SetInteractable(bool value)
    {
        canInteract = value;
    }

    public void SetInteractionPrompt(string prompt)
    {
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            interactionPrompt = prompt;
        }
    }

    public void MarkActionCompleted()
    {
        actionCompleted = true;
    }

    public bool TryInteract()
    {
        if (!canInteract)
        {
            return false;
        }

        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }

        if (runtime != null)
        {
            runtime.HandleInteraction(this);
        }
        else
        {
            Debug.LogWarning("InteractableObject has no RageLevelRuntime owner.", this);
        }

        return true;
    }
}
