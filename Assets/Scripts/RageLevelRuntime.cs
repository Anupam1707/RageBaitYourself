using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central level manager and state mediator across all levels (LV1 - LV6).
/// Manages level progression, objective states, revives, checkpointing,
/// puzzle validations, target rounds, and HUD coordination.
/// </summary>
public class RageLevelRuntime : MonoBehaviour
{
    [Header("Level")]
    public int levelNumber = 1;

    [Header("Serialized Scene References")]
    [SerializeField] private RageHUD hud;
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private Transform worldRoot;
    [SerializeField] private TargetController[] sceneTargets = new TargetController[0];

    [Header("BGM / Audio Settings")]
    [SerializeField] private AudioClip levelBgm;
    [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.5f;
    [SerializeField] private bool loopBgm = true;
    [SerializeField] private AudioSource bgmSource;

    [Header("LV5 Scene References")]
    [SerializeField] private GameObject level5Key2;
    [SerializeField] private GameObject level5FakeItem;
    [SerializeField] private GameObject level5RealDoor;
    [SerializeField] private Transform level5Key2RevealPoint;
    [SerializeField] private InteractableObject level5Key3Interaction;
    [SerializeField] private Renderer level5Key3Renderer;
    [SerializeField] private Renderer level5DrawerRenderer;
    [SerializeField] private Material level5DrawerOpenMaterial;
    [SerializeField] private Material level5Key3UnlockedMaterial;
    [SerializeField] private Renderer level5ApparentDoorRenderer;
    [SerializeField] private Material level5ApparentDoorOpenMaterial;

    public static RageLevelRuntime Active { get; private set; }
    public RageHUD HUD => hud;
    public PlayerController Player => player;
    public Transform WorldRoot => worldRoot;
    public Vector3 ActivePlayerPosition => player != null ? player.transform.position : Vector3.zero;
    public bool IsTransitioning { get; private set; }
    public int CheckpointsReached => checkpointsReached;
    public int Score { get; private set; }
    public int RelicsFound { get; private set; }
    public int EscapeKeysFound { get; private set; }
    public int TargetRound => targetRound;

    private readonly List<TargetController> activeTargets = new List<TargetController>();
    private Vector3 checkpointPosition;
    private int revives = 3;
    private bool respawning;
    private int checkpointsReached;
    private int targetRound;
    private int roundHits;
    private bool advancingTargets;
    private bool level4ExitUnlocked;
    private bool level2FinishTrollSeen;
    private bool level2FinishUnlocked;
    private bool level5LeverPulled;
    private bool level5DoorTrollSeen;
    private bool level5Key2Displaced;
    private bool level5DrawerOpened;
    private Coroutine level5LightRoutine;

    private static readonly Vector3[] RelicGroundPositions = new Vector3[]
    {
        // West Crypt Ground
        new Vector3(-17.5f, 1.2f, -3.0f),
        new Vector3(-14.0f, 1.2f, 4.0f),
        new Vector3(-16.0f, 1.2f, 12.0f),
        new Vector3(-11.0f, 1.2f, 18.0f),
        new Vector3(-15.5f, 1.2f, 22.0f),
        // East Garden Ground
        new Vector3(17.0f, 1.2f, -5.0f),
        new Vector3(13.0f, 1.2f, 2.0f),
        new Vector3(16.5f, 1.2f, 10.0f),
        new Vector3(11.5f, 1.2f, 16.0f),
        new Vector3(18.0f, 1.2f, 21.0f),
        // Shrine Ground
        new Vector3(-8.0f, 1.2f, 26.0f),
        new Vector3(0.0f, 1.2f, 32.0f),
        new Vector3(7.5f, 1.2f, 27.0f),
        new Vector3(-5.0f, 1.2f, 35.0f),
        new Vector3(6.0f, 1.2f, 36.0f),
        // Courtyard Ground
        new Vector3(-6.0f, 1.2f, -14.0f),
        new Vector3(5.5f, 1.2f, -15.0f),
        new Vector3(0.0f, 1.2f, -9.0f),
        new Vector3(-8.5f, 1.2f, -6.0f),
        new Vector3(7.0f, 1.2f, -8.0f)
    };

    public void ConfigureSceneReferences(
        RageHUD sceneHud,
        PlayerController scenePlayer,
        Transform scenePlayerSpawn,
        Transform sceneWorldRoot,
        TargetController[] targets)
    {
        hud = sceneHud;
        player = scenePlayer;
        playerSpawn = scenePlayerSpawn;
        worldRoot = sceneWorldRoot;
        sceneTargets = targets ?? new TargetController[0];
    }

    public void ConfigureLevel5References(
        GameObject key2,
        GameObject fakeItem,
        GameObject realDoor,
        Transform key2RevealPoint,
        InteractableObject key3,
        Renderer key3Renderer,
        Renderer drawerRenderer,
        Material drawerOpenMaterial,
        Material key3UnlockedMaterial,
        Renderer apparentDoorRenderer,
        Material apparentDoorOpenMaterial)
    {
        level5Key2 = key2;
        level5FakeItem = fakeItem;
        level5RealDoor = realDoor;
        level5Key2RevealPoint = key2RevealPoint;
        level5Key3Interaction = key3;
        level5Key3Renderer = key3Renderer;
        level5DrawerRenderer = drawerRenderer;
        level5DrawerOpenMaterial = drawerOpenMaterial;
        level5Key3UnlockedMaterial = key3UnlockedMaterial;
        level5ApparentDoorRenderer = apparentDoorRenderer;
        level5ApparentDoorOpenMaterial = apparentDoorOpenMaterial;
    }

    private void Awake()
    {
        Active = this;
        ResolveSceneReferences();
    }

    private void Start()
    {
        ResolveSceneReferences();
        if (player == null)
        {
            Debug.LogError("RageLevelRuntime requires a serialized PlayerController.", this);
            return;
        }

        if (hud == null)
        {
            Debug.LogError("RageLevelRuntime requires a serialized RageHUD.", this);
        }

        player.ConfigureRuntime(this);

        checkpointPosition = playerSpawn != null ? playerSpawn.position : player.transform.position;
        if (hud != null)
        {
            hud.SetTitle(GetLevelTitle());
            hud.SetObjective(GetLevelObjective());
            hud.SetScore(0);
            hud.SetRevives(revives);
        }

        SetupBgm();

        ApplySignFontSizeConsistency();

        if (levelNumber == 4)
        {
            DisplaceRelicsAtStart();
        }
        else if (levelNumber == 5)
        {
            level5LightRoutine = StartCoroutine(Level5LightFlickerRoutine());
        }
        else if (levelNumber == 6)
        {
            targetRound = 1;
            roundHits = 0;
            ActivateShootingRound(targetRound);
            UpdateShootingObjective();
        }

        ShowRageMessage(GetIntroMessage(), 3.5f);
    }

    private void ResolveSceneReferences()
    {
        if (hud == null)
        {
            hud = GetComponentInChildren<RageHUD>(true);
        }

        if (player == null)
        {
            player = GetComponentInChildren<PlayerController>(true);
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }
        }

        if (playerSpawn == null && player != null)
        {
            playerSpawn = player.transform;
        }

        if (worldRoot == null)
        {
            Transform candidate = transform.Find("World");
            worldRoot = candidate != null ? candidate : transform;
        }

        if ((sceneTargets == null || sceneTargets.Length == 0) && levelNumber == 6)
        {
            sceneTargets = GetComponentsInChildren<TargetController>(true);
        }
    }

    private string GetLevelTitle()
    {
        switch (levelNumber)
        {
            case 1: return "LV1  /  MAZE OF BAD DECISIONS";
            case 2: return "LV2  /  ROOTFALL RUN";
            case 3: return "LV3  /  CANOPY ASCENT";
            case 4: return "LV4  /  HOLLOW CROWN";
            case 5: return "LV5  /  ROOTBOUND ARCHIVE";
            case 6: return "LV6  /  LANTERN TRIAL";
            default: return "RAGEBAIT YOURSELF";
        }
    }

    private string GetLevelObjective()
    {
        switch (levelNumber)
        {
            case 1: return "Collect all coins and navigate the moving obstacles to reach the finish gate.  •  WASD/Arrows + Space";
            case 2: return "Reach the root gate. Time the sweepers and cross the moving bridge.  •  WASD/Arrows + Space";
            case 3: return "Climb the canopy. Watch each landing; the next branch is always in view.  •  WASD/Arrows + Space";
            case 4: return "Relics Found: 0/5  •  Explore the four forest grounds  •  Look & press E/LMB";
            case 5: return "Components: 0/3  •  Search the archive, work the lever, find the real exit  •  E/LMB";
            case 6: return "Round 1/5  •  Aim with the crosshair, click to shoot  •  Read the target marks";
            default: return "Complete the level.";
        }
    }

    private string GetIntroMessage()
    {
        const string hint = "\n<size=75%><color=#ffe066>[ Click anywhere to access camera & mouse control ]</color></size>";
        switch (levelNumber)
        {
            case 1: return "WELCOME TO THE MAZE. THE WALLS ARE WATCHING." + hint;
            case 2: return "The forest has a rhythm. Learn it. Then notice when it changes." + hint;
            case 3: return "Take the high route. Some promises carved into bark are questionable." + hint;
            case 4: return "Five relics are real. The forest would like you to think otherwise." + hint;
            case 5: return "Three components, one archive, and an exit with a very specific attitude." + hint;
            case 6: return "The lantern marks are honest. The targets are not all so considerate." + hint;
            default: return "Happy ragebaiting." + hint;
        }
    }

    public void AddScore(int amount)
    {
        Score += amount;
        if (hud != null)
        {
            hud.SetScore(Score);
        }
    }

    public void ShowRageMessage(string message, float seconds = 2.2f)
    {
        if (hud != null)
        {
            hud.ShowMessage(message, seconds);
        }
    }

    public void GiveUp()
    {
        if (IsTransitioning)
        {
            return;
        }

        IsTransitioning = true;
        SetPlayerMovementEnabled(false);
        if (BGMCon.instance != null) BGMCon.instance.StopMusic();
        if (bgmSource != null) bgmSource.Stop();
        UserAccountManager.Instance.RecordDeath();

        MainMenu.ShowLevelsFromGame();
    }

    public void NotifyLanding()
    {
        ShowRageMessage("Clean landing. The platform is taking notes.", 1.05f);
    }

    public void ActivateCheckpoint(Vector3 position, string message)
    {
        checkpointsReached++;
        checkpointPosition = position;
        ShowRageMessage(message, 1.55f);
    }

    public void FailPlayer(string message)
    {
        if (IsTransitioning || respawning || player == null)
        {
            return;
        }

        if (revives <= 0)
        {
            IsTransitioning = true;
            SetPlayerMovementEnabled(false);
            if (BGMCon.instance != null) BGMCon.instance.StopMusic();
            if (bgmSource != null) bgmSource.Stop();
            UserAccountManager.Instance.RecordDeath();
            ShowRageMessage("No revives left. The rage wins this round.", 1.2f);
            StartCoroutine(LoadSceneAfter("GameOver", 1.25f));
            return;
        }

        respawning = true;
        revives--;
        UserAccountManager.Instance.RecordDeath();
        if (hud != null)
        {
            hud.SetRevives(revives);
        }

        SetPlayerMovementEnabled(false);
        ShowRageMessage(message + "\nCheckpoint rewind. Revive " + revives + " remains.", 1.8f);
        StartCoroutine(RespawnAfterFailure());
    }

    private IEnumerator RespawnAfterFailure()
    {
        yield return new WaitForSeconds(0.65f);
        if (player != null)
        {
            player.Respawn(checkpointPosition);
        }

        SetPlayerMovementEnabled(true);
        respawning = false;
    }

    public void CompleteLevel()
    {
        if (IsTransitioning)
        {
            return;
        }

        IsTransitioning = true;
        SetPlayerMovementEnabled(false);
        if (BGMCon.instance != null) BGMCon.instance.StopMusic();
        if (bgmSource != null) bgmSource.Stop();

        UserAccountManager.Instance.UnlockNextLevel(levelNumber);
        UserAccountManager.Instance.RecordWin();
        UserAccountManager.Instance.RecordScore(Score);

        ShowRageMessage("LEVEL COMPLETE.\nYou out-raged the bait.", 1.4f);
        StartCoroutine(LoadSceneAfter("Victory", 1.5f));
    }

    private IEnumerator LoadSceneAfter(string sceneName, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SceneManager.LoadScene(sceneName);
    }

    public void ApplyPlatformMotion(Vector3 delta)
    {
        if (player != null)
        {
            player.ApplyPlatformMotion(delta);
        }
    }

    private void SetPlayerMovementEnabled(bool enabled)
    {
        if (player != null)
        {
            player.SetMovementEnabled(enabled);
        }
    }

    public void CollectRelic(Collectible collectible)
    {
        if (collectible == null)
        {
            return;
        }

        RelicsFound++;
        AddScore(collectible.Points);
        if (hud != null)
        {
            hud.SetObjective("Relics Found: " + RelicsFound + "/5  •  " +
                             (RelicsFound >= 5 ? "Exit unlocked — walk to the arch" : "Keep exploring"));
        }

        ShowRageMessage(collectible.ItemName + " found. " + RelicsFound + "/5.", 1.35f);
        if (RelicsFound >= 5 && !level4ExitUnlocked)
        {
            level4ExitUnlocked = true;
            ShowRageMessage("All real relics found. The exit is finally being honest.", 2.1f);
        }
    }

    public void AcquireEscapeKey(string keyName, GameObject keyObject)
    {
        if (keyObject == null || !keyObject.activeSelf)
        {
            return;
        }

        EscapeKeysFound++;
        AddScore(3);
        keyObject.SetActive(false);
        if (hud != null)
        {
            hud.SetObjective("Components: " + EscapeKeysFound + "/3  •  " +
                             (EscapeKeysFound >= 3 ? "The real exit is discoverable" : "Search the room"));
        }

        ShowRageMessage(keyName + " acquired. " + EscapeKeysFound + "/3.", 1.35f);
        if (EscapeKeysFound >= 3)
        {
            ShowRageMessage("Three components secured. Try the obvious door. Carefully.", 2.1f);
        }
    }

    public void HandleInteraction(InteractableObject source)
    {
        if (source == null || IsTransitioning)
        {
            return;
        }

        switch (source.Action)
        {
            case InteractableAction.Collectible:
                CollectFrom(source);
                break;
            case InteractableAction.ApproachRelic:
            case InteractableAction.FakeCollectible:
                FakeFrom(source);
                break;
            case InteractableAction.MovingRelic:
            case InteractableAction.FinalRelic:
                RelocateRelic(source);
                break;
            case InteractableAction.ClueChest:
                ShowRageMessage("The clue says: 'Trust the red sparkle.'\nIt is not a good clue.", 2.4f);
                break;
            case InteractableAction.EscapeKey1:
                AcquireEscapeKey("Key 1", source.gameObject);
                break;
            case InteractableAction.EscapeKey2:
                HandleKey2(source);
                break;
            case InteractableAction.EscapeDrawer:
                OpenDrawer(source);
                break;
            case InteractableAction.EscapeFakeItem:
                FakeFrom(source);
                if (source.GetComponent<FakeCollectible>() == null)
                {
                    FailPlayer("RED FAKE ITEM! Decoy trap cost you a life.");
                }
                break;
            case InteractableAction.EscapeKey3:
                if (!level5LeverPulled)
                {
                    if (player != null)
                    {
                        player.AddImpulse(-player.transform.forward * 10f);
                    }
                    FailPlayer("HIGH VOLTAGE! Key 3 is electrified. Pull the blue lever first!");
                }
                else
                {
                    AcquireEscapeKey("Key 3", source.gameObject);
                }
                break;
            case InteractableAction.EscapeLever:
                PullLever(source);
                break;
            case InteractableAction.EscapeButton:
                FailPlayer("RED BUTTON TRAP! Pressing the danger button cost you a life.");
                break;
            case InteractableAction.ApparentDoor:
                HandleApparentDoor(source);
                break;
            case InteractableAction.RealDoor:
                if (EscapeKeysFound >= 3 && level5DoorTrollSeen)
                {
                    CompleteLevel();
                }
                else
                {
                    ShowRageMessage("That door is the real one, but the room has not finished trolling you.", 1.8f);
                }
                break;
        }
    }

    private void CollectFrom(InteractableObject source)
    {
        Collectible collectible = source.GetComponent<Collectible>();
        if (collectible != null)
        {
            collectible.Interact();
        }
    }

    private void FakeFrom(InteractableObject source)
    {
        FakeCollectible fake = source.GetComponent<FakeCollectible>();
        if (fake != null)
        {
            fake.Interact();
        }
    }

    private void RelocateRelic(InteractableObject source)
    {
        if (!source.ActionCompleted)
        {
            source.MarkActionCompleted();
            Vector3 newPos = GetRandomRelicDisplacementPosition(source.transform.position);
            source.transform.position = newPos;

            source.SetInteractionPrompt(source.MovedPrompt);
            ShowRageMessage("The relic displaced to a random clearing!\nYou won't catch the algorithm.", 2.0f);
            return;
        }

        CollectFrom(source);
    }

    private void HandleKey2(InteractableObject source)
    {
        if (level5Key2Displaced)
        {
            AcquireEscapeKey("Key 2", source.gameObject);
            return;
        }

        level5Key2Displaced = true;
        source.gameObject.SetActive(false);
        ShowRageMessage("Key 2 vanished into the drawer. That was intentional. Open it.", 2.1f);
    }

    private void OpenDrawer(InteractableObject source)
    {
        if (level5DrawerOpened)
        {
            ShowRageMessage("The drawer is already open. The room has run out of drawers.", 1.35f);
            return;
        }

        level5DrawerOpened = true;
        if (level5DrawerRenderer != null && level5DrawerOpenMaterial != null)
        {
            level5DrawerRenderer.sharedMaterial = level5DrawerOpenMaterial;
        }

        if (level5FakeItem != null)
        {
            level5FakeItem.SetActive(true);
        }

        if (level5Key2 != null)
        {
            if (level5Key2RevealPoint != null)
            {
                level5Key2.transform.position = level5Key2RevealPoint.position;
            }

            level5Key2.SetActive(true);
        }

        ShowRageMessage("The drawer opened. The red item is fake; the gold key is underneath.", 2.3f);
    }

    private void PullLever(InteractableObject source)
    {
        if (level5LeverPulled)
        {
            return;
        }

        level5LeverPulled = true;
        source.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        Renderer leverRenderer = source.GetComponent<Renderer>();
        if (leverRenderer != null && level5Key3UnlockedMaterial != null)
        {
            leverRenderer.sharedMaterial = level5Key3UnlockedMaterial;
        }

        if (level5Key3Interaction != null)
        {
            level5Key3Interaction.SetInteractionPrompt("Take Key 3");
        }

        if (level5Key3Renderer != null && level5Key3UnlockedMaterial != null)
        {
            level5Key3Renderer.sharedMaterial = level5Key3UnlockedMaterial;
        }

        ShowRageMessage("Lever pulled. Key 3 is no longer pretending to be inaccessible.", 1.9f);
    }

    private void HandleApparentDoor(InteractableObject source)
    {
        if (EscapeKeysFound < 3)
        {
            ShowRageMessage("The door is locked. It is also not the real door.", 1.75f);
            return;
        }

        if (level5DoorTrollSeen)
        {
            return;
        }

        level5DoorTrollSeen = true;
        if (level5ApparentDoorRenderer != null && level5ApparentDoorOpenMaterial != null)
        {
            level5ApparentDoorRenderer.sharedMaterial = level5ApparentDoorOpenMaterial;
        }

        if (level5RealDoor != null)
        {
            level5RealDoor.SetActive(true);
        }

        if (player != null)
        {
            player.AddImpulse(-player.transform.forward * 14f);
            player.AddVerticalImpulse(5f);
        }

        ShowRageMessage("CARDBOARD DOOR TROLL!\nThe fake door hit you back! The real door is behind you.", 2.5f);
    }

    public bool HandleFinish(LevelFinishTrigger finish)
    {
        if (finish == null)
        {
            return false;
        }

        switch (finish.Requirement)
        {
            case FinishRequirement.Always:
                CompleteLevel();
                return true;
            case FinishRequirement.Level2FinishTroll:
                if (level2FinishUnlocked)
                {
                    CompleteLevel();
                    return true;
                }

                if (!level2FinishTrollSeen)
                {
                    level2FinishTrollSeen = true;
                    finish.UnlockVisuals();
                    ShowRageMessage("FINISH LINE!\n...the sign was bait. Give it one second.", 1.8f);
                    StartCoroutine(UnlockFinishAfter(1.25f));
                }
                else
                {
                    ShowRageMessage("The finish is still pretending to load. One second.", 1.1f);
                }

                return true;
            case FinishRequirement.Level4Relics:
                if (level4ExitUnlocked)
                {
                    CompleteLevel();
                }
                else
                {
                    ShowRageMessage("The exit is locked. The fake sparkle does not count.", 1.7f);
                }

                return true;
            default:
                return false;
        }
    }

    private IEnumerator UnlockFinishAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        level2FinishUnlocked = true;
    }

    public void RegisterTarget(TargetController target)
    {
        if (target != null && !activeTargets.Contains(target) && target.gameObject.activeInHierarchy)
        {
            activeTargets.Add(target);
        }
    }

    public void UnregisterTarget(TargetController target)
    {
        activeTargets.Remove(target);
    }

    public void NotifyTargetShotFired()
    {
        for (int i = activeTargets.Count - 1; i >= 0; i--)
        {
            TargetController target = activeTargets[i];
            if (target == null)
            {
                activeTargets.RemoveAt(i);
            }
            else
            {
                target.NotifyPlayerFired();
            }
        }
    }

    public void TargetWasShot(TargetController target)
    {
        if (target == null)
        {
            return;
        }

        if (!target.IsValid)
        {
            AddScore(-2);
            FailPlayer("RED TARGET HIT! Shooting the fake target cost you a life.");
            return;
        }

        roundHits++;
        AddScore(target.Points);
        ShowRageMessage("HIT! +" + target.Points + "  •  The target regrets existing.", 0.95f);
        UpdateShootingObjective();

        if (targetRound == 5)
        {
            CompleteLevel();
        }
        else if (!advancingTargets && roundHits >= GetRoundGoal(targetRound))
        {
            advancingTargets = true;
            StartCoroutine(AdvanceShootingRound());
        }
    }

    public void OnAimMiss()
    {
        ShowRageMessage("Miss. The arena remains emotionally unaffected.", 0.9f);
    }

    private IEnumerator AdvanceShootingRound()
    {
        ShowRageMessage("Round cleared. The next targets learned from you.", 1.35f);
        yield return new WaitForSeconds(1.1f);
        targetRound++;
        roundHits = 0;
        advancingTargets = false;
        ActivateShootingRound(targetRound);
        UpdateShootingObjective();

        // In Level 6: at the start of each round when movement is enabled (Round 3 onwards),
        // place the player back to the start point so they have to walk all the way across the arena again!
        if (levelNumber == 6 && targetRound >= 3 && player != null)
        {
            Vector3 spawnPos = playerSpawn != null ? playerSpawn.position : checkpointPosition;
            float spawnYaw = playerSpawn != null ? playerSpawn.eulerAngles.y : 0f;
            player.Respawn(spawnPos, spawnYaw);
            player.ShuffleDirectionMappings();
        }

        if (targetRound == 3)
        {
            ShowRageMessage("MOVEMENT UNLOCKED!\n...and your controls are losing stability.", 2.5f);
        }
        else if (targetRound > 3)
        {
            ShowRageMessage("RESET TO START!\nWalk all the way across the arena again.", 2.2f);
        }
    }

    private int GetRoundGoal(int round)
    {
        switch (round)
        {
            case 1: return 3;
            case 2: return 3;
            case 3: return 2;
            case 4: return 3;
            default: return 1;
        }
    }

    private void UpdateShootingObjective()
    {
        if (hud != null)
        {
            string stance = targetRound < 3 ? "  •  Stationary Stance" : "  •  WASD Mobile";
            hud.SetObjective("Round " + targetRound + "/5  •  Hits: " + roundHits + "/" + GetRoundGoal(targetRound) + stance);
        }
    }

    private void ActivateShootingRound(int round)
    {
        activeTargets.Clear();
        if (sceneTargets == null)
        {
            return;
        }

        for (int i = 0; i < sceneTargets.Length; i++)
        {
            TargetController target = sceneTargets[i];
            if (target == null)
            {
                continue;
            }

            bool active = target.Round == round;
            target.gameObject.SetActive(active);
            target.PrepareForRound(active);
            if (active)
            {
                RegisterTarget(target);
            }
        }
    }

    private void SetupBgm()
    {
        // Find dedicated scene BGM GameObject (matching LV1) if not already assigned
        if (bgmSource == null)
        {
            GameObject bgmObj = GameObject.Find("BGM");
            if (bgmObj != null)
            {
                bgmSource = bgmObj.GetComponent<AudioSource>();
            }
        }

        // If the scene has its own dedicated BGM AudioSource (like LV1):
        if (bgmSource != null)
        {
            // Ensure persistent menu music stops so only the level's scene BGM plays
            if (BGMCon.instance != null)
            {
                BGMCon.instance.StopMusic();
            }

            if (levelBgm != null && bgmSource.clip == null)
            {
                bgmSource.clip = levelBgm;
            }

            bgmSource.volume = bgmVolume;
            bgmSource.loop = loopBgm;

            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
            return;
        }

        if (levelBgm != null)
        {
            if (BGMCon.instance != null)
            {
                BGMCon.instance.SetVolume(bgmVolume);
                BGMCon.instance.PlayClip(levelBgm, loopBgm);
            }
            else
            {
                AudioSource localSource = GetComponent<AudioSource>();
                if (localSource != null)
                {
                    localSource.clip = levelBgm;
                    localSource.volume = bgmVolume;
                    localSource.loop = loopBgm;
                    localSource.playOnAwake = false;
                    localSource.Play();
                }
            }
        }
        else if (BGMCon.instance != null)
        {
            BGMCon.instance.PlayLevelBgm(levelNumber);
        }
    }

    private void DisplaceRelicsAtStart()
    {
        List<Vector3> candidatePool = new List<Vector3>(RelicGroundPositions);
        for (int i = 0; i < candidatePool.Count; i++)
        {
            int rnd = Random.Range(i, candidatePool.Count);
            Vector3 temp = candidatePool[i];
            candidatePool[i] = candidatePool[rnd];
            candidatePool[rnd] = temp;
        }

        Collectible[] relics = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
        int poolIdx = 0;
        foreach (Collectible relic in relics)
        {
            if (relic == null) continue;
            if (poolIdx < candidatePool.Count)
            {
                Vector2 jitter = Random.insideUnitCircle * 1.5f;
                Vector3 basePos = candidatePool[poolIdx++];
                relic.transform.position = new Vector3(basePos.x + jitter.x, basePos.y, basePos.z + jitter.y);
            }
        }
    }

    public Vector3 GetRandomRelicDisplacementPosition(Vector3 currentPos)
    {
        Vector3 playerPos = ActivePlayerPosition;
        List<Vector3> valid = new List<Vector3>();
        for (int i = 0; i < RelicGroundPositions.Length; i++)
        {
            Vector3 candidate = RelicGroundPositions[i];
            if (Vector3.Distance(candidate, currentPos) > 10f && Vector3.Distance(candidate, playerPos) > 8f)
            {
                valid.Add(candidate);
            }
        }

        Vector3 chosen = valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : RelicGroundPositions[Random.Range(0, RelicGroundPositions.Length)];
        Vector2 jitter = Random.insideUnitCircle * 1.5f;
        return new Vector3(chosen.x + jitter.x, chosen.y, chosen.z + jitter.y);
    }

    private IEnumerator Level5LightFlickerRoutine()
    {
        List<Light> escapeRoomLights = new List<Light>();

        GameObject moonObj = GameObject.Find("Moonlight");
        if (moonObj != null)
        {
            Light ml = moonObj.GetComponent<Light>();
            if (ml != null)
            {
                escapeRoomLights.Add(ml);
            }
        }

        if (escapeRoomLights.Count == 0)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l != null && l.type == LightType.Directional)
                {
                    escapeRoomLights.Add(l);
                    break;
                }
            }
        }

        // Create dedicated interior ceiling point lights covering all rooms and corridors
        Vector3[] interiorLocations = new Vector3[]
        {
            new Vector3(0f, 3.8f, -22f),   // Spawn corridor
            new Vector3(-4f, 3.8f, -14f),  // Entrance / Key 1 Room
            new Vector3(4f, 3.8f, -14f),   // Entrance right wing
            new Vector3(0f, 3.8f, -4f),    // Mid-hallway
            new Vector3(5f, 3.8f, 4f),     // Button room
            new Vector3(-5f, 3.8f, 12f),   // Section 3 / Key 3 Area
            new Vector3(0f, 3.8f, 20f),    // Pre-vault hall
            new Vector3(0f, 3.8f, 28f),    // Vault & Apparent Exit room
            new Vector3(0f, 3.8f, -31f)    // Real Exit Door room
        };

        for (int i = 0; i < interiorLocations.Length; i++)
        {
            string lightName = $"EscapeRoom_CeilingLight_{i}";
            GameObject lObj = GameObject.Find(lightName);
            if (lObj == null)
            {
                lObj = new GameObject(lightName);
                Light ptLight = lObj.AddComponent<Light>();
                ptLight.type = LightType.Point;
                ptLight.range = 24f;
                ptLight.intensity = 2.2f;
                ptLight.color = new Color(0.88f, 0.94f, 1.0f); // crisp fluorescent interior light
                ptLight.shadows = LightShadows.None;
                lObj.transform.position = interiorLocations[i];
            }
            Light pComp = lObj.GetComponent<Light>();
            if (pComp != null && !escapeRoomLights.Contains(pComp))
            {
                escapeRoomLights.Add(pComp);
            }
        }

        Color originalAmbient = RenderSettings.ambientLight;
        UnityEngine.Rendering.AmbientMode originalAmbientMode = RenderSettings.ambientMode;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        Color offAmbient = new Color(0.012f, 0.015f, 0.02f);
        Color onAmbient = new Color(0.24f, 0.28f, 0.35f);

        void ApplyLightState(bool on, float intensityMul = 1.0f)
        {
            foreach (Light l in escapeRoomLights)
            {
                if (l != null)
                {
                    l.enabled = on;
                    float baseVal = (l.type == LightType.Directional) ? 1.0f : 2.2f;
                    l.intensity = baseVal * intensityMul;
                }
            }
            RenderSettings.ambientLight = on ? Color.Lerp(offAmbient, onAmbient, intensityMul) : offAmbient;
        }

        // INITIALLY OFF: Start the escape room in darkness
        ApplyLightState(false);

        while (levelNumber == 5)
        {
            // OFF PHASE: Darkness is the default state
            ApplyLightState(false);
            float offDuration = Random.Range(2.5f, 4.0f);
            yield return new WaitForSeconds(offDuration);

            // Pre-flicker electrical spark
            ApplyLightState(true, 0.35f);
            yield return new WaitForSeconds(0.06f);
            ApplyLightState(false);
            yield return new WaitForSeconds(0.07f);
            ApplyLightState(true, 0.6f);
            yield return new WaitForSeconds(0.05f);
            ApplyLightState(false);
            yield return new WaitForSeconds(0.06f);

            // MOMENTARILY ON: bright interior illumination so the player can clearly explore
            ApplyLightState(true, 1.0f);
            float onDuration = Random.Range(1.8f, 2.8f);
            yield return new WaitForSeconds(onDuration);

            // Micro-flicker before cutting off
            ApplyLightState(true, 0.25f);
            yield return new WaitForSeconds(0.07f);

            // Back to dark
            ApplyLightState(false);
        }

        // Restore if level ended
        ApplyLightState(true, 1.0f);
        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.ambientLight = originalAmbient;
    }

    private void ApplySignFontSizeConsistency()
    {
        TMPro.TMP_Text[] allTexts = FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None);
        foreach (TMPro.TMP_Text t in allTexts)
        {
            if (t == null) continue;
            // Only update 3D world in-scene signs, do not touch UI Canvas
            if (t.GetComponentInParent<Canvas>() == null && t.gameObject.name.Contains("Sign"))
            {
                t.fontSize = 0.72f;
            }
        }
    }

    private void OnDestroy()
    {
        if (level5LightRoutine != null)
        {
            StopCoroutine(level5LightRoutine);
            level5LightRoutine = null;
        }

        if (Active == this)
        {
            Active = null;
        }

        if (player != null)
        {
            player.LockCursor(false);
        }
    }
}
