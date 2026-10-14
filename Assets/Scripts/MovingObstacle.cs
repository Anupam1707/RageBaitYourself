using UnityEngine;

public enum RageMotionMode
{
    Translate,
    Rotate
}

/// <summary>
/// Deterministic translation/rotation motion for scene-authored obstacles and
/// platforms. It moves existing transforms only.
/// </summary>
public class MovingObstacle : MonoBehaviour
{
    [SerializeField] private RageLevelRuntime runtime;
    [SerializeField] private RageMotionMode mode = RageMotionMode.Translate;
    [SerializeField] private Vector3 axis = Vector3.right;
    [SerializeField] private float amplitude = 1f;
    [SerializeField] private float speed = 1f;
    [SerializeField] private bool carryPlayer;
    [SerializeField] private Vector2 rideHalfExtents = Vector2.one;
    [SerializeField] private float platformTopY;
    [SerializeField] private bool changeTimingAfterCheckpoint;
    [SerializeField] private int checkpointForTimingChange = 1;
    [SerializeField] private float changedTimingPhase = 1.4f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool timingChangeReported;

    public void ConfigureTranslation(
        RageLevelRuntime levelRuntime,
        Vector3 movementAxis,
        float movementAmplitude,
        float movementSpeed,
        bool shouldCarryPlayer = false,
        Vector2 platformHalfExtents = default,
        float topY = 0f)
    {
        runtime = levelRuntime;
        mode = RageMotionMode.Translate;
        axis = movementAxis;
        amplitude = movementAmplitude;
        speed = movementSpeed;
        carryPlayer = shouldCarryPlayer;
        rideHalfExtents = platformHalfExtents;
        platformTopY = topY;
    }

    public void ConfigureRotation(RageLevelRuntime levelRuntime, Vector3 rotationAxis, float degreesPerSecond)
    {
        runtime = levelRuntime;
        mode = RageMotionMode.Rotate;
        axis = rotationAxis;
        speed = degreesPerSecond;
    }

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        axis = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.right;
        speed = Mathf.Max(0f, speed);
    }

    private void Start()
    {
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }
    }

    private void Update()
    {
        bool timingChanged = changeTimingAfterCheckpoint && runtime != null &&
                             runtime.CheckpointsReached >= Mathf.Max(1, checkpointForTimingChange);
        if (timingChanged && !timingChangeReported)
        {
            timingChangeReported = true;
            runtime.ShowRageMessage("The sweeper changed its rhythm after the checkpoint.", 1.8f);
        }

        if (mode == RageMotionMode.Translate)
        {
            float phase = timingChanged ? changedTimingPhase : 0f;
            Vector3 nextPosition = startPosition + axis * (Mathf.Sin(Time.time * speed + phase) * amplitude);
            Vector3 delta = nextPosition - transform.position;
            transform.position = nextPosition;
            CarryPlayer(delta);
        }
        else
        {
            transform.rotation = startRotation * Quaternion.AngleAxis(Time.time * speed, axis);
        }
    }

    private void CarryPlayer(Vector3 delta)
    {
        if (!carryPlayer || runtime == null || runtime.Player == null)
        {
            return;
        }

        Vector3 playerPosition = runtime.ActivePlayerPosition;
        Vector3 platformPosition = transform.position;
        bool nearTop = Mathf.Abs(playerPosition.y - platformTopY) < 0.45f;
        bool overPlatform = Mathf.Abs(playerPosition.x - platformPosition.x) <= rideHalfExtents.x &&
                            Mathf.Abs(playerPosition.z - platformPosition.z) <= rideHalfExtents.y;

        if (nearTop && overPlatform)
        {
            runtime.ApplyPlatformMotion(delta);
        }
    }
}
