using Cysharp.Threading.Tasks;
using UnityEngine;

public static class EnemyUtils
{
    public static async UniTask HitFlash(SpriteRenderer sr, Color originalColor, System.Func<bool> isDead)
    {
        sr.color = Color.red;
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.1f));
        if (!isDead())
            sr.color = originalColor;
    }

    public static async UniTask DeathBlink(SpriteRenderer sr)
    {
        for (int i = 0; i < 8; i++)
        {
            sr.enabled = !sr.enabled;
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.15f));
        }
    }

    public static async UniTask TellFlash(SpriteRenderer sr, Color color, Color originalColor, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.PingPong(elapsed * 10f, 1f);
            sr.color = Color.Lerp(originalColor, color, t);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        sr.color = originalColor;
    }

    public static async UniTask TellShake(Transform transform, float duration)
    {
        Vector3 origin = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float offsetX = Random.Range(-0.05f, 0.05f);
            transform.position = origin + new Vector3(offsetX, 0f, 0f);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        transform.position = origin;
    }

    // defaultFacesRight: 스프라이트 원본이 오른쪽을 보면 true (대부분의 적). 왼쪽을 보면 false.
    // deadband: 플레이어와 적의 x좌표 차가 이 값 미만이면 방향을 유지 — 겹쳤을 때 깜빡거림 방지.
    public static void FlipToPlayer(SpriteRenderer sr, Transform player, Transform self, bool defaultFacesRight = true, float deadband = 0.1f)
    {
        if (player == null)
            return;
        float dx = player.position.x - self.position.x;
        if (Mathf.Abs(dx) < deadband)
            return;
        bool playerOnLeft = dx < 0f;
        sr.flipX = defaultFacesRight ? playerOnLeft : !playerOnLeft;
    }

    public static bool IsGrounded(Collider2D col, Transform transform, LayerMask groundLayer)
    {
        float footY = col != null ? col.bounds.min.y : transform.position.y;
        Vector2 origin = new Vector2(transform.position.x, footY + 0.1f);
        return Physics2D.Raycast(origin, Vector2.down, 0.5f, groundLayer).collider != null;
    }

    public static float FindFloorY(Vector3 pos, LayerMask groundLayer)
    {
        var hit = Physics2D.Raycast(pos, Vector2.down, 20f, groundLayer);
        return hit.collider != null ? hit.point.y : pos.y;
    }

    public static void SpawnGoldDrops(
        GameObject prefab, Vector3 pos, LayerMask groundLayer,
        int count, int minGold, int maxGold,
        float minAngle = 30f, float maxAngle = 150f)
    {
        if (prefab == null)
            return;

        Vector3 spawnPos = pos + Vector3.up * 0.3f;
        float floorY = FindFloorY(pos, groundLayer);

        for (int i = 0; i < count; i++)
        {
            var gold = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
            var worldGold = gold.GetComponent<WorldGold>();
            if (worldGold != null)
            {
                worldGold.amount = Random.Range(minGold, maxGold + 1);
                float angle = Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;
                float force = Random.Range(3f, 6f);
                worldGold.Launch(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * force, floorY);
            }
        }
    }
}
