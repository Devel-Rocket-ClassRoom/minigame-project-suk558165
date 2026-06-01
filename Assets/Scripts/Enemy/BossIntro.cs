using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class BossIntro : MonoBehaviour
{
    [Header("보스 정보")]
    [SerializeField] private string bossName = "보스";
    [SerializeField] private string bossTitle = "";

    [Header("카메라 연출")]
    [Tooltip("줌아웃 시 OrthographicSize (기본 카메라보다 크게)")]
    [SerializeField] private float zoomOutSize = 8f;
    [SerializeField] private float zoomDuration = 0.8f;

    [Header("타이밍")]
    [Tooltip("보스 이름 표시 시간")]
    [SerializeField] private float nameDisplayDuration = 1.5f;

    [Header("보스 이름 UI 프리팹")]
    [SerializeField] private GameObject bossNameUIPrefab;

    private CinemachineCamera cinemachineCamera;
    private Transform originalFollow;
    private float originalLensSize;

    public void Play(System.Action onComplete)
    {
        StartCoroutine(IntroSequence(onComplete));
    }

    IEnumerator IntroSequence(System.Action onComplete)
    {
        cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();

        var player = GameObject.FindGameObjectWithTag("Player");
        var playerController = player != null ? player.GetComponent<PlayerController>() : null;

        if (playerController != null)
            playerController.InputLocked = true;

        if (cinemachineCamera != null)
        {
            originalFollow = cinemachineCamera.Follow;
            originalLensSize = cinemachineCamera.Lens.OrthographicSize;

            cinemachineCamera.Follow = transform;
            yield return LerpLensSize(originalLensSize, zoomOutSize, zoomDuration);
        }

        if (bossNameUIPrefab != null)
        {
            var uiGO = Instantiate(bossNameUIPrefab);
            var nameUI = uiGO.GetComponent<BossNameUI>();
            if (nameUI != null)
                nameUI.Show(bossName, bossTitle, nameDisplayDuration);
            yield return new WaitForSeconds(nameDisplayDuration);
        }

        if (cinemachineCamera != null)
        {
            cinemachineCamera.Follow = originalFollow;
            yield return LerpLensSize(zoomOutSize, originalLensSize, zoomDuration);
        }

        if (playerController != null)
            playerController.InputLocked = false;

        onComplete?.Invoke();
    }

    IEnumerator LerpLensSize(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            cinemachineCamera.Lens.OrthographicSize = Mathf.Lerp(from, to, t);
            yield return null;
        }
        cinemachineCamera.Lens.OrthographicSize = to;
    }
}
