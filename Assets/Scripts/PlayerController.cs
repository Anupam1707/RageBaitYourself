using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Single unified player controller across all levels (LV1 - LV6).
/// Handles first-person locomotion, mouse look, jumping, interactions,
/// camera management, scrambled controls, recoil, and hazard/level triggers.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Locomotion")]
    public float moveSpeed = 6f;
    public float mouseSensitivity = 2.4f;
    public float jumpHeight = 2.3f;
    public float gravity = -22f;
    public bool allowJump;
    public float fallDeathY = -8f;

    [Header("Camera & Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 5f;
    [SerializeField] private bool interactionEnabled = true;
    [SerializeField] private bool shootingEnabled;

    [Header("LV1 Specific References")]
    [SerializeField] private RageHUD hud;
    [SerializeField] private TextMeshProUGUI textBoxScore;
    [SerializeField] private TextMeshProUGUI textBoxRevive;
    [SerializeField] private TMP_Text spinnerSignText;
    [SerializeField] private AudioSource coinCollectSound;
    [SerializeField] private AudioSource treasureCollectSound;
    [SerializeField] private AudioSource blockSound;

    public int score = 0;
    public int revives = -1;

    // Movement & state variables
    private CharacterController characterController;
    private RageLevelRuntime runtime;
    private InteractableObject currentInteractable;
    private float verticalVelocity;
    private float pitch;
    private bool movementEnabled = true;
    private bool cursorLocked = true;
    private bool wasGrounded;
    private float lastLandingFeedbackTime = -10f;
    private Vector3 externalVelocity;

    // Level 4/5/6 directional scramble
    private Vector2 keyDirW = Vector2.up;
    private Vector2 keyDirS = Vector2.down;
    private Vector2 keyDirA = Vector2.left;
    private Vector2 keyDirD = Vector2.right;
    private float nextDirectionShuffleTime;
    private bool shuffleInitialized;

    // Level 1 specific state
    private const int TotalCoins = 21;
    private bool transitioning;
    private bool respawning;
    private bool isInvulnerable;
    private Vector3 checkpointPosition;
    private Quaternion checkpointRotation;
    private Coroutine legacyMessageRoutine;
    private readonly HashSet<int> previousControllerContacts = new HashSet<int>();
    private readonly HashSet<int> currentControllerContacts = new HashSet<int>();
    private readonly HashSet<int> collectedCoinIds = new HashSet<int>();

    // Public properties
    public Camera PlayerCamera => playerCamera;
    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public bool MovementEnabled => movementEnabled;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        ConfigureCharacterController();
        collectedCoinIds.Clear();
    }

    private void Start()
    {
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }

        SetPlayerCameraActive(true);
        LockCursor(false);

        checkpointPosition = transform.position;
        checkpointRotation = transform.rotation;

        if (hud == null)
        {
            hud = FindFirstObjectByType<RageHUD>();
        }

        if (spinnerSignText == null)
        {
            GameObject spinnerObj = GameObject.Find("Maze Sign - Spinner");
            if (spinnerObj != null)
            {
                spinnerSignText = spinnerObj.GetComponent<TMP_Text>();
            }
        }

        HookUpHudButtons();
        EnsureCameraHintOverlay();
        UpdateCameraHintOverlay();

        // Level 1 HUD initialization if running in Level 1
        if (hud != null && (runtime == null || runtime.levelNumber == 1))
        {
            hud.SetTitle("LV1  /  MAZE OF BAD DECISIONS");
            hud.SetObjective(GetObjectiveText());
            hud.SetScore(score);
            hud.SetRevives(revives);
            hud.SetPrompt("<color=#ffe066>CLICK TO ACCESS CAMERA</color>  •  WASD to move  •  [G] Give Up");
            hud.ShowMessage("WELCOME TO THE MAZE. THE WALLS ARE WATCHING.\n<size=75%><color=#ffe066>[ Click anywhere to access camera & mouse control ]</color></size>", 3.5f);
        }
        else if (hud == null && textBoxScore != null)
        {
            ResolveLegacyTextReferences();
            if (textBoxScore != null) RageRuntimeUtility.ApplyCinzel(textBoxScore);
            if (textBoxRevive != null) RageRuntimeUtility.ApplyCinzel(textBoxRevive);
            ShowMessage("Happy RageBaiting", 5f);
        }
    }

    public void ConfigureRuntime(RageLevelRuntime levelRuntime)
    {
        runtime = levelRuntime;
    }

    public void Initialize(RageLevelRuntime levelRuntime, bool canJump)
    {
        runtime = levelRuntime;
        allowJump = canJump;
        ConfigureCharacterController();
        SetPlayerCameraActive(true);
    }

    public void SetCameraReference(Camera sceneCamera)
    {
        playerCamera = sceneCamera;
    }

    private void ConfigureCharacterController()
    {
        if (characterController == null)
        {
            return;
        }

        characterController.height = 1.8f;
        characterController.radius = 0.2f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.skinWidth = 0.05f;
        characterController.stepOffset = 0.35f;
        characterController.slopeLimit = 50f;
    }

    private void Update()
    {
        if (runtime == null)
        {
            runtime = RageLevelRuntime.Active;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (runtime != null)
            {
                runtime.GiveUp();
            }
            else
            {
                ShowLevels();
            }
            return;
        }

        HandleCursor();

        if (!movementEnabled || characterController == null)
        {
            if (runtime != null && runtime.HUD != null)
            {
                runtime.HUD.SetPrompt(string.Empty);
            }
            return;
        }

        if (cursorLocked)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            transform.Rotate(Vector3.up, mouseX * mouseSensitivity, Space.World);
            pitch = Mathf.Clamp(pitch - mouseY * mouseSensitivity, -78f, 78f);
            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        Vector3 direction = Vector3.zero;

        if (!IsMovementAllowed())
        {
            direction = Vector3.zero;
        }
        else if (ShouldScrambleDirections())
        {
            if (!shuffleInitialized)
            {
                shuffleInitialized = true;
                nextDirectionShuffleTime = Time.time + Random.Range(3.5f, 5.0f);
            }

            if (Time.time >= nextDirectionShuffleTime)
            {
                ShuffleDirectionMappings();
            }

            Vector2 input2D = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input2D += keyDirW;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input2D += keyDirS;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input2D += keyDirA;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input2D += keyDirD;

            if (input2D.sqrMagnitude > 1f)
            {
                input2D.Normalize();
            }

            direction = transform.right * input2D.x + transform.forward * input2D.y;
        }
        else
        {
            float horizontal = ReadAxis(KeyCode.A, KeyCode.D, KeyCode.LeftArrow, KeyCode.RightArrow);
            float vertical = ReadAxis(KeyCode.S, KeyCode.W, KeyCode.DownArrow, KeyCode.UpArrow);

            direction = transform.right * horizontal + transform.forward * vertical;
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }
        }

        bool grounded = characterController.isGrounded;
        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (allowJump && grounded && IsMovementAllowed() && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 totalMovement = (direction * moveSpeed + Vector3.up * verticalVelocity + externalVelocity) * Time.deltaTime;
        CollisionFlags flags = characterController.Move(totalMovement);

        if (externalVelocity.sqrMagnitude > 0.01f)
        {
            externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, Time.deltaTime * 6f);
        }
        else
        {
            externalVelocity = Vector3.zero;
        }

        bool nowGrounded = (flags & CollisionFlags.Below) != 0 || characterController.isGrounded;
        if (nowGrounded && !wasGrounded && Time.time - lastLandingFeedbackTime > 0.7f && runtime != null && allowJump)
        {
            lastLandingFeedbackTime = Time.time;
            runtime.NotifyLanding();
        }

        wasGrounded = nowGrounded;

        if (transform.position.y < fallDeathY && runtime != null)
        {
            runtime.FailPlayer("You missed the landing.");
        }

        HandleLevelInput();
    }

    private void LateUpdate()
    {
        previousControllerContacts.Clear();
        previousControllerContacts.UnionWith(currentControllerContacts);
        currentControllerContacts.Clear();
    }

    private void HandleLevelInput()
    {
        if (runtime == null || runtime.HUD == null)
        {
            return;
        }

        currentInteractable = null;
        if (interactionEnabled && playerCamera != null && playerCamera.isActiveAndEnabled &&
            Physics.Raycast(GetAimRay(), out RaycastHit hit, interactionDistance))
        {
            InteractableObject candidate = hit.collider.GetComponentInParent<InteractableObject>();
            if (candidate != null && candidate.CanInteract &&
                Vector3.Distance(playerCamera.transform.position, candidate.transform.position) <= candidate.InteractionRange + 0.5f)
            {
                currentInteractable = candidate;
            }
        }

        if (!cursorLocked)
        {
            runtime.HUD.SetPrompt("<color=#ffe066>CLICK ANYWHERE TO ACCESS CAMERA / MOUSE LOOK</color>  •  [G] Give Up");
        }
        else if (currentInteractable != null)
        {
            runtime.HUD.SetPrompt(currentInteractable.InteractionPrompt + "  [E / LMB]  •  [Esc] Free Mouse  •  [G] Give Up");
        }
        else
        {
            runtime.HUD.SetPrompt("[Esc] Free Mouse  •  [G] Give Up");
        }

        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            currentInteractable.TryInteract();
            return;
        }

        if (!cursorLocked || justLockedCursorThisFrame || !Input.GetMouseButtonDown(0) || IsPointerOverUi())
        {
            return;
        }

        if (shootingEnabled)
        {
            FireTargetShot();
        }
        else if (interactionEnabled && currentInteractable != null)
        {
            currentInteractable.TryInteract();
        }
    }

    private void FireTargetShot()
    {
        if (runtime == null)
        {
            return;
        }

        runtime.NotifyTargetShotFired();

        if (runtime.levelNumber == 6 || shootingEnabled)
        {
            DeviateAimRandomly(2.2f, 4.8f);
        }
        else
        {
            AddPitchRecoil(1.25f);
        }

        if (Physics.Raycast(GetAimRay(), out RaycastHit hit, 120f))
        {
            TargetController target = hit.collider.GetComponentInParent<TargetController>();
            if (target != null)
            {
                target.OnShot();
                return;
            }
        }

        runtime.OnAimMiss();
    }

    private bool justLockedCursorThisFrame = false;

    private void HandleCursor()
    {
        justLockedCursorThisFrame = false;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(false);
        }
        else if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
        {
            if (!cursorLocked)
            {
                justLockedCursorThisFrame = true;
            }
            LockCursor(true);
        }
    }

    private bool IsMovementAllowed()
    {
        if (runtime == null) return true;
        if (runtime.levelNumber == 6 && runtime.TargetRound < 3)
        {
            return false;
        }
        return true;
    }

    private bool ShouldScrambleDirections()
    {
        if (runtime == null) return false;
        if (runtime.levelNumber == 4) return true;
        if (runtime.levelNumber == 5) return true;
        if (runtime.levelNumber == 6 && runtime.TargetRound >= 3) return true;
        return false;
    }

    public void ShuffleDirectionMappings()
    {
        Vector2[] options = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        int attempts = 0;
        do
        {
            for (int i = 0; i < options.Length; i++)
            {
                int r = Random.Range(i, options.Length);
                Vector2 tmp = options[i];
                options[i] = options[r];
                options[r] = tmp;
            }
            attempts++;
        }
        while (attempts < 10 && options[0] == keyDirW && options[1] == keyDirS && options[2] == keyDirA && options[3] == keyDirD);

        keyDirW = options[0];
        keyDirS = options[1];
        keyDirA = options[2];
        keyDirD = options[3];

        nextDirectionShuffleTime = Time.time + Random.Range(3.5f, 5.5f);

        if (runtime != null)
        {
            string[] msgs = new string[]
            {
                "Controls scrambled! Adapt.",
                "Direction mapping randomized.",
                "Muscle memory revoked.",
                "WASD swapped again."
            };
            runtime.ShowRageMessage(msgs[Random.Range(0, msgs.Length)], 1.2f);
        }
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static float ReadAxis(KeyCode negativePrimary, KeyCode positivePrimary, KeyCode negativeAlternate, KeyCode positiveAlternate)
    {
        bool positive = Input.GetKey(positivePrimary) || Input.GetKey(positiveAlternate);
        bool negative = Input.GetKey(negativePrimary) || Input.GetKey(negativeAlternate);
        return positive == negative ? 0f : (positive ? 1f : -1f);
    }

    public void SetPlayerCameraActive(bool active)
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true) ?? Camera.main;
        }

        if (playerCamera == null)
        {
            return;
        }

        playerCamera.enabled = active;
        playerCamera.tag = active ? "MainCamera" : "Untagged";
        AudioListener listener = playerCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = active;
        }
    }

    public void SetDesktopCameraActive(bool active) => SetPlayerCameraActive(active);

    private GameObject cameraHintOverlay;

    private void EnsureCameraHintOverlay()
    {
        if (cameraHintOverlay != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        cameraHintOverlay = new GameObject("CameraAccessHintOverlay", typeof(RectTransform), typeof(Image));
        cameraHintOverlay.transform.SetParent(canvas.transform, false);

        RectTransform rt = cameraHintOverlay.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -80f);
        rt.sizeDelta = new Vector2(480f, 66f);

        Image img = cameraHintOverlay.GetComponent<Image>();
        img.color = new Color(0.06f, 0.08f, 0.14f, 0.90f);
        img.raycastTarget = false;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(cameraHintOverlay.transform, false);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 4f);
        textRt.offsetMax = new Vector2(-10f, -4f);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "<color=#ffe066><b>CLICK ANYWHERE TO ACCESS CAMERA</b></color>\n<size=75%><color=#b0c0d0>Move mouse to look around  •  [Esc] to free cursor</color></size>";
        tmp.fontSize = 15f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        RageRuntimeUtility.ApplyCinzel(tmp);

        cameraHintOverlay.SetActive(!cursorLocked);
    }

    private void UpdateCameraHintOverlay()
    {
        if (cameraHintOverlay == null)
        {
            EnsureCameraHintOverlay();
        }

        if (cameraHintOverlay != null)
        {
            cameraHintOverlay.SetActive(!cursorLocked);
        }
    }

    public void LockCursor(bool locked)
    {
        cursorLocked = locked;
        Cursor.visible = !locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        UpdateCameraHintOverlay();
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        if (!enabled)
        {
            LockCursor(false);
        }
    }

    public void ApplyPlatformMotion(Vector3 delta)
    {
        if (!movementEnabled || characterController == null || !characterController.enabled)
        {
            return;
        }

        characterController.Move(delta);
    }

    public void AddVerticalImpulse(float impulse)
    {
        verticalVelocity = impulse;
    }

    public void AddImpulse(Vector3 impulse)
    {
        externalVelocity += impulse;
    }

    public void AddPitchRecoil(float degrees)
    {
        pitch = Mathf.Clamp(pitch - degrees, -78f, 78f);
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    public void DeviateAimRandomly(float minDegrees, float maxDegrees)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float magnitude = Random.Range(minDegrees, maxDegrees);
        float yawDev = Mathf.Cos(angle) * magnitude;
        float pitchDev = Mathf.Sin(angle) * magnitude;

        transform.Rotate(Vector3.up, yawDev, Space.World);
        pitch = Mathf.Clamp(pitch + pitchDev, -78f, 78f);
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    public void Respawn(Vector3 position, float yaw = 0f)
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        externalVelocity = Vector3.zero;
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        pitch = 0f;
        verticalVelocity = 0f;
        if (characterController != null)
        {
            characterController.enabled = true;
        }
        movementEnabled = true;
        LockCursor(true);
    }

    public Ray GetAimRay()
    {
        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>(true);
        }
        return cam != null
            ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(transform.position, transform.forward);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 1. Generic RageHazard check (Levels 2-6)
        RageHazard hazard = hit.collider.GetComponentInParent<RageHazard>();
        if (hazard != null)
        {
            hazard.HitPlayer(this);
            return;
        }

        // 2. Contact deduplication for Level 1 collider interactions
        int colliderId = hit.collider.GetInstanceID();
        if (currentControllerContacts.Add(colliderId) && !previousControllerContacts.Contains(colliderId))
        {
            HandleCollision(hit.collider.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    private void HandleCollision(GameObject collidedObject)
    {
        if (collidedObject == null || transitioning || respawning || isInvulnerable)
        {
            return;
        }

        if (collidedObject.name == "Treasure")
        {
            HandleTreasure(collidedObject);
            return;
        }

        if (collidedObject.name == "End")
        {
            HandleEnd();
            return;
        }

        if (collidedObject.name == "DeadEnd")
        {
            PlaySound(blockSound);
            ShowMessage("THIS IS NOT WHAT YOU ARE LOOKING FOR, BUDDY.....", 2f);
            return;
        }

        if (IsCoin(collidedObject))
        {
            HandleCoin(collidedObject);
            return;
        }

        Transform parent = collidedObject.transform.parent;
        bool isObstacle = parent != null &&
            (parent.name == "StaticObst" || (parent.parent != null && parent.parent.name == "RotarObst"));

        if (isObstacle)
        {
            HandleObstacleHit();
        }
    }

    private static bool IsCoin(GameObject obj)
    {
        if (obj == null) return false;
        return obj.name == "Coin(Clone)" || obj.name == "Coin";
    }

    private void HandleTreasure(GameObject treasure)
    {
        if (revives != -1) return;

        PlaySound(treasureCollectSound);
        checkpointPosition = transform.position;
        checkpointPosition.y = Mathf.Max(checkpointPosition.y, 1f);
        checkpointRotation = transform.rotation;
        Destroy(treasure);
        revives = 3;
        UpdateHud();
        ShowMessage("TREASURE ACQUIRED. THREE REVIVES UNLOCKED.", 2.5f);
    }

    private bool AreAllCoinsCollected()
    {
        if (CoinSpawn.HasUncollectedCoins()) return false;

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && obj.activeInHierarchy && IsCoin(obj)) return false;
        }

        int required = CoinSpawn.TotalSpawnedCoins > 0 ? CoinSpawn.TotalSpawnedCoins : TotalCoins;
        return score >= required;
    }

    private void HandleEnd()
    {
        if (AreAllCoinsCollected())
        {
            transitioning = true;
            SetMovementEnabled(false);
            if (BGMCon.instance != null) BGMCon.instance.StopMusic();
            GameObject bgmObj = GameObject.Find("BGM");
            if (bgmObj != null)
            {
                AudioSource src = bgmObj.GetComponent<AudioSource>();
                if (src != null) src.Stop();
            }
            ShowMessage("EXIT OPEN. YOU SURVIVED THE MAZE.", 1.25f);
            UserAccountManager.Instance.UnlockNextLevel(1);
            UserAccountManager.Instance.RecordWin();
            UserAccountManager.Instance.RecordScore(score);
            StartCoroutine(LoadSceneAfter("Victory", 1.25f));
            return;
        }

        PlaySound(blockSound);
        ShowMessage("THE EXIT IS LOCKED. ALL COINS MUST BE COLLECTED TO WIN.", 2.5f);
    }

    private void HandleCoin(GameObject coin)
    {
        if (coin == null) return;

        int coinId = coin.GetInstanceID();
        if (collectedCoinIds.Contains(coinId)) return;
        collectedCoinIds.Add(coinId);

        CoinSpawn.UnregisterCoin(coin);
        coin.SetActive(false);

        Collider[] colliders = coin.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        PlaySound(coinCollectSound);
        Destroy(coin);
        score++;
        UpdateHud();
        ShowMessage("COIN COLLECTED. KEEP MOVING.", 1.5f);
    }

    private void HandleObstacleHit()
    {
        if (transitioning || respawning || isInvulnerable) return;

        if (revives > 0)
        {
            StartCoroutine(ReviveRoutine());
            return;
        }

        transitioning = true;
        SetMovementEnabled(false);
        if (BGMCon.instance != null) BGMCon.instance.StopMusic();
        GameObject bgm = GameObject.Find("BGM");
        if (bgm != null)
        {
            AudioSource src = bgm.GetComponent<AudioSource>();
            if (src != null) src.Stop();
        }
        PlaySound(blockSound);
        UserAccountManager.Instance.RecordDeath();
        ShowMessage("NO REVIVES LEFT. THE MAZE WINS THIS ROUND.", 1.25f);
        StartCoroutine(LoadSceneAfter("GameOver", 1.25f));
    }

    private IEnumerator ReviveRoutine()
    {
        respawning = true;
        isInvulnerable = true;
        SetMovementEnabled(false);

        revives--;
        UserAccountManager.Instance.RecordDeath();
        UpdateSpinnerSignText();
        PlaySound(blockSound);
        UpdateHud();
        ShowMessage("REVIVED TO CHECKPOINT  •  " + revives + " LEFT", 2f);

        Respawn(checkpointPosition, checkpointRotation.eulerAngles.y);
        SetMovementEnabled(false);
        Physics.SyncTransforms();

        yield return new WaitForSeconds(0.4f);
        respawning = false;
        SetMovementEnabled(true);
        LockCursor(true);

        yield return new WaitForSeconds(1.2f);
        isInvulnerable = false;
    }

    private void UpdateSpinnerSignText()
    {
        if (spinnerSignText == null)
        {
            GameObject spinnerObj = GameObject.Find("Maze Sign - Spinner");
            if (spinnerObj != null)
            {
                spinnerSignText = spinnerObj.GetComponent<TMP_Text>();
            }
        }

        if (spinnerSignText != null)
        {
            switch (revives)
            {
                case 2:
                    spinnerSignText.text = "DID THAT HURT?\nONLY 2 LIVES LEFT\nSPINNER IS UNIMPRESSED";
                    break;
                case 1:
                    spinnerSignText.text = "ARE YOU EVEN TRYING?\n1 REVIVE LEFT\nFINAL CHANCE, BUDDY";
                    break;
                case 0:
                    spinnerSignText.text = "LAST BREATH\nZERO REVIVES LEFT\nONE MORE TAP AND YOU CRY";
                    break;
                default:
                    spinnerSignText.text = $"REVIVES: {Mathf.Max(0, revives)}\nTHE BLADES KEEP SPINNING\nYOU KEEP FAILING";
                    break;
            }
        }
    }

    public void ShowLevels()
    {
        if (transitioning) return;
        MainMenu.ShowLevelsFromGame();
    }

    private void HookUpHudButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button btn in buttons)
        {
            if (btn == null) continue;
            if (btn.name == "GiveUpButton")
            {
                bool hasValidTarget = false;
                for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
                {
                    if (btn.onClick.GetPersistentTarget(i) != null)
                    {
                        hasValidTarget = true;
                        break;
                    }
                }

                if (!hasValidTarget)
                {
                    btn.onClick.AddListener(ShowLevels);
                }
            }
        }
    }

    private IEnumerator LoadSceneAfter(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    private void UpdateHud()
    {
        if (hud != null)
        {
            hud.SetScore(score);
            hud.SetRevives(revives);
            hud.SetObjective(GetObjectiveText());
            return;
        }

        if (textBoxRevive != null)
        {
            textBoxRevive.text = "Revives Left : " + Mathf.Max(0, revives);
        }
    }

    private string GetObjectiveText()
    {
        if (revives < 0) return "Collect all coins & find treasure to unlock revives.";
        return "Reach the exit. All coins must be collected.";
    }

    private void ShowMessage(string message, float seconds)
    {
        if (hud != null)
        {
            hud.ShowMessage(message, seconds);
            return;
        }

        if (textBoxScore == null) return;

        if (legacyMessageRoutine != null)
        {
            StopCoroutine(legacyMessageRoutine);
        }

        legacyMessageRoutine = StartCoroutine(ShowLegacyMessage(message, seconds));
    }

    private IEnumerator ShowLegacyMessage(string message, float seconds)
    {
        textBoxScore.text = message;
        yield return new WaitForSeconds(seconds);

        if (textBoxScore != null)
        {
            textBoxScore.text = string.Empty;
        }

        legacyMessageRoutine = null;
    }

    private void ResolveLegacyTextReferences()
    {
        if (textBoxScore == null)
        {
            GameObject scoreObject = GameObject.Find("Score");
            if (scoreObject != null)
            {
                textBoxScore = scoreObject.GetComponent<TextMeshProUGUI>();
            }
        }

        if (textBoxRevive == null)
        {
            GameObject reviveObject = GameObject.Find("Revives");
            if (reviveObject != null)
            {
                textBoxRevive = reviveObject.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private static void PlaySound(AudioSource source)
    {
        if (source != null)
        {
            source.Play();
        }
    }
}

/// <summary>
/// Shared font styling utility for Cinzel runtime text.
/// </summary>
public static class RageRuntimeUtility
{
    private static TMP_FontAsset cinzelFont;

    public static void ApplyCinzel(TMP_Text textComponent)
    {
        if (textComponent == null) return;

        if (cinzelFont == null)
        {
            cinzelFont = Resources.Load<TMP_FontAsset>("Cinzel-Regular SDF");
            if (cinzelFont == null)
            {
                cinzelFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Cinzel-Regular SDF");
            }

#if UNITY_EDITOR
            if (cinzelFont == null)
            {
                cinzelFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/Resources/Cinzel-Regular SDF.asset");
            }
#endif
        }

        if (cinzelFont != null)
        {
            textComponent.font = cinzelFont;
        }

        textComponent.fontStyle = FontStyles.Bold;
    }
}
