using UnityEngine;
using System.Collections.Generic;

public class ObjectPool<T> where T :Component
{
    readonly T prefab;
    readonly Transform parent;
    readonly Queue<T> pool = new();
    public ObjectPool(T prefab, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    public T Get(Vector3 position, Quaternion rotation)
    {
        T obj = pool.Count > 0 ? pool.Dequeue() : Object.Instantiate(prefab, parent);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Release(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
