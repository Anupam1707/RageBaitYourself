using UnityEngine;

/// <summary>
/// Serialized trigger volume that updates the shared revive position.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private RageLevelRuntime runtime;
    [SerializeField] private Vector3 respawnPosition;
    [SerializeField] private string checkpointMessage = "Checkpoint reached.";
    [SerializeField] private bool useOwnPosition = true;

    private bool activated;

    public void Configure(RageLevelRuntime levelRuntime, Vector3 position, string message)
    {
        runtime = levelRuntime;
        respawnPosition = position;
        checkpointMessage = message;
        useOwnPosition = false;
    }

    private void Awake()
    {
        if (useOwnPosition)
        {
            respawnPosition = transform.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        activated = true;
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }

        if (runtime != null)
        {
            runtime.ActivateCheckpoint(respawnPosition, checkpointMessage);
        }
    }
}
