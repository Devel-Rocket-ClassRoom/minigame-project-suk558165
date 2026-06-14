using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public class ShieldController : MonoBehaviour
{
    [Header("Shield")]
    [Tooltip("방패가 막는 앞쪽 각도 범위 (좌우 각각)")]
    public float blockAngle = 70f;

    [Header("Feedback")]
    public AudioClip blockSound;

    private EnemyController _enemy;
    private SpriteRenderer _sr;
    private Transform _player;
    private CancellationTokenSource _blockFlashCts;

    void Awake()
    {
        _enemy = GetComponent<EnemyController>();
        _sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        _player = PlayerRef.Transform;
        _enemy.isAttackBlocked = IsBlocked;
    }

    void OnDestroy()
    {
        _blockFlashCts?.Cancel();
        _blockFlashCts?.Dispose();
    }

    bool IsBlocked()
    {
        if (_player == null || _enemy.IsDead)
            return false;

        // 적이 바라보는 방향 (flipX=false → 오른쪽)
        bool facingRight = !_sr.flipX;
        float playerX = _player.position.x;
        float enemyX = transform.position.x;

        // 플레이어가 적의 앞에 있으면 차단
        bool playerInFront = facingRight ? (playerX > enemyX) : (playerX < enemyX);

        if (playerInFront)
        {
            _blockFlashCts?.Cancel();
            _blockFlashCts?.Dispose();
            _blockFlashCts = new CancellationTokenSource();
            BlockFlash(_blockFlashCts.Token).Forget();
            AudioManager.Instance?.PlaySFX(blockSound);
            return true;
        }

        return false;
    }

    async UniTaskVoid BlockFlash(CancellationToken token)
    {
        _sr.color = new Color(0.4f, 0.6f, 1f); // 파란 빛 = 방패로 막음
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.12f), cancellationToken: token);
        _sr.color = Color.white;
    }
}
