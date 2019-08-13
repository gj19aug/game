﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

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

    public void Initialize(string name, T prefab, int initialCount)
    {
        // TODO: Assert prefab is actually a prefab
        root = new GameObject(name).GetComponent<Transform>();
        pool = new List<T>(initialCount);
        active = new List<T>(initialCount);

        for (int i = 0; i < initialCount; i++)
            CreateInstance(prefab);
    }

    bool warned = false;
    public T Spawn()
    {
        if (pool.Count == 0)
        {
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

    public void Despawn(T instance)
    {
        instance.gameObject.SetActive(false);
        bool removed = active.Remove(instance);
        Assert.IsTrue(removed);
        pool.Add(instance);
    }
}
