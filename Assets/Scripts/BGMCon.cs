using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class BGMCon : MonoBehaviour
{
    public static BGMCon instance;

    [Header("Audio Settings")]
    public AudioClip musicClip;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;

    [Header("Level BGM Serialized Clips")]
    [SerializeField] private AudioClip menuBgm;
    [SerializeField] private AudioClip level1Bgm;
    [SerializeField] private AudioClip level2Bgm;
    [SerializeField] private AudioClip level3Bgm;
    [SerializeField] private AudioClip level4Bgm;
    [SerializeField] private AudioClip level5Bgm;
    [SerializeField] private AudioClip level6Bgm;
    [SerializeField] private AudioClip victoryBgm;
    [SerializeField] private AudioClip gameOverBgm;

    public AudioSource Source => audioSource;
    public float Volume => musicVolume;

    void Awake()
    {
        // Only this dedicated audio object persists. It must not share a GameObject with
        // scene-specific menu or Rules components, because those references are replaced
        // whenever Home or Levels is loaded.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // The scene-authored PersistentAudio object owns this source.
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            Debug.LogError("BGMCon requires its scene-authored AudioSource.", this);
            enabled = false;
            return;
        }

        audioSource.volume = musicVolume;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        AudioClip initialClip = menuBgm != null ? menuBgm : musicClip;
        if (initialClip != null)
        {
            audioSource.clip = initialClip;
            audioSource.Play();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void StopMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    public void PlayMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void PlayClip(AudioClip clip, bool loop = true)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        if (audioSource.clip == clip && audioSource.isPlaying)
        {
            return;
        }

        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.volume = musicVolume;
        audioSource.Play();
    }

    public void PlayLevelBgm(int levelNumber)
    {
        AudioClip clip = GetLevelClip(levelNumber);
        if (clip != null)
        {
            PlayClip(clip, true);
        }
    }

    public AudioClip GetLevelClip(int levelNumber)
    {
        switch (levelNumber)
        {
            case 1: return level1Bgm;
            case 2: return level2Bgm;
            case 3: return level3Bgm;
            case 4: return level4Bgm;
            case 5: return level5Bgm;
            case 6: return level6Bgm;
            default: return menuBgm != null ? menuBgm : musicClip;
        }
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = musicVolume;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Home" || scene.name == "Levels" || scene.name == "About")
        {
            AudioClip clip = menuBgm != null ? menuBgm : musicClip;
            if (clip != null)
            {
                PlayClip(clip, true);
            }
        }
        else if (scene.name == "Victory")
        {
            if (victoryBgm != null)
            {
                PlayClip(victoryBgm, false);
            }
            else
            {
                StopMusic();
            }
        }
        else if (scene.name == "GameOver")
        {
            if (gameOverBgm != null)
            {
                PlayClip(gameOverBgm, false);
            }
            else
            {
                StopMusic();
            }
        }
        else if (scene.name.StartsWith("LV "))
        {
            string numStr = scene.name.Replace("LV ", "").Trim();
            if (int.TryParse(numStr, out int lvl))
            {
                AudioClip clip = GetLevelClip(lvl);
                if (clip != null)
                {
                    PlayClip(clip, true);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
