using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages object pooling for cubes, obstacles, effects, and combo texts to reduce instantiation cost.
/// Allows spawning and despawning objects efficiently using predefined pool configurations.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    // Singleton instance for easy access
    public static ObjectPoolManager Instance { get; private set; }

    // Dictionary to hold all pooled objects by their unique keys
    public static Dictionary<string, PooledObjectInfo> poolDictionary = new Dictionary<string, PooledObjectInfo>();

    [Header("Pool Configuration")]
    [Tooltip("List of prefabs and their respective preload amounts for each pool key.")]
    [SerializeField] private List<PoolConfig> poolConfigs;

    [Header("Pool Containers")]
    [Tooltip("Parent container for pooled cube objects.")]
    [SerializeField] private Transform cubePoolContainer;
    [Tooltip("Parent container for pooled obstacle objects.")]
    [SerializeField] private Transform obstaclePoolContainer;
    [Tooltip("Parent container for pooled effect objects.")]
    [SerializeField] private Transform effectPoolContainer;
    [Tooltip("Parent container for pooled combo text objects.")]
    [SerializeField] private Transform comboTextContainer;

    /// <summary>
    /// Initializes the singleton instance and pool containers. Preloads objects based on their configuration.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Do not destroy this object on scene load to keep the pools alive
            DontDestroyOnLoad(this.gameObject);

            // Create default containers if not assigned
            if (cubePoolContainer == null)
                cubePoolContainer = new GameObject("CubePoolContainer").transform;
            if (obstaclePoolContainer == null)
                obstaclePoolContainer = new GameObject("ObstaclePoolContainer").transform;
            if (effectPoolContainer == null)
                effectPoolContainer = new GameObject("EffectPoolContainer").transform;
            if (comboTextContainer == null)
                comboTextContainer = new GameObject("ComboTextPoolContainer").transform;

            // Assign containers to this object
            cubePoolContainer.SetParent(transform);
            obstaclePoolContainer.SetParent(transform);
            effectPoolContainer.SetParent(transform);
            comboTextContainer.SetParent(transform);

            // Preload all pools based on the configuration
            PreloadPools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Spawns an object from the pool using a given key and spawn position.
    /// Instantiates one, if the pool is empty.
    /// </summary>
    /// <param name="poolKey">The key identifying the pool (e.g., "RedCube").</param>
    /// <param name="spawnPosition">The position to spawn the object at.</param>
    /// <param name="inBoard">The flag for whether to parent the object to the GameBoard.</param>
    public static GameObject SpawnObjectByKey(string poolKey, Vector2 spawnPosition, bool inBoard = true)
    {
        // Get the pool by key, or log an error if it doesn't exist
        if (!poolDictionary.TryGetValue(poolKey, out PooledObjectInfo pool))
        {
            Debug.LogError($"[ObjectPoolManager] Pool '{poolKey}' not found.");
            return null;
        }

        // Set the initial object to null
        GameObject obj = null;

        // Check if there are inactive objects available in the pool
        if (pool.inactiveObjects.Count > 0)
        {
            // Get the first inactive object from the pool
            obj = pool.inactiveObjects[0];
            // Remove it from the inactive list
            pool.inactiveObjects.RemoveAt(0);

            // Check the pool key to determine where to parent the object
            if (poolKey == "ComboText")
            {
                obj.transform.SetParent(GameObject.Find("MiddleUIContainer")?.transform, false);
            }
            else if (!inBoard)
            {
                obj.transform.SetParent(GameObject.Find("Particles")?.transform, false);
            }
            else
            {
                obj.transform.SetParent(GameBoard.Instance.transform);
            }

            // Reset the scale, set the position, and activate the object
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = spawnPosition;
            obj.SetActive(true);
        }
        // If no inactive objects are available, instantiate a new one from the prefab
        else
        {
            // Find the prefab associated with the pool key
            GameObject prefab = Instance.poolConfigs.Find(c => c.poolKey == poolKey)?.prefab;
            // If the prefab is not found, log an error and return null
            if (prefab == null)
            {
                Debug.LogError($"[ObjectPoolManager] Missing prefab for key '{poolKey}', cannot spawn.");
                return null;
            }

            // Otherwise, instantiate a new object
            obj = Instantiate(prefab, spawnPosition, Quaternion.identity);
            obj.name = poolKey + "(Clone)";
            obj.transform.localScale = Vector3.one;
            obj.transform.SetParent(inBoard ? Instance.cubePoolContainer : Instance.effectPoolContainer);
            obj.transform.localPosition = spawnPosition;
            obj.SetActive(true);

            // Log a warning that the pool was exhausted and a new object was instantiated
            Debug.LogWarning($"[ObjectPoolManager] Pool '{poolKey}' exhausted. Instantiated new object.");
        }
        // Then, return the spawned object
        return obj;
    }

    /// <summary>
    /// Spawns an object from the pool using a reference to the object.
    /// </summary>
    /// <param name="objToSpawn">The prefab or reference object to spawn.</param>
    /// <param name="spawnPosition">The position to spawn the object at.</param>
    public static GameObject SpawnObject(GameObject objToSpawn, Vector2 spawnPosition)
    {
        // Get the base name of the object to spawn, which is used as the key in the pool dictionary
        string baseName = GetBaseName(objToSpawn.name);

        // Check if the pool for this base name exists, if not, create it
        if (!poolDictionary.TryGetValue(baseName, out PooledObjectInfo pool))
        {
            pool = new PooledObjectInfo();
            pool.lookupString = baseName;
            poolDictionary.Add(baseName, pool);
            Debug.LogWarning("Created missing pool for object: " + baseName);
        }

        GameObject obj;

        // Follow the same logic as in SpawnObjectByKey, but using the base name
        if (pool.inactiveObjects.Count > 0)
        {
            obj = pool.inactiveObjects[0];
            pool.inactiveObjects.RemoveAt(0);
            obj.SetActive(true);
            obj.transform.SetParent(null);
            obj.transform.localPosition = spawnPosition;
        }
        else
        {
            obj = Instantiate(objToSpawn, spawnPosition, Quaternion.identity);
            obj.name = baseName + "(Clone)";
            Debug.LogWarning($"Pool '{baseName}' exhausted. Instantiating extra object.");
        }
        // Then, return the spawned object
        return obj;
    }

    /// <summary>
    /// Returns an object back into its pool and deactivates it.
    /// </summary>
    /// <param name="objToDespawn">The object to return to the pool.</param>
    public static void DespawnObject(GameObject objToDespawn)
    {
        // Get the base name of the object to despawn, which is used as the key in the pool dictionary
        string baseName = GetBaseName(objToDespawn.name);

        // Check if the pool for this base name exists, if not, create it
        if (!poolDictionary.TryGetValue(baseName, out PooledObjectInfo pool))
        {
            pool = new PooledObjectInfo();
            poolDictionary.Add(baseName, pool);
            Debug.LogWarning("Created missing pool for object: " + baseName);
        }

        // Deactivate the object and return it to the pool
        objToDespawn.SetActive(false);

        // Check the base name to determine what pool to return the object to
        if (baseName.Contains("Cube"))
            objToDespawn.transform.SetParent(Instance.cubePoolContainer);
        else if (baseName.Contains("Obstacle"))
            objToDespawn.transform.SetParent(Instance.obstaclePoolContainer);
        else
            objToDespawn.transform.SetParent(Instance.effectPoolContainer);

        // Add the object back to the inactive objects list of the pool
        pool.inactiveObjects.Add(objToDespawn);
    }

    /// <summary>
    /// Returns the base name of a prefab by stripping the "(Clone)" suffix.
    /// </summary>
    public static string GetBaseName(string name)
    {
        return name.Split('(')[0];
    }

    /// <summary>
    /// Converts a CubeColor enum to the corresponding pool key string.
    /// </summary>
    public static string GetPoolKeyFromColor(CubeColor color)
    {
        return color switch
        {
            CubeColor.Red => "RedCube",
            CubeColor.Blue => "BlueCube",
            CubeColor.Black => "BlackCube",
            CubeColor.Yellow => "YellowCube",
            CubeColor.White => "WhiteCube",
            _ => "UnknownCube"
        };
    }

    /// <summary>
    /// Preloads all configured pools by instantiating the defined number of inactive objects.
    /// </summary>
    public void PreloadPools()
    {
        // Loop through each pool configuration and instantiate the required number of objects
        foreach (var config in poolConfigs)
        {
            // Check if the pool key already exists in the dictionary, if not, create a new entry
            if (!poolDictionary.ContainsKey(config.poolKey))
            {
                poolDictionary[config.poolKey] = new PooledObjectInfo();
            }

            // Find the parent container based on the pool key
            Transform parent = config.poolKey.Contains("ComboText") ? comboTextContainer :
                               config.poolKey.Contains("Obstacle") ? obstaclePoolContainer :
                               config.poolKey.Contains("Cube") ? cubePoolContainer :
                               effectPoolContainer;

            // Loop to instantiate the specified number of inactive objects
            for (int i = 0; i < config.preloadCount; i++)
            {
                GameObject obj = Instantiate(config.prefab);
                obj.name = config.poolKey + "(Clone)";
                obj.transform.SetParent(parent);
                obj.SetActive(false);
                poolDictionary[config.poolKey].inactiveObjects.Add(obj);
            }
        }
    }
}

/// <summary>
/// Holds inactive GameObjects for a specific pool key.
/// </summary>
public class PooledObjectInfo
{
    public string lookupString;
    public List<GameObject> inactiveObjects = new List<GameObject>();
}

/// <summary>
/// Serializable configuration for a single object pool.
/// </summary>
[System.Serializable]
public class PoolConfig
{
    [Tooltip("Unique key for the object pool (e.g., RedCube, StoneObstacle, ComboText).")]
    public string poolKey;
    [Tooltip("Prefab that should be instantiated and pooled.")]
    public GameObject prefab;
    [Tooltip("Number of instances to preload at the start of the game.")]
    public int preloadCount;
}