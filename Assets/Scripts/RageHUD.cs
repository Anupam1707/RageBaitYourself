using TMPro;
using UnityEngine;

/// <summary>
/// References the HUD authored in each level scene. The component only updates
/// existing UI; it never creates Canvas or text objects at runtime.
/// </summary>
public class RageHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI reviveText;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI crosshairText;
    [SerializeField] private RageMessageController messages;

    public TextMeshProUGUI TitleText => titleText;
    public TextMeshProUGUI ObjectiveText => objectiveText;
    public TextMeshProUGUI ScoreText => scoreText;
    public TextMeshProUGUI ReviveText => reviveText;
    public TextMeshProUGUI PromptText => promptText;
    public TextMeshProUGUI CrosshairText => crosshairText;
    public RageMessageController Messages
    {
        get
        {
            EnsureReferences();
            return messages;
        }
    }

    private void Awake()
    {
        EnsureReferences();
        SetRaycastTargets(false);
        EnsureTextDisplay(reviveText);
        EnsureTextDisplay(scoreText);
    }

    private void Start()
    {
        EnsureReferences();
        EnsureTextDisplay(reviveText);
        EnsureTextDisplay(scoreText);
    }

    private void EnsureReferences()
    {
        if (messages == null)
        {
            messages = GetComponent<RageMessageController>();
        }

        if (titleText == null) titleText = FindChildText("title");
        if (objectiveText == null) objectiveText = FindChildText("objective");
        if (scoreText == null) scoreText = FindChildText("score");
        if (reviveText == null) reviveText = FindChildText("revive");
        if (promptText == null) promptText = FindChildText("prompt");
        if (crosshairText == null) crosshairText = FindChildText("crosshair");
    }

    private TextMeshProUGUI FindChildText(string keyword)
    {
        foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text != null && text.gameObject.name.ToLower().Contains(keyword))
            {
                return text;
            }
        }
        return null;
    }

    private void EnsureTextDisplay(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        if (!text.gameObject.activeSelf)
        {
            text.gameObject.SetActive(true);
        }

        text.overflowMode = TextOverflowModes.Overflow;
        text.enableWordWrapping = false;
        RageRuntimeUtility.ApplyCinzel(text);

        RectTransform rt = text.rectTransform;
        if (rt != null && rt.sizeDelta.y < 44f)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 44f);
        }
    }

    private void SetRaycastTargets(bool value)
    {
        SetRaycastTarget(titleText, value);
        SetRaycastTarget(objectiveText, value);
        SetRaycastTarget(scoreText, value);
        SetRaycastTarget(reviveText, value);
        SetRaycastTarget(promptText, value);
        SetRaycastTarget(crosshairText, value);
    }

    private static void SetRaycastTarget(TextMeshProUGUI text, bool value)
    {
        if (text != null)
        {
            text.raycastTarget = value;
        }
    }

    public void Configure(
        TextMeshProUGUI title,
        TextMeshProUGUI objective,
        TextMeshProUGUI score,
        TextMeshProUGUI revives,
        TextMeshProUGUI prompt,
        TextMeshProUGUI crosshair,
        RageMessageController messageController)
    {
        titleText = title;
        objectiveText = objective;
        scoreText = score;
        reviveText = revives;
        promptText = prompt;
        crosshairText = crosshair;
        messages = messageController;
        SetRaycastTargets(false);
        EnsureTextDisplay(reviveText);
        EnsureTextDisplay(scoreText);
    }

    public void SetTitle(string value)
    {
        if (titleText != null)
        {
            titleText.text = value;
        }
    }

    public void SetObjective(string value)
    {
        if (objectiveText != null)
        {
            objectiveText.text = value;
        }
    }

    public void SetScore(int score)
    {
        EnsureReferences();
        if (scoreText != null)
        {
            EnsureTextDisplay(scoreText);
            scoreText.text = "Score: " + score;
        }
    }

    public void SetRevives(int revives)
    {
        EnsureReferences();
        if (reviveText != null)
        {
            EnsureTextDisplay(reviveText);
            reviveText.text = "Revives: " + Mathf.Max(0, revives);
        }
    }

    public void SetPrompt(string value)
    {
        if (promptText != null)
        {
            promptText.text = value;
        }
    }

    public void ShowMessage(string message, float seconds = 2.2f)
    {
        EnsureReferences();
        if (messages != null)
        {
            messages.Show(message, seconds);
        }
    }

    public RageMessageController GetMessageController()
    {
        EnsureReferences();
        return messages;
    }
}
