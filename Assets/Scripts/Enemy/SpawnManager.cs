using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject prefab;
        public Transform spawnPoint;

        [HideInInspector]
        public EnemyController activeEnemy;
    }

    public List<SpawnEntry> entries = new List<SpawnEntry>();
    public float respawnDelay = 3f;

    void Start()
    {
        foreach (var entry in entries)
            SpawnEnemy(entry);
    }

    void SpawnEnemy(SpawnEntry entry)
    {
        if (entry.prefab == null || entry.spawnPoint == null)
            return;

        var go = Instantiate(entry.prefab, entry.spawnPoint.position, Quaternion.identity);
        entry.activeEnemy = go.GetComponent<EnemyController>();
        if (entry.activeEnemy != null)
            entry.activeEnemy.onDeath += () => StartCoroutine(Respawn(entry));
    }

    IEnumerator Respawn(SpawnEntry entry)
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnEnemy(entry);
    }
}
