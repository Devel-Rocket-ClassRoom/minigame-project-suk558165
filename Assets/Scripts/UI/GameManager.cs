using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Quit();

#if UNITY_EDITOR
        // ── 디버그 단축키 (에디터 전용) ──────────────────────
        // F1 : 현재 방 즉시 클리어 → 다음 방으로 이동
        if (Input.GetKeyDown(KeyCode.F1))
        {
            var rm = FindAnyObjectByType<RoomManager>();
            if (rm != null)
            {
                Debug.Log(
                    $"[Debug] 방 스킵: Room {rm.CurrentRoomNumber} → {rm.CurrentRoomNumber + 1}"
                );
                rm.GoToNextRoom();
            }
        }
#endif
    }

    void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
