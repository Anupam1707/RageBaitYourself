using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Rules : MonoBehaviour
{
    private static bool homeContentLoaded;
    private static bool lastCompletionState;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI rulesText;
    public Button playButton;
    public string title = "Ready to RageBait Yourself ?";
    public float typewriterSpeed = 0.08f;
    public float rulesFontSize = 17f;

    [Header("Default Rules")]
    [TextArea(2, 5)]
    public string[] warnings =
    {
        "This whole game is designed to test your patience.",
        "All coins must be collected to unlock the exit and win.",
        "Find treasure for revives, and stay alert: 'green never means go'.",
        "Expect traps, surprises, and plenty of chances to rage.",
        "There is no safe level. Good luck making it to the end."
    };

    [Header("Completed All Levels Replacement")]
    public string completedTitle = "You Conquered RageBait Yourself !";
    [TextArea(2, 5)]
    public string[] completedWarnings =
    {
        "Against all odds, your patience and sanity survived.",
        "Every cruel trap, fake exit, and lying sign was overcome.",
        "You proved that 'green never means go' could not break you.",
        "All 6 treacherous trials have been fully mastered.",
        "You are officially certified un-ragebaitable. Respect."
    };

    private Coroutine typewriterRoutine;

    public bool IsCompleted => UserAccountManager.Instance != null && UserAccountManager.Instance.HasCompletedAllLevels();
    public string CurrentTitle => IsCompleted ? completedTitle : title;
    public string[] CurrentWarnings => IsCompleted ? completedWarnings : warnings;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        homeContentLoaded = false;
        lastCompletionState = false;
    }

    private void Awake()
    {
        if (UserAccountManager.Instance != null)
        {
            UserAccountManager.Instance.OnAccountChanged += HandleAccountOrProgressChanged;
            UserAccountManager.Instance.OnProgressChanged += HandleAccountOrProgressChanged;
        }
    }

    private void OnDestroy()
    {
        if (UserAccountManager.Instance != null)
        {
            UserAccountManager.Instance.OnAccountChanged -= HandleAccountOrProgressChanged;
            UserAccountManager.Instance.OnProgressChanged -= HandleAccountOrProgressChanged;
        }
    }

    private void HandleAccountOrProgressChanged()
    {
        bool completed = IsCompleted;
        if (completed != lastCompletionState || homeContentLoaded)
        {
            lastCompletionState = completed;
            if (typewriterRoutine != null)
            {
                StopCoroutine(typewriterRoutine);
                typewriterRoutine = null;
            }
            DisplayContentImmediately();
        }
    }

    void Start()
    {
        if (rulesText == null)
        {
            Debug.LogError("Rules requires a TextMeshProUGUI reference.");
            return;
        }

        RageRuntimeUtility.ApplyCinzel(titleText);
        RageRuntimeUtility.ApplyCinzel(rulesText);
        if (rulesFontSize > 0f)
        {
            rulesText.fontSize = rulesFontSize;
        }

        // Play is available immediately; only the Home copy is revealed over time.
        if (playButton != null)
        {
            playButton.gameObject.SetActive(true);
        }

        bool completed = IsCompleted;
        if (homeContentLoaded && completed == lastCompletionState)
        {
            DisplayContentImmediately();
            return;
        }

        lastCompletionState = completed;
        rulesText.text = "";

        typewriterRoutine = StartCoroutine(DisplayRules());
    }

    private IEnumerator DisplayRules()
    {
        string activeTitle = CurrentTitle;
        string[] activeWarnings = CurrentWarnings;

        if (titleText != null)
        {
            titleText.text = "";

            foreach (char character in activeTitle)
            {
                titleText.text += character;
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }

        foreach (string warning in activeWarnings)
        {
            yield return new WaitForSeconds(1f);
            rulesText.text += (rulesText.text.Length > 0 ? "\n" : "") + warning;
        }

        if (playButton != null)
        {
            playButton.gameObject.SetActive(true);
        }

        homeContentLoaded = true;
    }

    private void DisplayContentImmediately()
    {
        if (titleText != null)
        {
            titleText.text = CurrentTitle;
        }

        rulesText.text = string.Join("\n", CurrentWarnings);
        if (playButton != null)
        {
            playButton.gameObject.SetActive(true);
        }

        homeContentLoaded = true;
    }
}
