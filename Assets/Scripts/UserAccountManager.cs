using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserAccount
{
    public string username = "";
    public int maxUnlockedLevel = 1;
    public int totalScore = 0;
    public int totalDeaths = 0;
    public int totalWins = 0;
    public string lastPlayed;
    public bool completedAllLevels = false;

    public UserAccount() { }

    public UserAccount(string name)
    {
        username = name;
        maxUnlockedLevel = 1;
        totalScore = 0;
        totalDeaths = 0;
        totalWins = 0;
        lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        completedAllLevels = false;
    }
}

[Serializable]
public class UserAccountData
{
    public string activeUsername = "";
    public List<UserAccount> accounts = new List<UserAccount>();
}

/// <summary>
/// Persistent account and progressive level unlock system.
/// Handles account creation, profile switching, stats saving,
/// and enforces strictly sequential level progression.
/// No sample account is created automatically; user must create their own profile.
/// </summary>
public class UserAccountManager : MonoBehaviour
{
    private const string PREFS_KEY = "RAGEBAIT_USER_ACCOUNTS";
    private static UserAccountManager instance;

    public static UserAccountManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject host = new GameObject("UserAccountManager");
                instance = host.AddComponent<UserAccountManager>();
                DontDestroyOnLoad(host);
            }
            return instance;
        }
    }

    [Header("Runtime State")]
    [SerializeField] private UserAccountData data = new UserAccountData();

    public UserAccount ActiveAccount { get; private set; }
    public bool HasAccount => ActiveAccount != null;

    public event Action OnAccountChanged;
    public event Action OnProgressChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadData();
    }

    public static string DatabaseFilePath => System.IO.Path.Combine(Application.persistentDataPath, "user_accounts_db.json");

    private void LoadData()
    {
        bool loaded = false;
        string dbPath = DatabaseFilePath;

        if (System.IO.File.Exists(dbPath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(dbPath);
                data = JsonUtility.FromJson<UserAccountData>(json);
                loaded = true;
                Debug.Log($"[UserAccountManager] Loaded database from file: {dbPath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UserAccountManager] Failed to read {dbPath}: {ex.Message}");
            }
        }

        if (!loaded && PlayerPrefs.HasKey(PREFS_KEY))
        {
            try
            {
                string json = PlayerPrefs.GetString(PREFS_KEY);
                data = JsonUtility.FromJson<UserAccountData>(json);
                loaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to parse UserAccountData from PlayerPrefs: " + ex.Message);
            }
        }

        if (!loaded || data == null)
        {
            data = new UserAccountData();
        }

        if (data.accounts == null)
        {
            data.accounts = new List<UserAccount>();
        }

        // Clean out legacy sample account "RageMaster" if present
        data.accounts.RemoveAll(a => a.username.Equals("RageMaster", StringComparison.OrdinalIgnoreCase));
        if (data.activeUsername != null && data.activeUsername.Equals("RageMaster", StringComparison.OrdinalIgnoreCase))
        {
            data.activeUsername = "";
        }

        // Do not create a sample account! Let user create their own account.
        if (data.accounts.Count > 0)
        {
            ActiveAccount = data.accounts.Find(a => a.username == data.activeUsername);
            if (ActiveAccount == null)
            {
                ActiveAccount = data.accounts[0];
                data.activeUsername = ActiveAccount.username;
            }

            ActiveAccount.lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }
        else
        {
            ActiveAccount = null;
            data.activeUsername = "";
        }

        SaveData();
    }

    public void ReloadData()
    {
        LoadData();
        OnAccountChanged?.Invoke();
        OnProgressChanged?.Invoke();
    }

    public void SaveData()
    {
        if (data == null) return;
        try
        {
            string json = JsonUtility.ToJson(data, true);

            // 1. Save to tangible JSON file on disk
            string dbPath = DatabaseFilePath;
            string dir = System.IO.Path.GetDirectoryName(dbPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllText(dbPath, json);

            // 2. Mirror in PlayerPrefs for backup
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();

            Debug.Log($"[UserAccountManager] DB saved ({data.accounts.Count} accounts) at: {dbPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save UserAccountData: " + ex.Message);
        }
    }

    public List<UserAccount> GetAllAccounts()
    {
        return data.accounts ?? new List<UserAccount>();
    }

    public bool SwitchAccount(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;

        UserAccount target = data.accounts.Find(a => a.username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (target != null)
        {
            ActiveAccount = target;
            data.activeUsername = target.username;
            target.lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            SaveData();
            OnAccountChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool CreateAccount(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        username = username.Trim();

        UserAccount existing = data.accounts.Find(a => a.username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            SwitchAccount(existing.username);
            return true;
        }

        UserAccount newAcc = new UserAccount(username);
        data.accounts.Add(newAcc);
        ActiveAccount = newAcc;
        data.activeUsername = newAcc.username;
        SaveData();
        OnAccountChanged?.Invoke();
        return true;
    }

    public bool DeleteAccount(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || data == null || data.accounts == null) return false;

        UserAccount target = data.accounts.Find(a => a.username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (target == null) return false;

        bool wasActive = (ActiveAccount != null && ActiveAccount.username.Equals(target.username, StringComparison.OrdinalIgnoreCase));

        data.accounts.Remove(target);

        if (wasActive)
        {
            if (data.accounts.Count > 0)
            {
                ActiveAccount = data.accounts[0];
                data.activeUsername = ActiveAccount.username;
                ActiveAccount.lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            }
            else
            {
                ActiveAccount = null;
                data.activeUsername = "";
            }
        }

        SaveData();
        OnAccountChanged?.Invoke();
        OnProgressChanged?.Invoke();
        return true;
    }

    public bool DeleteActiveAccount()
    {
        if (ActiveAccount == null) return false;
        return DeleteAccount(ActiveAccount.username);
    }

    public void ResetActiveAccountProgress()
    {
        if (ActiveAccount == null) return;
        ActiveAccount.maxUnlockedLevel = 1;
        ActiveAccount.totalScore = 0;
        ActiveAccount.completedAllLevels = false;
        SaveData();
        OnProgressChanged?.Invoke();
    }

    public bool IsLevelUnlocked(int levelNumber)
    {
        // No skipping allowed, and player must have an active profile
        if (ActiveAccount == null) return false;
        if (levelNumber <= 1) return true; // Level 1 is unlocked once an account exists
        return ActiveAccount.maxUnlockedLevel >= levelNumber;
    }

    public bool HasCompletedAllLevels()
    {
        if (ActiveAccount == null) return false;
        return ActiveAccount.completedAllLevels;
    }

    public void UnlockNextLevel(int completedLevel)
    {
        if (ActiveAccount == null) return;

        if (completedLevel >= 6)
        {
            ActiveAccount.completedAllLevels = true;
            Debug.Log($"[UserAccount] ALL LEVELS COMPLETED by {ActiveAccount.username}!");
        }

        // Progressive unlock: only unlock next if completing the latest reached level
        if (completedLevel >= ActiveAccount.maxUnlockedLevel)
        {
            int nextLevel = Mathf.Clamp(completedLevel + 1, 1, 6);
            if (nextLevel > ActiveAccount.maxUnlockedLevel)
            {
                ActiveAccount.maxUnlockedLevel = nextLevel;
                Debug.Log($"[UserAccount] Level {nextLevel} unlocked for {ActiveAccount.username}!");
            }
        }

        SaveData();
        OnProgressChanged?.Invoke();
    }

    public void RecordScore(int points)
    {
        if (ActiveAccount == null) return;
        ActiveAccount.totalScore += points;
        SaveData();
    }

    public void RecordDeath()
    {
        if (ActiveAccount == null) return;
        ActiveAccount.totalDeaths++;
        SaveData();
    }

    public void RecordWin()
    {
        if (ActiveAccount == null) return;
        ActiveAccount.totalWins++;
        SaveData();
    }
}
