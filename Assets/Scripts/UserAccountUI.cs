using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI overlay for managing user accounts and showing current progress in Home and Levels screens.
/// Self-assembles cleanly if not pre-authored into the scene canvas.
/// Allows user to create their own accounts; does not generate sample accounts.
/// </summary>
public class UserAccountUI : MonoBehaviour
{
    private static UserAccountUI instance;

    private Canvas canvas;
    private GameObject profileBar;
    private TextMeshProUGUI statusText;
    private Button accountButton;
    private TextMeshProUGUI accountButtonText;

    private GameObject modalPanel;
    private TMP_InputField nameInput;
    private Transform accountListContainer;
    private TextMeshProUGUI feedbackText;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        UserAccountManager.Instance.ReloadData();
        EnsureUI();
        UpdateDisplay();

        UserAccountManager.Instance.OnAccountChanged += OnAccountOrProgressChanged;
        UserAccountManager.Instance.OnProgressChanged += OnAccountOrProgressChanged;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (UserAccountManager.Instance != null)
        {
            UserAccountManager.Instance.OnAccountChanged -= OnAccountOrProgressChanged;
            UserAccountManager.Instance.OnProgressChanged -= OnAccountOrProgressChanged;
        }
    }

    private void OnAccountOrProgressChanged()
    {
        UpdateDisplay();
        if (modalPanel != null && modalPanel.activeSelf)
        {
            RefreshAccountList();
        }
    }

    private void EnsureUI()
    {
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Create top profile bar
        profileBar = new GameObject("UserProfileBar", typeof(RectTransform), typeof(Image));
        profileBar.transform.SetParent(canvas.transform, false);

        RectTransform barRect = profileBar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -6f);
        barRect.sizeDelta = new Vector2(-40f, 26f);

        Image barBg = profileBar.GetComponent<Image>();
        barBg.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);

        // Account status text
        GameObject textObj = new GameObject("AccountStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(profileBar.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(12f, 0f);
        textRect.offsetMax = new Vector2(-175f, 0f);

        statusText = textObj.GetComponent<TextMeshProUGUI>();
        statusText.fontSize = 12.5f;
        statusText.alignment = TextAlignmentOptions.MidlineLeft;
        statusText.color = new Color(0.9f, 0.92f, 0.95f, 1f);
        RageRuntimeUtility.ApplyCinzel(statusText);

        // Switch / Profile Button
        GameObject btnObj = new GameObject("SwitchAccountBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(profileBar.transform, false);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 0.5f);
        btnRect.anchorMax = new Vector2(1f, 0.5f);
        btnRect.pivot = new Vector2(1f, 0.5f);
        btnRect.anchoredPosition = new Vector2(-6f, 0f);
        btnRect.sizeDelta = new Vector2(165f, 20f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.24f, 0.42f, 0.65f, 0.95f);

        accountButton = btnObj.GetComponent<Button>();
        accountButton.onClick.AddListener(OpenModal);

        GameObject btnTextObj = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        accountButtonText = btnTextObj.GetComponent<TextMeshProUGUI>();
        accountButtonText.text = "PROFILES / ACCOUNTS";
        accountButtonText.fontSize = 11f;
        accountButtonText.alignment = TextAlignmentOptions.Center;
        accountButtonText.color = Color.white;
        RageRuntimeUtility.ApplyCinzel(accountButtonText);

        BuildModal();
    }

    private void BuildModal()
    {
        modalPanel = new GameObject("AccountModalOverlay", typeof(RectTransform), typeof(Image));
        modalPanel.transform.SetParent(canvas.transform, false);
        modalPanel.SetActive(false);

        RectTransform overlayRect = modalPanel.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayBg = modalPanel.GetComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.82f);

        // Card container
        GameObject card = new GameObject("ModalCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(modalPanel.transform, false);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(520f, 440f);

        Image cardBg = card.GetComponent<Image>();
        cardBg.color = new Color(0.11f, 0.12f, 0.18f, 0.98f);

        // Title
        GameObject titleObj = new GameObject("ModalTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(card.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -16f);
        titleRect.sizeDelta = new Vector2(-40f, 36f);

        TextMeshProUGUI title = titleObj.GetComponent<TextMeshProUGUI>();
        title.text = "PLAYER PROFILES & PROGRESS";
        title.fontSize = 20f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.95f, 0.85f, 0.35f, 1f);
        RageRuntimeUtility.ApplyCinzel(title);

        // Close Button
        GameObject closeBtn = CreateSimpleButton(card.transform, "X", new Vector2(460f, -14f), new Vector2(36f, 32f), new Color(0.6f, 0.2f, 0.2f));
        closeBtn.GetComponent<Button>().onClick.AddListener(CloseModal);

        // Input Field for New Account
        GameObject inputObj = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputObj.transform.SetParent(card.transform, false);
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.06f, 0.76f);
        inputRect.anchorMax = new Vector2(0.65f, 0.86f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        Image inputBg = inputObj.GetComponent<Image>();
        inputBg.color = new Color(0.18f, 0.2f, 0.28f, 1f);

        nameInput = inputObj.GetComponent<TMP_InputField>();

        GameObject inputTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        inputTextObj.transform.SetParent(inputObj.transform, false);
        RectTransform textR = inputTextObj.GetComponent<RectTransform>();
        textR.anchorMin = Vector2.zero;
        textR.anchorMax = Vector2.one;
        textR.offsetMin = new Vector2(10f, 0f);
        textR.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI inText = inputTextObj.GetComponent<TextMeshProUGUI>();
        inText.fontSize = 16f;
        inText.alignment = TextAlignmentOptions.MidlineLeft;
        inText.color = Color.white;
        nameInput.textComponent = inText;

        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderObj.transform.SetParent(inputObj.transform, false);
        RectTransform placeR = placeholderObj.GetComponent<RectTransform>();
        placeR.anchorMin = Vector2.zero;
        placeR.anchorMax = Vector2.one;
        placeR.offsetMin = new Vector2(10f, 0f);
        placeR.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI placeText = placeholderObj.GetComponent<TextMeshProUGUI>();
        placeText.text = "Enter your profile name...";
        placeText.fontSize = 15f;
        placeText.alignment = TextAlignmentOptions.MidlineLeft;
        placeText.color = new Color(0.6f, 0.6f, 0.65f, 0.7f);
        nameInput.placeholder = placeText;

        // "Create" button
        GameObject createBtn = CreateSimpleButton(card.transform, "+ CREATE", new Vector2(395f, -80f), new Vector2(110f, 40f), new Color(0.2f, 0.55f, 0.35f));
        createBtn.GetComponent<Button>().onClick.AddListener(OnCreateAccountClicked);

        // Feedback Text
        GameObject fbObj = new GameObject("FeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
        fbObj.transform.SetParent(card.transform, false);
        RectTransform fbRect = fbObj.GetComponent<RectTransform>();
        fbRect.anchorMin = new Vector2(0.06f, 0.69f);
        fbRect.anchorMax = new Vector2(0.94f, 0.75f);
        fbRect.offsetMin = Vector2.zero;
        fbRect.offsetMax = Vector2.zero;

        feedbackText = fbObj.GetComponent<TextMeshProUGUI>();
        feedbackText.fontSize = 13f;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.color = new Color(0.85f, 0.85f, 0.9f, 1f);

        // Account List Scroll View
        GameObject listObj = new GameObject("AccountList", typeof(RectTransform), typeof(Image));
        listObj.transform.SetParent(card.transform, false);
        RectTransform listRect = listObj.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.06f, 0.22f);
        listRect.anchorMax = new Vector2(0.94f, 0.68f);
        listRect.offsetMin = Vector2.zero;
        listRect.offsetMax = Vector2.zero;

        Image listBg = listObj.GetComponent<Image>();
        listBg.color = new Color(0.07f, 0.08f, 0.12f, 0.9f);
        accountListContainer = listObj.transform;

        VerticalLayoutGroup vlg = listObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Reset Progress Button and Delete Active Profile Button at bottom
        GameObject resetBtn = CreateSimpleButton(card.transform, "RESET PROGRESS", new Vector2(145f, -395f), new Vector2(195f, 36f), new Color(0.55f, 0.35f, 0.18f));
        resetBtn.GetComponent<Button>().onClick.AddListener(OnResetProgressClicked);

        GameObject deleteBtn = CreateSimpleButton(card.transform, "DELETE ACTIVE PROFILE", new Vector2(365f, -395f), new Vector2(215f, 36f), new Color(0.72f, 0.18f, 0.18f));
        deleteBtn.GetComponent<Button>().onClick.AddListener(OnDeleteActiveAccountClicked);
    }

    private GameObject CreateSimpleButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
    {
        GameObject btnObj = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform r = btnObj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = color;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 14f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        RageRuntimeUtility.ApplyCinzel(txt);

        return btnObj;
    }

    public void OpenModal()
    {
        if (modalPanel != null)
        {
            modalPanel.SetActive(true);
            if (!UserAccountManager.Instance.HasAccount && feedbackText != null)
            {
                feedbackText.text = "<color=#ffe066>Enter your profile name above and click '+ CREATE' to begin!</color>";
            }
            RefreshAccountList();
        }
    }

    public void CloseModal()
    {
        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }
    }

    private void OnCreateAccountClicked()
    {
        if (nameInput == null || string.IsNullOrWhiteSpace(nameInput.text))
        {
            if (feedbackText != null)
            {
                feedbackText.text = "<color=#ff6666>Please enter a valid profile name!</color>";
            }
            return;
        }

        string cleanName = nameInput.text.Trim();
        UserAccountManager.Instance.CreateAccount(cleanName);
        nameInput.text = "";
        if (feedbackText != null)
        {
            feedbackText.text = $"<color=#66ff88>Profile '{cleanName}' created and activated! Ready to play.</color>";
        }
        UpdateDisplay();
        RefreshAccountList();
        StartCoroutine(AutoCloseModal(1.0f));
    }

    private System.Collections.IEnumerator AutoCloseModal(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseModal();
    }

    private void OnResetProgressClicked()
    {
        if (!UserAccountManager.Instance.HasAccount)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "<color=#ff6666>No active profile to reset!</color>";
            }
            return;
        }

        UserAccountManager.Instance.ResetActiveAccountProgress();
        if (feedbackText != null)
        {
            feedbackText.text = "<color=#ffcc44>Current profile progress reset to Level 1.</color>";
        }
        UpdateDisplay();
        RefreshAccountList();
    }

    private void OnDeleteActiveAccountClicked()
    {
        if (!UserAccountManager.Instance.HasAccount)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "<color=#ff6666>No active profile to delete!</color>";
            }
            return;
        }

        string userToDelete = UserAccountManager.Instance.ActiveAccount.username;
        OnDeleteAccountClicked(userToDelete);
    }

    private void OnDeleteAccountClicked(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return;

        bool success = UserAccountManager.Instance.DeleteAccount(username);
        if (success)
        {
            if (feedbackText != null)
            {
                feedbackText.text = $"<color=#ff6666>Profile '{username}' deleted successfully.</color>";
            }
        }
        else
        {
            if (feedbackText != null)
            {
                feedbackText.text = $"<color=#ff6666>Failed to delete profile '{username}'.</color>";
            }
        }
        UpdateDisplay();
        RefreshAccountList();
    }

    private void RefreshAccountList()
    {
        if (accountListContainer == null) return;

        for (int i = accountListContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(accountListContainer.GetChild(i).gameObject);
        }

        List<UserAccount> accounts = UserAccountManager.Instance.GetAllAccounts();
        UserAccount active = UserAccountManager.Instance.ActiveAccount;

        if (accounts.Count == 0)
        {
            GameObject emptyObj = new GameObject("EmptyLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyObj.transform.SetParent(accountListContainer, false);
            LayoutElement le = emptyObj.AddComponent<LayoutElement>();
            le.minHeight = 44f;
            le.preferredHeight = 44f;

            TextMeshProUGUI emptyTxt = emptyObj.GetComponent<TextMeshProUGUI>();
            emptyTxt.text = "No profiles created yet.\nEnter your name above and click '+ CREATE' to start!";
            emptyTxt.fontSize = 14f;
            emptyTxt.alignment = TextAlignmentOptions.Center;
            emptyTxt.color = new Color(0.7f, 0.75f, 0.8f, 0.8f);
            RageRuntimeUtility.ApplyCinzel(emptyTxt);
            return;
        }

        foreach (UserAccount acc in accounts)
        {
            bool isActive = active != null && acc.username == active.username;

            GameObject row = new GameObject("Row_" + acc.username, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(accountListContainer, false);

            LayoutElement le = row.AddComponent<LayoutElement>();
            le.minHeight = 36f;
            le.preferredHeight = 36f;

            Image rowBg = row.GetComponent<Image>();
            rowBg.color = isActive ? new Color(0.2f, 0.45f, 0.3f, 0.95f) : new Color(0.18f, 0.2f, 0.28f, 0.85f);

            // Left side click area to switch account
            GameObject switchBtnObj = new GameObject("SwitchBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            switchBtnObj.transform.SetParent(row.transform, false);
            RectTransform switchRect = switchBtnObj.GetComponent<RectTransform>();
            switchRect.anchorMin = Vector2.zero;
            switchRect.anchorMax = new Vector2(0.85f, 1f);
            switchRect.offsetMin = Vector2.zero;
            switchRect.offsetMax = Vector2.zero;

            Image switchImg = switchBtnObj.GetComponent<Image>();
            switchImg.color = Color.clear;

            string targetUser = acc.username;
            switchBtnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                UserAccountManager.Instance.SwitchAccount(targetUser);
                if (feedbackText != null)
                {
                    feedbackText.text = $"<color=#66ff88>Switched to '{targetUser}'!</color>";
                }
                UpdateDisplay();
                RefreshAccountList();
            });

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(switchBtnObj.transform, false);
            RectTransform tr = txtObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(10f, 0f);
            tr.offsetMax = new Vector2(-6f, 0f);

            TextMeshProUGUI t = txtObj.GetComponent<TextMeshProUGUI>();
            int completedCount = acc.completedAllLevels ? 6 : Mathf.Clamp(acc.maxUnlockedLevel - 1, 0, 6);
            string tag = isActive ? "<b>[ACTIVE]</b> " : "";
            string champ = acc.completedAllLevels ? " <color=#ffe066>[CHAMPION]</color>" : "";
            t.text = $"{tag}{acc.username}{champ}  •  Completed: {completedCount}/6  •  Wins: {acc.totalWins}";
            t.fontSize = 13f;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.color = isActive ? Color.white : new Color(0.85f, 0.85f, 0.9f, 0.9f);
            RageRuntimeUtility.ApplyCinzel(t);

            // Right side button to delete this specific account
            GameObject delBtnObj = new GameObject("DelBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            delBtnObj.transform.SetParent(row.transform, false);
            RectTransform delRect = delBtnObj.GetComponent<RectTransform>();
            delRect.anchorMin = new Vector2(0.86f, 0.12f);
            delRect.anchorMax = new Vector2(0.985f, 0.88f);
            delRect.offsetMin = Vector2.zero;
            delRect.offsetMax = Vector2.zero;

            Image delImg = delBtnObj.GetComponent<Image>();
            delImg.color = new Color(0.72f, 0.2f, 0.2f, 0.95f);

            Button delBtn = delBtnObj.GetComponent<Button>();
            delBtn.onClick.AddListener(() =>
            {
                OnDeleteAccountClicked(targetUser);
            });

            GameObject delTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            delTxtObj.transform.SetParent(delBtnObj.transform, false);
            RectTransform delTr = delTxtObj.GetComponent<RectTransform>();
            delTr.anchorMin = Vector2.zero;
            delTr.anchorMax = Vector2.one;
            delTr.offsetMin = Vector2.zero;
            delTr.offsetMax = Vector2.zero;

            TextMeshProUGUI delTxt = delTxtObj.GetComponent<TextMeshProUGUI>();
            delTxt.text = "DEL";
            delTxt.fontSize = 11.5f;
            delTxt.alignment = TextAlignmentOptions.Center;
            delTxt.color = Color.white;
            RageRuntimeUtility.ApplyCinzel(delTxt);
        }
    }

    public void UpdateDisplay()
    {
        if (statusText == null) return;

        UserAccount current = UserAccountManager.Instance.ActiveAccount;
        if (current != null)
        {
            int completedCount = current.completedAllLevels ? 6 : Mathf.Clamp(current.maxUnlockedLevel - 1, 0, 6);
            string compStr = current.completedAllLevels
                ? "<color=#ffe066><b>6 / 6 (ALL LEVELS)</b></color>"
                : $"<color=#66ff88><b>{completedCount} / 6</b></color>";

            statusText.text = $"PROFILE: <color=#ffe066><b>{current.username.ToUpper()}</b></color>   |   COMPLETED: {compStr}";
            if (accountButtonText != null)
            {
                accountButtonText.text = "PROFILES / ACCOUNTS";
            }
        }
        else
        {
            statusText.text = "PROFILE: <color=#ff6666><b>NONE (CREATE PROFILE TO PLAY)</b></color>";
            if (accountButtonText != null)
            {
                accountButtonText.text = "+ CREATE PROFILE";
            }
        }
    }
}
