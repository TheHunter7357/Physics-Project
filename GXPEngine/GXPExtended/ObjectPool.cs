﻿using System.Collections.Generic;
using System;

public class ObjectPool<T> where T : class, new()
{
    private static Dictionary<Type, ObjectPool<T>> _pools = new Dictionary<Type, ObjectPool<T>>();
    private List<T> _pool = new List<T>();
    private static ObjectPool<T> CreateInstance(Type type)
    {
        ObjectPool<T> pool = new ObjectPool<T>();
        _pools.Add(type, pool);
        return pool;
    }
    public static ObjectPool<T> GetInstance(Type type)
    {
        if (!_pools.ContainsKey(type))
            return CreateInstance(type);

        return _pools[type];
    }

    public T GetObject()
    {
        T obj;
        if (_pool.Count > 0)
        {
            obj = _pool[_pool.Count - 1];
            _pool.RemoveAt(_pool.Count - 1);
        }
        else 
            obj = new T();

        return obj;
    }

    public void ReturnObject(T obj) => _pool.Add(obj);
    
    public void InitPool(int amount) 
    {
        for (int i = 0; i < amount; i++)
            _pool.Add(new T());
    }
    public void ClearPool() => _pool.Clear();
}


