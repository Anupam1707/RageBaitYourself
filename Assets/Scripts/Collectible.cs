using UnityEngine;

/// <summary>
/// One-shot collectible behavior for a serialized level object.
/// </summary>
public class Collectible : MonoBehaviour
{
    [SerializeField] private RageLevelRuntime runtime;
    [SerializeField] private string itemName = "Collectible";
    [SerializeField] private int points = 1;

    private bool collected;

    public string ItemName => string.IsNullOrWhiteSpace(itemName) ? "Collectible" : itemName;
    public int Points => Mathf.Max(0, points);
    public bool IsCollected => collected;

    public void Configure(RageLevelRuntime levelRuntime, string name, int value = 1)
    {
        runtime = levelRuntime;
        itemName = string.IsNullOrWhiteSpace(name) ? "Collectible" : name;
        points = Mathf.Max(0, value);
    }

    public void Interact()
    {
        CompleteCollection();
    }

    public void CompleteCollection()
    {
        if (collected)
        {
            return;
        }

        collected = true;
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }

        if (runtime != null)
        {
            runtime.CollectRelic(this);
        }

        gameObject.SetActive(false);
    }
}
