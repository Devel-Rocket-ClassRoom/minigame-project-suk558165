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
    private int pendingSpawnCount;
    private bool waitingForTrigger;
    private bool allWavesCleared;

    void Start()
    {
        currentWaveIndex = 0;
        aliveCount = 0;
        waitingForTrigger = false;

        foreach (var wave in waves)
        {
            if (wave.triggerZone != null)
            {
                // 같은 GameObject의 모든 Collider2D를 트리거로 강제 — 플레이어가 충돌로 막히지 않게.
                foreach (var col in wave.triggerZone.GetComponents<Collider2D>())
                    col.isTrigger = true;

                // 트리거 이벤트는 콜라이더가 붙은 GameObject에서만 발생하므로 포워더 부착.
                if (wave.triggerZone.GetComponent<WaveTriggerForwarder>() == null)
                {
                    var fwd = wave.triggerZone.gameObject.AddComponent<WaveTriggerForwarder>();
                    fwd.target = this;
                }

                wave.triggerZone.gameObject.SetActive(false);
            }
        }

        if (waves.Count == 0)
            return;

        var bossIntro = GetComponentInChildren<BossIntro>();
        if (bossIntro != null)
        {
            // 인트로와 첫 웨이브 스폰을 병렬로 시작 — 보스가 스폰되는 모습을 카메라가 잡도록.
            bossIntro.Play(null);
            StartCoroutine(SpawnWave(waves[0]));
        }
        else
        {
            StartCoroutine(SpawnWave(waves[0]));
        }
    }

    IEnumerator SpawnWave(Wave wave)
    {
        if (wave.delayBeforeSpawn > 0f)
            yield return new WaitForSeconds(wave.delayBeforeSpawn);

        // 이 웨이브가 스폰할 총 적 수를 미리 더해서, 스폰이 다 끝나기 전엔 클리어 판정이 안 나도록.
        int totalPending = 0;
        foreach (var group in wave.groups)
        {
            if (
                group.prefab != null
                && group.spawnPointIndex >= 0
                && group.spawnPointIndex < spawnPoints.Length
                && spawnPoints[group.spawnPointIndex] != null
            )
            {
                totalPending += group.count;
            }
        }
        pendingSpawnCount += totalPending;

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
            // 이미 웨이브 진행 자체가 끝났으면 남은 스폰 취소 (트리거 기반 다음 웨이브 진입 등).
            if (allWavesCleared)
            {
                pendingSpawnCount = Mathf.Max(0, pendingSpawnCount - (group.count - i));
                yield break;
            }
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
        pendingSpawnCount = Mathf.Max(0, pendingSpawnCount - 1);

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

        // 이번 웨이브가 아직 스폰 중이면 클리어 판정 보류.
        if (pendingSpawnCount > 0)
            return;

        AdvanceWave();
    }

    void AdvanceWave()
    {
        currentWaveIndex++;

        if (currentWaveIndex >= waves.Count)
        {
            allWavesCleared = true;
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

    // SpawnManager 자체 GameObject에 콜라이더가 있을 때를 위한 fallback.
    void OnTriggerEnter2D(Collider2D other) => HandleTrigger(other);

    // WaveTriggerForwarder에서 호출됨.
    public void NotifyWaveTrigger(Collider2D other) => HandleTrigger(other);

    void HandleTrigger(Collider2D other)
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
