using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Timed message behavior for a serialized HUD panel and text element.
/// </summary>
public class RageMessageController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image panel;
    private Coroutine clearRoutine;

    public TextMeshProUGUI MessageText => messageText;
    public Image Panel => panel;

    private void Awake()
    {
        SetVisible(false);
    }

    public void Configure(TextMeshProUGUI target, Image messagePanel)
    {
        messageText = target;
        panel = messagePanel;
        SetVisible(false);
    }

    public void Show(string message, float seconds)
    {
        if (messageText == null)
        {
            return;
        }

        if (clearRoutine != null)
        {
            StopCoroutine(clearRoutine);
        }

        SetVisible(true);
        messageText.text = message;
        clearRoutine = StartCoroutine(ClearAfter(seconds));
    }

    private IEnumerator ClearAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (messageText != null)
        {
            messageText.text = string.Empty;
        }

        SetVisible(false);
        clearRoutine = null;
    }

    private void SetVisible(bool visible)
    {
        if (messageText != null && !visible)
        {
            messageText.text = string.Empty;
        }

        if (panel != null)
        {
            panel.gameObject.SetActive(visible);
        }
    }
}
