using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [System.Serializable]
    public class SpawnGroup
    {
        public GameObject prefab;

        [Tooltip("이 그룹이 사용할 스폰포인트 (spawnPoints 배열의 인덱스)")]
        public int spawnPointIndex;

        [Tooltip("이 포인트에서 스폰할 마릿수")]
        public int count = 1;

        [Tooltip("여러 마리일 때 스폰 간격")]
        public float interval = 0.5f;
    }

    [System.Serializable]
    public class Wave
    {
        public List<SpawnGroup> groups = new List<SpawnGroup>();

        [Tooltip("이 웨이브 스폰 전 대기 시간")]
        public float delayBeforeSpawn = 1f;

        [Tooltip(
            "비어있으면 전멸 시 바로 다음 웨이브. 지정하면 전멸 후 이 존에 진입해야 다음 웨이브"
        )]
        public Collider2D triggerZone;
    }

    [Header("스폰 포인트 (좌끝, 좌중간, 중앙, 우중간, 우끝 등)")]
    [SerializeField]
    private Transform[] spawnPoints;

    [Header("스폰 이펙트")]
    [SerializeField]
    private GameObject spawnEffectPrefab;

    [Tooltip("이펙트 재생 후 몬스터가 나타나기까지 대기 시간")]
    [SerializeField]
    private float spawnEffectDelay = 0.5f;

    [Tooltip("이펙트 생성 Y 오프셋 (음수 = 아래)")]
    [SerializeField]
    private float spawnEffectYOffset = -0.5f;

    [Header("웨이브 설정")]
    [SerializeField]
    private List<Wave> waves = new List<Wave>();

    public System.Action onAllEnemiesDead;

    private int currentWaveIndex;
    private int aliveCount;
    private bool waitingForTrigger;

    void Start()
    {
        currentWaveIndex = 0;
        aliveCount = 0;
        waitingForTrigger = false;

        foreach (var wave in waves)
        {
            if (wave.triggerZone != null)
            {
                wave.triggerZone.isTrigger = true;
                wave.triggerZone.gameObject.SetActive(false);
            }
        }

        if (waves.Count == 0)
            return;

        var bossIntro = GetComponentInChildren<BossIntro>();
        if (bossIntro != null)
            bossIntro.Play(() => StartCoroutine(SpawnWave(waves[0])));
        else
            StartCoroutine(SpawnWave(waves[0]));
    }

    IEnumerator SpawnWave(Wave wave)
    {
        if (wave.delayBeforeSpawn > 0f)
            yield return new WaitForSeconds(wave.delayBeforeSpawn);

        foreach (var group in wave.groups)
            StartCoroutine(SpawnGroupItems(group));
    }

    IEnumerator SpawnGroupItems(SpawnGroup group)
    {
        if (group.prefab == null)
            yield break;
        if (group.spawnPointIndex < 0 || group.spawnPointIndex >= spawnPoints.Length)
            yield break;

        Transform point = spawnPoints[group.spawnPointIndex];
        if (point == null)
            yield break;

        for (int i = 0; i < group.count; i++)
        {
            yield return SpawnEnemyWithEffect(group.prefab, point.position);
            if (i < group.count - 1 && group.interval > 0f)
                yield return new WaitForSeconds(group.interval);
        }
    }

    IEnumerator SpawnEnemyWithEffect(GameObject prefab, Vector3 position)
    {
        if (spawnEffectPrefab != null)
        {
            var fxPos = position + new Vector3(0f, spawnEffectYOffset, 0f);
            var fx = Instantiate(spawnEffectPrefab, fxPos, Quaternion.identity);
            Destroy(fx, 3f);
            if (spawnEffectDelay > 0f)
                yield return new WaitForSeconds(spawnEffectDelay);
        }

        var go = Instantiate(prefab, position, Quaternion.identity);
        aliveCount++;

        var enemy = go.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.onDeath += OnEnemyDied;
            yield break;
        }

        var boss = go.GetComponent<BossController>();
        if (boss != null)
        {
            boss.onDeath += OnEnemyDied;
            yield break;
        }

        var miniBoss = go.GetComponent<MiniBossController>();
        if (miniBoss != null)
            miniBoss.onDeath += OnEnemyDied;
    }

    void OnEnemyDied()
    {
        aliveCount--;

        if (aliveCount > 0)
            return;

        AdvanceWave();
    }

    void AdvanceWave()
    {
        currentWaveIndex++;

        if (currentWaveIndex >= waves.Count)
        {
            onAllEnemiesDead?.Invoke();
            return;
        }

        Wave nextWave = waves[currentWaveIndex];

        Wave clearedWave = waves[currentWaveIndex - 1];
        if (clearedWave.triggerZone != null)
        {
            waitingForTrigger = true;
            clearedWave.triggerZone.gameObject.SetActive(true);
        }
        else
        {
            StartCoroutine(SpawnWave(nextWave));
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!waitingForTrigger)
            return;
        if (!other.CompareTag("Player"))
            return;

        waitingForTrigger = false;

        Wave clearedWave = waves[currentWaveIndex - 1];
        if (clearedWave.triggerZone != null)
            clearedWave.triggerZone.gameObject.SetActive(false);

        StartCoroutine(SpawnWave(waves[currentWaveIndex]));
    }
}
