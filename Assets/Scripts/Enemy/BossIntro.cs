using System.Collections;
using UnityEngine;

public class BossIntro : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => IsPlaying = false;

    [Header("보스 정보")]
    [SerializeField]
    private string bossName = "보스";

    [SerializeField]
    private string bossTitle = "";

    [Header("카메라 연출")]
    [Tooltip("줌아웃 시 OrthographicSize (기본 카메라보다 크게)")]
    [SerializeField]
    private float zoomOutSize = 8f;

    [SerializeField]
    private float zoomDuration = 0.8f;

    [Header("타이밍")]
    [Tooltip("카메라 도착 후 보스 스폰까지 대기 시간")]
    [SerializeField]
    private float spawnDelay = 0.2f;

    [Tooltip("보스 스폰 후 이름 표시까지 대기 시간")]
    [SerializeField]
    private float spawnToNameDelay = 0.4f;

    [Tooltip("보스 이름 표시 시간")]
    [SerializeField]
    private float nameDisplayDuration = 1.5f;

    [Header("보스 이름 UI 프리팹")]
    [SerializeField]
    private GameObject bossNameUIPrefab;

    [Header("카메라 타겟")]
    [Tooltip("인트로 중 카메라가 비출 위치. 보통 보스 스폰 포인트 Transform을 할당.")]
    [SerializeField]
    private Transform cameraTarget;

    /// <param name="onSpawn">카메라가 보스 위치에 도착한 직후 호출 — 보스 스폰 트리거로 사용.</param>
    /// <param name="onComplete">인트로 전체 완료 후 호출.</param>
    public void Play(System.Action onSpawn, System.Action onComplete)
    {
        StartCoroutine(IntroSequence(onSpawn, onComplete));
    }

    IEnumerator IntroSequence(System.Action onSpawn, System.Action onComplete)
    {
        IsPlaying = true;
        var cam = CameraFollow.Instance;
        var playerController = PlayerRef.Controller;

        if (playerController != null)
            playerController.InputLocked = true;

        Transform originalTarget = null;
        float originalLensSize = 0f;
        Transform focusTarget = cameraTarget != null ? cameraTarget : transform;

        if (cam != null)
        {
            originalTarget = cam.target;
            originalLensSize = cam.OrthographicSize;
            cam.SetFollowTarget(focusTarget);
            yield return cam.LerpOrthographicSize(originalLensSize, zoomOutSize, zoomDuration);
        }
        else
        {
            yield return new WaitForSeconds(zoomDuration);
        }

        // 카메라가 보스 위치에 도착 — 보스 스폰
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);
        onSpawn?.Invoke();

        // 보스 등장 직후 잠깐 보여준 뒤 이름 표시
        if (spawnToNameDelay > 0f)
            yield return new WaitForSeconds(spawnToNameDelay);

        if (bossNameUIPrefab != null)
        {
            var uiGO = Instantiate(bossNameUIPrefab);
            var nameUI = uiGO.GetComponent<BossNameUI>();
            if (nameUI != null)
                nameUI.Show(bossName, bossTitle, nameDisplayDuration);
        }
        yield return new WaitForSeconds(nameDisplayDuration);

        if (cam != null)
        {
            cam.SetFollowTarget(originalTarget);
            yield return cam.LerpOrthographicSize(zoomOutSize, originalLensSize, zoomDuration);
        }
        else
        {
            yield return new WaitForSeconds(zoomDuration);
        }

        if (playerController != null)
            playerController.InputLocked = false;

        IsPlaying = false;
        onComplete?.Invoke();
    }
}
