using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// BossController 의 패턴 연출 부분 (상태/수명주기는 BossController.cs).
public partial class BossController
{
    // ── 텔 (예고 연출) ──

    IEnumerator TellFlash(Color color) =>
        EnemyUtils.TellFlash(sr, color, originalColor, tellDuration);

    IEnumerator TellShake() => EnemyUtils.TellShake(transform, tellDuration);

    // ── 패턴: 돌진 공격 (돌진 후 베기) ──

    IEnumerator ChargeAttack()
    {
        yield return TellFlash(Color.red);

        attackFlip = true;
        FlipToPlayer();
        AudioManager.Instance?.PlaySFX(dashSound);
        if (animator != null)
            animator.Play("Dash", 0, 0f);

        float dir = player.position.x > transform.position.x ? 1f : -1f;
        float elapsed = 0f;

        // 돌진: 플레이어 근처까지 이동 (데미지 없음)
        while (elapsed < chargeDuration)
        {
            transform.position += new Vector3(dir * chargeSpeed * Time.deltaTime, 0f, 0f);

            if (Vector2.Distance(transform.position, player.position) <= comboRange)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 도착 후 베기
        rb.linearVelocity = Vector2.zero;
        FlipToPlayer();
        if (animator != null)
            animator.Play("Dash", 0, 0f);

        yield return new WaitForSeconds(0.2f);
        DealAreaDamage(transform.position, comboRange);
        yield return new WaitForSeconds(0.15f);

        attackFlip = false;

        // 스턴 (반격 타이밍)
        sr.color = Color.gray;
        yield return new WaitForSeconds(chargeStunDuration);
        sr.color = originalColor;
    }

    // ── 패턴: 내려찍기 ──

    IEnumerator SlamAttack()
    {
        yield return TellShake();

        if (animator != null)
            animator.Play("Slam", 0, 0f);

        // 점프
        rb.linearVelocity = new Vector2(0f, slamJumpForce);

        // 점프 후 실제로 지면을 벗어날 때까지 대기 (바로 IsGrounded 체크하면 아직 지면 접촉 판정)
        float liftWait = 0f;
        while (IsGrounded() && liftWait < 0.3f)
        {
            liftWait += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        // 경고 표시 (바닥 전체)
        Vector3 targetPos = player.position;
        GameObject warning = null;
        if (slamWarningPrefab != null)
        {
            warning = Instantiate(slamWarningPrefab, targetPos, Quaternion.identity);
            warning.transform.localScale = new Vector3(100f, 0.3f, 1f);
        }

        yield return new WaitForSeconds(0.15f);

        // 급강하 — Rigidbody 통해 X 이동 (transform.position 직접 수정은 물리 디싱크 유발)
        rb.MovePosition(new Vector2(targetPos.x, rb.position.y));
        rb.linearVelocity = new Vector2(0f, -slamFallSpeed);

        float fallTimeout = 3f;
        float fallElapsed = 0f;
        while (!IsGrounded() && fallElapsed < fallTimeout)
        {
            rb.linearVelocity = new Vector2(0f, -slamFallSpeed);
            fallElapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;

        if (warning != null)
            Destroy(warning);

        // 착지 데미지
        AudioManager.Instance?.PlaySFX(slamSound);
        SlamGroundDamage();

        yield return new WaitForSeconds(0.25f);
    }

    // ── 패턴: 투사체 ──

    IEnumerator ProjectileAttack()
    {
        yield return TellFlash(new Color(1f, 0.5f, 0f));

        FlipToPlayer();
        if (animator != null)
            animator.Play("Charge", 0, 0f);

        if (projectilePrefab == null || player == null)
            yield break;

        AudioManager.Instance?.PlaySFX(projectileSound);
        Vector2 baseDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < projectileCount; i++)
        {
            float offset = 0f;
            if (projectileCount > 1)
                offset = Mathf.Lerp(
                    -projectileSpread / 2f,
                    projectileSpread / 2f,
                    (float)i / (projectileCount - 1)
                );

            float angle = (baseAngle + offset) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            var projComp = GetPooledProjectile();
            projComp.Pool = projPool;
            projComp.Init(dir, projectileSpeed, damage, gameObject);
        }

        yield return new WaitForSeconds(0.25f);
    }

    Projectile GetPooledProjectile()
    {
        if (projPool == null)
            projPool = new ObjectPool<Projectile>(projectilePrefab.GetComponent<Projectile>());
        return projPool.Get(transform.position, Quaternion.identity);
    }

    // ── Phase 2 패턴: 바닥 가시 (공중 이탈 후 랜덤 가시) ──

    IEnumerator SpikeStormAttack()
    {
        yield return GoAirborne(4f, 0.4f);

        const float spacing = 1.4f;
        const float halfSpan = 7f; // 플레이어 기준 좌우 범위
        const int waves = 2;
        const float warnTime = 0.45f;
        var positions = new List<Vector3>();

        for (int w = 0; w < waves; w++)
        {
            positions.Clear();
            float centerX = player != null ? player.position.x : transform.position.x;
            for (float x = centerX - halfSpan; x <= centerX + halfSpan; x += spacing)
            {
                // 랜덤하게 띄엄띄엄 생성 → 회피 가능한 안전 구간 보장
                if (Random.value > 0.55f)
                    continue;
                float floorY = EnemyUtils.FindFloorY(
                    new Vector3(x, transform.position.y, 0f),
                    groundLayer
                );
                positions.Add(new Vector3(x, floorY, 0f));
            }

            // 예고 표식
            if (warningPrefab != null)
                foreach (var p in positions)
                    Destroy(Instantiate(warningPrefab, p, Quaternion.identity), warnTime);
            yield return new WaitForSeconds(warnTime);

            // 가시 솟구침
            AudioManager.Instance?.PlaySFX(slamSound);
            if (spikePrefab != null)
                foreach (var p in positions)
                {
                    var spike = Instantiate(spikePrefab, p, Quaternion.identity);
                    spike.GetComponent<BossSpike>()?.Init(damage, gameObject);
                }

            yield return new WaitForSeconds(0.7f);
        }

        yield return ReturnFromAir(0.4f);
    }

    // ── Phase 2 패턴: 공중 마법 (플레이어 추적 낙하) ──

    IEnumerator AirMagicAttack()
    {
        yield return GoAirborne(4f, 0.4f);

        const int waves = 3;
        const float dropHeight = 8f;
        const float warnTime = 0.4f;

        for (int w = 0; w < waves; w++)
        {
            if (player == null)
                break;

            float targetX = player.position.x; // 시전 순간 위치 추적
            float floorY = EnemyUtils.FindFloorY(
                new Vector3(targetX, transform.position.y, 0f),
                groundLayer
            );

            if (warningPrefab != null)
                Destroy(
                    Instantiate(warningPrefab, new Vector3(targetX, floorY, 0f), Quaternion.identity),
                    warnTime
                );
            yield return new WaitForSeconds(warnTime);

            AudioManager.Instance?.PlaySFX(projectileSound);
            var magic = GetPooledMagic(new Vector3(targetX, floorY + dropHeight, 0f));
            magic.Pool = magicPool;
            magic.Init(Vector2.down, magicProjectileSpeed, damage, gameObject);

            yield return new WaitForSeconds(0.45f);
        }

        yield return ReturnFromAir(0.4f);
    }

    Projectile GetPooledMagic(Vector3 pos)
    {
        if (magicPool == null)
        {
            var prefab = magicProjectilePrefab != null ? magicProjectilePrefab : projectilePrefab;
            magicPool = new ObjectPool<Projectile>(prefab.GetComponent<Projectile>());
        }
        return magicPool.Get(pos, Quaternion.identity);
    }

    // ── 공중 이탈 / 복귀 (가시·공중마법 공용) ──

    IEnumerator GoAirborne(float height, float duration)
    {
        preAirbornePos = transform.position;
        untargetable = true;
        if (col != null)
            col.enabled = false;
        if (meleeHitbox != null)
            meleeHitbox.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (animator != null)
            animator.Play("Fly", 0, 0f);

        Vector3 start = transform.position;
        Vector3 target = start + Vector3.up * height;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.position = Vector3.Lerp(start, target, k);
            SetAlpha(Mathf.Lerp(1f, 0f, k));
            yield return null;
        }
        transform.position = target;
        SetAlpha(0f);
    }

    IEnumerator ReturnFromAir(float duration)
    {
        if (animator != null)
            animator.Play("Fly", 0, 0f);

        Vector3 start = transform.position;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.position = Vector3.Lerp(start, preAirbornePos, k);
            SetAlpha(Mathf.Lerp(0f, 1f, k));
            yield return null;
        }
        transform.position = preAirbornePos;
        SetAlpha(1f);
        rb.bodyType = RigidbodyType2D.Dynamic;
        if (col != null)
            col.enabled = true;
        untargetable = false;
    }

    void SetAlpha(float a)
    {
        sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, a);
    }

    // ── 패턴: 연속 베기 ──

    IEnumerator ComboAttack()
    {
        yield return TellShake();

        attackFlip = true;
        FlipToPlayer();
        AudioManager.Instance?.PlaySFX(comboSound);
        if (animator != null)
            animator.Play("Combo", 0, 0f);

        for (int i = 0; i < comboHitCount; i++)
        {
            FlipToPlayer();
            DealAreaDamage(transform.position, comboRange);

            if (i < comboHitCount - 1)
                yield return new WaitForSeconds(comboInterval);
        }

        attackFlip = false;
        yield return new WaitForSeconds(0.15f);
    }

    // ── 범위 데미지 ──

    void DealAreaDamage(Vector3 center, float radius)
    {
        if (player == null)
            return;
        if (Vector2.Distance(center, player.position) <= radius)
        {
            player.GetComponent<IDamageable>()?.TakeDamage(damage, gameObject);
            var playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                Vector2 knockDir = ((Vector2)player.position - (Vector2)center).normalized;
                playerCtrl.Knockback(knockDir * 8f);
            }
        }
    }

    void SlamGroundDamage()
    {
        if (player == null)
            return;

        float footY = col != null ? col.bounds.min.y : transform.position.y;
        float playerY = player.position.y;

        if (Mathf.Abs(playerY - footY) <= 2f)
        {
            player.GetComponent<IDamageable>()?.TakeDamage(damage, gameObject);
            var playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
                playerCtrl.Knockback(Vector2.up * 8f);
        }
    }

    bool IsGrounded() => EnemyUtils.IsGrounded(col, transform, groundLayer);
}
