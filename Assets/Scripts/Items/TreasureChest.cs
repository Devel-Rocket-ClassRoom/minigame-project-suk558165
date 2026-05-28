using System.Collections;
using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    [Header("Reward")]
    public int goldMin = 20;
    public int goldMax = 40;
    public GameObject goldDropPrefab;

    [Header("Launch")]
    public int coinCount = 5;
    public float launchForceMin = 3f;
    public float launchForceMax = 6f;
    public float launchAngleMin = 60f;
    public float launchAngleMax = 120f;
    public float launchDuration = 0.5f;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.A;
    public float interactRange = 1.5f;

    [Header("UI")]
    public GameObject hintObject;

    [Header("Animation")]
    // Animator가 없을 때 사용되는 코드 애니메이션 총 재생 시간
    public float builtinAnimDuration = 0.8f;

    // Animator가 있을 때 Open 트리거 후 대기 시간 (애니메이션 클립 길이에 맞게 설정)
    public float animatorOpenDuration = 0.5f;

    private bool opened;
    private Transform player;
    private Animator animator;
    private Vector3 originScale;
    private Vector3 originPos;

    void Start()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            player = playerGO.transform;

        animator = GetComponent<Animator>();
        originScale = transform.localScale;
        originPos = transform.position;

        if (hintObject != null)
            hintObject.SetActive(false);
    }

    void Update()
    {
        if (opened || player == null)
            return;

        bool inRange = Vector2.Distance(transform.position, player.position) <= interactRange;

        if (hintObject != null)
            hintObject.SetActive(inRange);

        if (inRange && Input.GetKeyDown(interactKey))
            Open();
    }

    void Open()
    {
        opened = true;

        if (hintObject != null)
            hintObject.SetActive(false);

        SpawnGoldCoins();

        if (animator != null)
            StartCoroutine(AnimatorOpenRoutine());
        else
            StartCoroutine(BuiltinOpenRoutine());
    }

    // Animator 보유 시: Open 트리거 → 클립 재생 대기 → 파괴
    IEnumerator AnimatorOpenRoutine()
    {
        animator.SetTrigger("Open");
        yield return new WaitForSeconds(animatorOpenDuration);
        Destroy(gameObject);
    }

    // Animator 없을 때: 바운스 → 셰이크 → 축소 소멸
    IEnumerator BuiltinOpenRoutine()
    {
        // 1. 위로 튀어오르기
        yield return MoveLocal(originPos, originPos + Vector3.up * 0.35f, 0.12f);
        yield return MoveLocal(transform.position, originPos, 0.08f);

        // 2. 찌그러짐 (squash & stretch)
        yield return ScaleTo(
            new Vector3(originScale.x * 1.35f, originScale.y * 0.65f, originScale.z),
            0.07f
        );
        yield return ScaleTo(
            new Vector3(originScale.x * 0.75f, originScale.y * 1.35f, originScale.z),
            0.07f
        );
        yield return ScaleTo(originScale, 0.06f);

        // 3. 셰이크
        float shakeTime = 0.25f;
        float elapsed = 0f;
        float intensity = 0.08f;
        while (elapsed < shakeTime)
        {
            float progress = elapsed / shakeTime;
            float offset = Mathf.Lerp(intensity, 0f, progress);
            transform.position = originPos + (Vector3)(Random.insideUnitCircle * offset);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originPos;

        // 4. 축소되며 소멸
        yield return ScaleTo(Vector3.zero, 0.2f);

        Destroy(gameObject);
    }

    IEnumerator MoveLocal(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        transform.position = to;
    }

    void SpawnGoldCoins()
    {
        if (goldDropPrefab == null)
            return;

        int totalGold = Random.Range(goldMin, goldMax + 1);
        int perCoin = Mathf.Max(1, totalGold / coinCount);
        int remainder = totalGold - perCoin * coinCount;

        float floorY = transform.position.y;
        Vector3 spawnPos = transform.position + Vector3.up * 0.3f;

        for (int i = 0; i < coinCount; i++)
        {
            var go = Instantiate(goldDropPrefab, spawnPos, Quaternion.identity);
            var wg = go.GetComponent<WorldGold>();
            if (wg == null)
                continue;

            wg.amount = perCoin + (i == 0 ? remainder : 0);

            float angle = Random.Range(launchAngleMin, launchAngleMax);
            float force = Random.Range(launchForceMin, launchForceMax);
            float rad = angle * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            wg.Launch(dir * force, floorY);
        }
    }

    IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = transform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        transform.localScale = target;
    }
}
