using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    private TextMeshProUGUI lockNoticeText;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // Ensure user account manager and UI exist
        _ = UserAccountManager.Instance;
        EnsureUserAccountUI();

        if (sceneName == "Levels")
        {
            ResumeMusic();
            RefreshLevelLockVisuals();
            UserAccountManager.Instance.OnAccountChanged += RefreshLevelLockVisuals;
            UserAccountManager.Instance.OnProgressChanged += RefreshLevelLockVisuals;
        }
    }

    void OnDestroy()
    {
        if (UserAccountManager.Instance != null)
        {
            UserAccountManager.Instance.OnAccountChanged -= RefreshLevelLockVisuals;
            UserAccountManager.Instance.OnProgressChanged -= RefreshLevelLockVisuals;
        }
    }

    private void EnsureUserAccountUI()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "Home" || scene == "Levels")
        {
            if (FindFirstObjectByType<UserAccountUI>() == null)
            {
                gameObject.AddComponent<UserAccountUI>();
            }
        }
    }

    public void RefreshLevelLockVisuals()
    {
        if (SceneManager.GetActiveScene().name != "Levels") return;

        for (int i = 1; i <= 6; i++)
        {
            string btnName = "Level" + i;
            GameObject btnObj = GameObject.Find(btnName);
            if (btnObj == null) continue;

            Button btn = btnObj.GetComponent<Button>();
            Image img = btnObj.GetComponent<Image>();
            bool isUnlocked = UserAccountManager.Instance.IsLevelUnlocked(i);

            if (btn != null)
            {
                btn.interactable = isUnlocked;
            }

            if (img != null)
            {
                img.color = isUnlocked ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.45f);
            }

            // Look for existing lock badge or create one
            Transform lockBadge = btnObj.transform.Find("LockBadge");
            if (!isUnlocked)
            {
                if (lockBadge == null)
                {
                    GameObject badgeObj = new GameObject("LockBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
                    badgeObj.transform.SetParent(btnObj.transform, false);
                    RectTransform rt = badgeObj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    TextMeshProUGUI tmp = badgeObj.GetComponent<TextMeshProUGUI>();
                    tmp.text = "LOCKED";
                    tmp.fontSize = 13f;
                    tmp.alignment = TextAlignmentOptions.Bottom;
                    tmp.color = new Color(1f, 0.3f, 0.3f, 0.95f);
                    RageRuntimeUtility.ApplyCinzel(tmp);
                }
                else
                {
                    lockBadge.gameObject.SetActive(true);
                }
            }
            else
            {
                if (lockBadge != null)
                {
                    lockBadge.gameObject.SetActive(false);
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            QuitGame();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Back();
            return;
        }

        if (SceneManager.GetActiveScene().name != "Levels")
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            LaunchLV1();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            LaunchLV2();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            LaunchLV3();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            LaunchLV4();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            LaunchLV5();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
        {
            LaunchLV6();
        }
    }

    void Back()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "Levels":
            case "About":
                Home();
                break;
            case "GameOver":
            case "Victory":
                ShowLevels();
                break;
        }
    }

    public void Home()
    {
        ResumeMusic();
        SceneManager.LoadScene("Home");
    }

    public void ShowLevels()
    {
        if (!UserAccountManager.Instance.HasAccount)
        {
            UserAccountUI ui = FindFirstObjectByType<UserAccountUI>();
            if (ui != null)
            {
                ui.OpenModal();
                return;
            }
        }

        ResumeMusic();
        SceneManager.LoadScene("Levels");
    }

    public void ShowAbout()
    {
        ResumeMusic();
        SceneManager.LoadScene("About");
    }

    public static void ShowLevelsFromGame()
    {
        ResumeMusic();
        SceneManager.LoadScene("Levels");
    }

    static void ResumeMusic()
    {
        BGMCon musicController = BGMCon.instance;
        if (musicController == null)
        {
            musicController = FindFirstObjectByType<BGMCon>();
            BGMCon.instance = musicController;
        }

        if (musicController != null)
        {
            musicController.PlayMusic();
        }
    }

    private bool CheckLevelAccess(int level)
    {
        if (!UserAccountManager.Instance.HasAccount)
        {
            ShowNotice("<color=#ff4444>NO PROFILE CREATED!</color>\nPlease enter your profile name above to begin playing.");
            UserAccountUI ui = FindFirstObjectByType<UserAccountUI>();
            if (ui != null)
            {
                ui.OpenModal();
            }
            return false;
        }

        if (UserAccountManager.Instance.IsLevelUnlocked(level))
        {
            return true;
        }

        ShowLockedWarning(level);
        return false;
    }

    private void ShowLockedWarning(int level)
    {
        int required = level - 1;
        string msg = $"<color=#ff4444>LEVEL {level} IS LOCKED!</color>\nYou must beat Level {required} first. No skipping allowed!";
        ShowNotice(msg);
    }

    private void ShowNotice(string msg)
    {
        Debug.LogWarning(msg);

        if (lockNoticeText == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                GameObject noticeObj = new GameObject("LockNoticeText", typeof(RectTransform), typeof(TextMeshProUGUI));
                noticeObj.transform.SetParent(canvas.transform, false);

                RectTransform rt = noticeObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.1f, 0.08f);
                rt.anchorMax = new Vector2(0.9f, 0.22f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                lockNoticeText = noticeObj.GetComponent<TextMeshProUGUI>();
                lockNoticeText.fontSize = 20f;
                lockNoticeText.alignment = TextAlignmentOptions.Center;
                RageRuntimeUtility.ApplyCinzel(lockNoticeText);
            }
        }

        if (lockNoticeText != null)
        {
            lockNoticeText.text = msg;
            StopCoroutine("HideNoticeAfter");
            StartCoroutine("HideNoticeAfter", 2.5f);
        }
    }

    private IEnumerator HideNoticeAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (lockNoticeText != null)
        {
            lockNoticeText.text = "";
        }
    }

    public void LaunchLV1()
    {
        if (!CheckLevelAccess(1)) return;

        if (BGMCon.instance != null)
        {
            BGMCon.instance.StopMusic();
        }

        SceneManager.LoadScene("LV 1");
    }

    public void LaunchLV2()
    {
        if (!CheckLevelAccess(2)) return;

        if (BGMCon.instance != null)
        {
            BGMCon.instance.StopMusic();
        }

        SceneManager.LoadScene("LV 2");
    }

    public void LaunchLV3()
    {
        if (!CheckLevelAccess(3)) return;

        if (BGMCon.instance != null)
        {
            BGMCon.instance.StopMusic();
        }

        SceneManager.LoadScene("LV 3");
    }

    public void LaunchLV4()
    {
        if (!CheckLevelAccess(4)) return;

        if (BGMCon.instance != null)
        {
            BGMCon.instance.StopMusic();
        }

        SceneManager.LoadScene("LV 4");
    }

    public void LaunchLV5()
    {
        if (!CheckLevelAccess(5)) return;

        if (BGMCon.instance != null)
        {
            BGMCon.instance.StopMusic();
        }

        SceneManager.LoadScene("LV 5");
    }

    public void LaunchLV6()
    {
        if (!CheckLevelAccess(6)) return;

        if (BGMCon.instance != null)
        {
            BGMCon.instance.StopMusic();
        }

        SceneManager.LoadScene("LV 6");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
