using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public class Pool<T> where T : MonoBehaviour
{
    public T prefab;
    public Transform root;
    public List<T> pool;
    public List<T> active;

    // NOTE: Wildly inefficient
    private T CreateInstance(T prefab)
    {
        // TODO: Instantiate this disabled?
        T instance = GameObject.Instantiate<T>(prefab, Vector3.zero, Quaternion.identity, root);
        instance.gameObject.SetActive(false);
        pool.Add(instance);
        return instance;
    }

    public void Initialize(T prefab, int initialCount)
    {
        // TODO: Assert prefab is actually a prefab
        this.prefab = prefab;
        string name = string.Format("Pool ({0})", prefab.name);
        root = new GameObject(name).GetComponent<Transform>();
        pool = new List<T>(initialCount);
        active = new List<T>(initialCount);

        for (int i = 0; i < initialCount; i++)
            CreateInstance(prefab);
    }

    public bool IsSpawned(T instance) { return active.Contains(instance); }
    public bool IsDespawned(T instance) { return pool.Contains(instance); }
    public bool Contains(T instance) { return active.Contains(instance) || pool.Contains(instance); }

    bool warned = false;
    public T Spawn()
    {
        if (pool.Count == 0)
        {
            // NOTE: Double the size of the pool
            int newCapacity = active.Count + pool.Count;
            for (int i = 0; i < newCapacity; i++)
                CreateInstance(prefab);

            if (!warned)
            {
                warned = true;
                Debug.LogWarningFormat("Pool '{0}' exceeded initial capacity", root.name);
            }
        }

        T instance = pool[pool.Count - 1];
        pool.RemoveAt(pool.Count - 1);
        active.Add(instance);
        instance.gameObject.SetActive(true);
        return instance;
    }

    public bool TryDespawn(T instance)
    {
        instance.gameObject.SetActive(false);
        if (active.Remove(instance))
        {
            pool.Add(instance);
            return true;
        }
        return false;
    }

    public void Despawn(T instance)
    {
        bool removed = TryDespawn(instance);
        Assert.IsTrue(removed);
    }

    public bool Remove(T instance)
    {
        return active.Remove(instance);
    }

    public void Add(T instance)
    {
        active.Add(instance);
    }
}
