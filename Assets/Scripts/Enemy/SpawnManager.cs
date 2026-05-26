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
    [SerializeField] private bool respawnEnabled = false;
    [SerializeField] private float respawnDelay = 3f;

    public System.Action onAllEnemiesDead;

    private int aliveCount;

    void Start()
    {
        aliveCount = 0;
        foreach (var entry in entries)
        {
            SpawnEnemy(entry);
            aliveCount++;
        }
    }

    void SpawnEnemy(SpawnEntry entry)
    {
        if (entry.prefab == null || entry.spawnPoint == null)
            return;

        var go = Instantiate(entry.prefab, entry.spawnPoint.position, Quaternion.identity);
        entry.activeEnemy = go.GetComponent<EnemyController>();
        if (entry.activeEnemy != null)
        {
            entry.activeEnemy.onDeath += () => OnEnemyDied(entry);
        }
    }

    void OnEnemyDied(SpawnEntry entry)
    {
        aliveCount--;

        if (respawnEnabled)
        {
            StartCoroutine(Respawn(entry));
            return;
        }

        if (aliveCount <= 0)
            onAllEnemiesDead?.Invoke();
    }

    IEnumerator Respawn(SpawnEntry entry)
    {
        yield return new WaitForSeconds(respawnDelay);
        aliveCount++;
        SpawnEnemy(entry);
    }
}
