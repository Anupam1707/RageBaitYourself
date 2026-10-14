using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinSpawn : MonoBehaviour
{
    [SerializeField]
    GameObject prefab;

    private static CoinSpawn instance;
    private static readonly HashSet<GameObject> spawnedCoins = new HashSet<GameObject>();
    public static int TotalSpawnedCoins { get; private set; } = 0;

    private readonly List<Vector3> spawnPoints = new List<Vector3>
    {
        new Vector3(-11f, -0.1f, -8f),
        new Vector3(-11f, -0.1f, -2f),
        new Vector3(-11f, -0.1f, 4f),
        new Vector3(-11f, -0.1f, 10f),
        new Vector3(-6.5f, -0.1f, 8.3f),
        new Vector3(-2.9f, -0.1f, 4.8f),
        new Vector3(0.5f, -0.1f, 1.1f),
        new Vector3(3.6f, -0.1f, -2f),
        new Vector3(7.9f, -0.1f, -5.9f),
        new Vector3(3.7f, -0.1f, -8.7f),
        new Vector3(-3.7f, -0.1f, -8.7f),
        new Vector3(-3.7f, -0.1f, -6.7f),
        new Vector3(2f, -0.1f, -6.7f),
        new Vector3(-8.7f, -0.1f, -2.3f),
        new Vector3(-8.7f, -0.1f, -11f),
        new Vector3(11f, -0.1f, -11f),
        new Vector3(2.1f, -0.1f, 2.5f),
        new Vector3(6.3f, -0.1f, 7.5f),
        new Vector3(8.6f, -0.1f, 2.5f),
        new Vector3(0.2f, -0.1f, 8.2f),
        new Vector3(10.5f, -0.1f, -2f),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        spawnedCoins.Clear();
        TotalSpawnedCoins = 0;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
        spawnedCoins.Clear();
        TotalSpawnedCoins = 0;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            spawnedCoins.Clear();
            TotalSpawnedCoins = 0;
        }
    }

    void Start()
    {
        if (instance != this)
        {
            return;
        }

        spawnedCoins.Clear();
        foreach (Vector3 point in spawnPoints)
        {
            SpawnCoinIndependently(point);
        }
        TotalSpawnedCoins = spawnedCoins.Count;
    }

    public static bool HasUncollectedCoins()
    {
        spawnedCoins.RemoveWhere(c => c == null || !c.activeInHierarchy);
        return spawnedCoins.Count > 0;
    }

    public static int RemainingCoinsCount()
    {
        spawnedCoins.RemoveWhere(c => c == null || !c.activeInHierarchy);
        return spawnedCoins.Count;
    }

    public static void UnregisterCoin(GameObject coin)
    {
        if (coin != null)
        {
            spawnedCoins.Remove(coin);
        }
    }

    private void SpawnCoinIndependently(Vector3 pos)
    {
        if (prefab == null)
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(90f, Random.Range(0f, 720), 0f);
        GameObject coin = Instantiate(prefab, pos, rotation);
        if (coin != null)
        {
            Collider[] colliders = coin.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders)
            {
                c.isTrigger = true;
            }
            spawnedCoins.Add(coin);
        }
    }

    void Update()
    {
        
    }
}
