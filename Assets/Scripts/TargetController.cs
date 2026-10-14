using UnityEngine;

/// <summary>
/// One serialized target in the LV6 shooting sequence.
/// </summary>
public class TargetController : MonoBehaviour
{
    [SerializeField] private RageLevelRuntime runtime;
    [SerializeField] private bool valid = true;
    [SerializeField] private int points = 1;
    [SerializeField] private string label = "VALID";
    [SerializeField] private int round = 1;
    [SerializeField] private Vector3 movementAxis = Vector3.right;
    [SerializeField] private float movementAmplitude;
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float visibilityPeriod = 2.1f;
    [SerializeField] private float visibleDuration = 0.72f;
    [SerializeField] private float visibilityOffset;
    [SerializeField] private Vector3 evadePosition;
    [SerializeField] private bool movesOnFire;

    private Collider targetCollider;
    private Renderer[] renderers;
    private Vector3 startPosition;
    private bool evadeUsed;
    private bool hasMotion;
    private bool hasVisibility;
    private bool available = true;
    private bool visible = true;

    public bool IsValid => valid;
    public int Points => Mathf.Max(0, points);
    public int Round => round;
    public bool IsAvailable => available;

    public void Configure(RageLevelRuntime levelRuntime, bool targetIsValid, int targetPoints, string targetLabel, int targetRound)
    {
        runtime = levelRuntime;
        valid = targetIsValid;
        points = targetPoints;
        label = targetLabel;
        round = Mathf.Max(1, targetRound);
    }

    public void ConfigureMotion(Vector3 axis, float amplitude, float speed)
    {
        movementAxis = axis;
        movementAmplitude = amplitude;
        movementSpeed = speed;
        hasMotion = true;
    }

    public void ConfigureVisibility(float period, float duration, float offset)
    {
        visibilityPeriod = Mathf.Max(0.1f, period);
        visibleDuration = Mathf.Clamp(duration, 0.05f, visibilityPeriod);
        visibilityOffset = offset;
        hasVisibility = true;
    }

    public void ConfigureEvadeOnFire(Vector3 position)
    {
        evadePosition = position;
        movesOnFire = true;
    }

    private void Awake()
    {
        targetCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>(true);
        startPosition = transform.position;
        movementAxis = movementAxis.sqrMagnitude > 0.001f ? movementAxis.normalized : Vector3.right;
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }
    }

    private void OnEnable()
    {
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }

        if (runtime != null)
        {
            runtime.RegisterTarget(this);
        }
    }

    private void Start()
    {
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }

        if (runtime != null)
        {
            runtime.RegisterTarget(this);
        }
    }

    public void PrepareForRound(bool active)
    {
        available = active;
        evadeUsed = false;
        if (active)
        {
            transform.position = startPosition;
            SetVisible(true);
        }
        else
        {
            SetVisible(false);
        }
    }

    public void NotifyPlayerFired()
    {
        if (!available || !visible || !movesOnFire || evadeUsed)
        {
            return;
        }

        evadeUsed = true;
        transform.position = evadePosition;
        if (runtime != null)
        {
            runtime.ShowRageMessage("It moved after you fired. That was the bit.", 1.7f);
        }
    }

    public void OnShot()
    {
        if (!available || !visible)
        {
            return;
        }

        available = false;
        SetVisible(false);
        if (runtime != null)
        {
            runtime.TargetWasShot(this);
        }
    }

    private void Update()
    {
        if (!available)
        {
            return;
        }

        if (hasMotion)
        {
            transform.position = startPosition + movementAxis * (Mathf.Sin(Time.time * movementSpeed) * movementAmplitude);
        }

        if (hasVisibility)
        {
            float cycle = Mathf.Repeat(Time.time + visibilityOffset, visibilityPeriod);
            SetVisible(cycle < visibleDuration);
        }
    }

    private void SetVisible(bool value)
    {
        visible = value;
        if (targetCollider != null)
        {
            targetCollider.enabled = value && available;
        }

        if (renderers == null)
        {
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = value && available;
            }
        }
    }

    private void OnDisable()
    {
        if (runtime != null)
        {
            runtime.UnregisterTarget(this);
        }
    }

    private void OnDestroy()
    {
        if (runtime != null)
        {
            runtime.UnregisterTarget(this);
        }
    }
}
