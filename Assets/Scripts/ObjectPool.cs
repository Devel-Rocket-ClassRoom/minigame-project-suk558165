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
        T obj = null;
        // 씬 전환 등으로 파괴된 항목이 큐에 남아 있을 수 있으므로 살아있는 것만 꺼낸다
        while (pool.Count > 0 && obj == null)
            obj = pool.Dequeue();

        if (obj == null)
            obj = Object.Instantiate(prefab, parent);

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Release(T obj)
    {
        if (obj == null)
            return;
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
