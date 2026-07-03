using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Pool
{
    public string tag;          // Unity Tag
    public GameObject prefab;
    public int size = 10;
}

public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    [Header("Pool Settings")]
    public List<Pool> pools;

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolConfigDictionary;
    private Dictionary<string, Transform> poolGroupDictionary;

    protected override void Awake()
    {
        base.Awake();
        InitPools();
    }

    void InitPools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolConfigDictionary = new Dictionary<string, Pool>();
        poolGroupDictionary = new Dictionary<string, Transform>();

        if (pools == null) return;

        foreach (Pool pool in pools)
        {
            if (pool == null || string.IsNullOrEmpty(pool.tag) || pool.prefab == null)
                continue;

            CreatePool(pool.tag, pool.prefab, pool.size);
        }
    }

    // =========================
    // INIT POOL BẰNG CODE
    // =========================
    public void CreatePool(string unityTag, GameObject prefab, int size = 10)
    {
        if (string.IsNullOrEmpty(unityTag) || prefab == null)
        {
            Debug.LogError("❌ CreatePool: tag hoặc prefab null!");
            return;
        }

        // Nếu đã có pool -> skip
        if (poolDictionary.ContainsKey(unityTag))
            return;

        // lưu config
        Pool poolConfig = new Pool()
        {
            tag = unityTag,
            prefab = prefab,
            size = size
        };

        poolConfigDictionary[unityTag] = poolConfig;

        // tạo group theo tag
        Transform group = CreateOrGetGroup(unityTag);

        Queue<GameObject> objectPool = new Queue<GameObject>();
        // init object
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, group);
            obj.name = unityTag;
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }
        poolDictionary.Add(unityTag, objectPool);
    }

    private Transform CreateOrGetGroup(string unityTag)
    {
        if (poolGroupDictionary.TryGetValue(unityTag, out Transform group))
            return group;

        GameObject go = new GameObject($"[POOL] {unityTag}");
        go.transform.SetParent(transform, false);

        poolGroupDictionary[unityTag] = go.transform;
        return go.transform;
    }

    // ==================================
    // GET 2D/3D TRUYỀN PREFAB
    // ==================================

    public GameObject GetObject2D(GameObject prefab, Vector3 position, Transform parent, int initSizeIfNew = 10)
    {
        string unityTag = prefab.name;
        if (!poolDictionary.ContainsKey(unityTag))
        {
            CreatePool(unityTag, prefab, initSizeIfNew);
        }

        return GetObject2D(unityTag, position, parent);
    }

    public GameObject GetObject3D( GameObject prefab, Vector3 position, Quaternion rotation, int initSizeIfNew = 10)
    {
        string unityTag = prefab.name;

        if (!poolDictionary.ContainsKey(unityTag))
        {
            CreatePool(unityTag, prefab, initSizeIfNew);
        }

        return GetObject3D(unityTag, position, rotation);
    }

    // =========================
    //  GIỮ NGUYÊN HÀM CŨ
    // =========================

    public T GetObject3D<T>(string unityTag, Vector3 position) where T : Component
    {
        var obj = GetObject3D(unityTag, position, Quaternion.identity);
        return obj != null ? obj.GetComponent<T>() : null;
    }

    public GameObject GetObject3D(string unityTag, Vector3 position)
    {
        return GetObject3D(unityTag, position, Quaternion.identity);
    }

    public T GetObject2D<T>(string unityTag, Vector3 position, Transform parent) where T : Component
    {
        var obj = GetObject2D(unityTag, position, parent);
        return obj != null ? obj.GetComponent<T>() : null;
    }

    public GameObject GetObject2D(string unityTag, Vector3 position, Transform parent)
    {
        if (!poolDictionary.ContainsKey(unityTag))
        {
            Debug.LogWarning($"❌ Pool with tag {unityTag} does not exist!");
            return null;
        }

        GameObject objectToSpawn;

        if (poolDictionary[unityTag].Count == 0)
        {
            // Tự động tạo thêm object nếu pool rỗng
            if (!poolConfigDictionary.TryGetValue(unityTag, out Pool pool) || pool.prefab == null)
            {
                pool = pools.Find(p => p.tag == unityTag);
            }

            if (pool == null || pool.prefab == null)
            {
                Debug.LogError($"❌ Prefab for pool '{unityTag}' not found!");
                return null;
            }

            Transform group = CreateOrGetGroup(unityTag);

            objectToSpawn = Instantiate(pool.prefab, group);
            objectToSpawn.name = unityTag;
            objectToSpawn.SetActive(false);
        }
        else
        {
            objectToSpawn = poolDictionary[unityTag].Dequeue();
        }

        objectToSpawn.transform.SetParent(parent, false);
        objectToSpawn.transform.localPosition = position;
        objectToSpawn.SetActive(true);

        var pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        pooledObj?.OnObjectSpawn();

        return objectToSpawn;
    }

    public GameObject GetObject3D(string unityTag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(unityTag))
        {
            Debug.LogWarning($"❌ Pool with tag {unityTag} does not exist!");
            return null;
        }

        GameObject objectToSpawn;

        if (poolDictionary[unityTag].Count == 0)
        {
            // Tự động tạo thêm object nếu pool rỗng
            if (!poolConfigDictionary.TryGetValue(unityTag, out Pool pool) || pool.prefab == null)
            {
                pool = pools.Find(p => p.tag == unityTag);
            }

            if (pool == null || pool.prefab == null)
            {
                Debug.LogError($"❌ Prefab for pool '{unityTag}' not found!");
                return null;
            }

            Transform group = CreateOrGetGroup(unityTag);

            objectToSpawn = Instantiate(pool.prefab, group);
            objectToSpawn.name = unityTag;
            objectToSpawn.SetActive(false);
        }
        else
        {
            objectToSpawn = poolDictionary[unityTag].Dequeue();
        }

        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        var pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        pooledObj?.OnObjectSpawn();

        return objectToSpawn;
    }

    /// <summary>
    /// Trả object về pool
    /// </summary>
    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;

        // DÙNG UNITY TAG THẬT
        string unityTag = obj.name;

        if (!poolDictionary.ContainsKey(unityTag))
        {
            Debug.LogWarning($"⚠️ Pool with tag {unityTag} does not exist! Destroy object.");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);

        // trả về đúng group theo tag
        Transform group = CreateOrGetGroup(unityTag);
        obj.transform.SetParent(group, false);

        poolDictionary[unityTag].Enqueue(obj);
    }
}

public interface IPooledObject
{
    void OnObjectSpawn();
}
