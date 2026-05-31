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

    [System.Serializable]
    public class Wave
    {
        public List<SpawnEntry> entries = new List<SpawnEntry>();
        [Tooltip("이 웨이브 스폰 전 대기 시간")]
        public float delayBeforeSpawn = 1f;
    }

    [SerializeField] private List<Wave> waves = new List<Wave>();

    public System.Action onAllEnemiesDead;

    private int currentWaveIndex;
    private int aliveCount;

    void Start()
    {
        currentWaveIndex = 0;
        aliveCount = 0;

        if (waves.Count == 0)
            return;

        StartCoroutine(SpawnWave(waves[0]));
    }

    IEnumerator SpawnWave(Wave wave)
    {
        if (wave.delayBeforeSpawn > 0f)
            yield return new WaitForSeconds(wave.delayBeforeSpawn);

        foreach (var entry in wave.entries)
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
            entry.activeEnemy.onDeath += () => OnEnemyDied();
        }
    }

    void OnEnemyDied()
    {
        aliveCount--;

        if (aliveCount > 0)
            return;

        currentWaveIndex++;

        if (currentWaveIndex < waves.Count)
        {
            StartCoroutine(SpawnWave(waves[currentWaveIndex]));
        }
        else
        {
            onAllEnemiesDead?.Invoke();
        }
    }
}
