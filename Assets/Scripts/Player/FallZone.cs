using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 맵 아래로 떨어졌을 때의 처리. 방 프리팹 루트에 붙인다.
///
/// 지형에 구덩이를 만들려면 반드시 필요하다. 이게 없으면 플레이어가
/// 무한히 떨어져 게임이 진행 불가 상태가 된다.
/// 던그리드 방식 — 즉사시키지 않고 피해를 준 뒤 마지막으로 밟았던 곳으로 되돌린다.
/// </summary>
public class FallZone : MonoBehaviour
{
    [Tooltip("이 Y 아래로 내려가면 낙하로 판정. 0이면 Floor 타일맵 최하단에서 자동 계산")]
    public float killY = 0f;

    [Tooltip("자동 계산 시 지형 최하단에서 얼마나 더 아래를 기준으로 삼을지")]
    public float autoMargin = 6f;

    [Tooltip("낙하 시 잃는 체력 (유효 최대 체력 대비 비율)")]
    [Range(0f, 1f)]
    public float damageRatio = 0.12f;

    [Tooltip("복귀 지점을 갱신하는 간격(초). 매 프레임 갱신할 필요는 없다")]
    public float sampleInterval = 0.25f;

    private Vector3 lastSafePos;
    private bool hasSafePos;
    private float sampleTimer;

    void Start()
    {
        if (Mathf.Approximately(killY, 0f))
            killY = ComputeKillY();
    }

    /// <summary>방 안의 Floor 타일맵 중 가장 낮은 지점보다 아래를 낙하 기준으로 삼는다.</summary>
    float ComputeKillY()
    {
        float lowest = float.PositiveInfinity;
        foreach (var tm in GetComponentsInChildren<Tilemap>(true))
        {
            if (tm.gameObject.layer != LayerMask.NameToLayer("Ground"))
                continue;
            tm.CompressBounds();
            var b = tm.localBounds;
            if (b.size == Vector3.zero)
                continue;
            lowest = Mathf.Min(lowest, tm.transform.TransformPoint(b.min).y);
        }
        return float.IsInfinity(lowest) ? -50f : lowest - autoMargin;
    }

    void Update()
    {
        if (!PlayerRef.Exists)
            return;

        var player = PlayerRef.Transform;
        var health = PlayerRef.Health;
        if (health == null || health.IsDead)
            return;

        // 밟고 있는 동안만 복귀 지점을 기록한다.
        var movement = PlayerRef.Movement;
        sampleTimer -= Time.deltaTime;
        if (sampleTimer <= 0f)
        {
            sampleTimer = sampleInterval;
            if (movement != null && movement.IsGrounded && player.position.y > killY)
            {
                lastSafePos = player.position;
                hasSafePos = true;
            }
        }

        if (player.position.y > killY)
            return;

        Fall(player, health);
    }

    void Fall(Transform player, PlayerHealth health)
    {
        // 복귀 지점이 아직 없으면(스폰 직후 추락 등) 스폰포인트로 되돌린다.
        Vector3 target = lastSafePos;
        if (!hasSafePos)
        {
            var spawn = GetComponentInChildren<PlayerSpawnPoint>(true);
            target = spawn != null ? spawn.transform.position : player.position + Vector3.up * 10f;
        }

        player.position = target;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        CameraFollow.Instance?.SnapToTarget();
        CameraFollow.Instance?.Shake(0.3f, 0.4f);

        // 낙하 피해는 반사·회피 대상이 아니므로 공격자 없이 전달한다.
        health.TakeDamage(health.EffectiveMaxHp * damageRatio);
    }

    void OnDrawGizmosSelected()
    {
        float y = Mathf.Approximately(killY, 0f) ? ComputeKillY() : killY;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawLine(new Vector3(transform.position.x - 60f, y, 0f),
                        new Vector3(transform.position.x + 60f, y, 0f));
    }
}
