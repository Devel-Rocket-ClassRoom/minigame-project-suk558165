using UnityEngine;

// LateUpdate에서 실행 → Animator가 localPosition을 키프레임으로 설정한 뒤
// 마우스 방향에 따라 X를 반전시켜 왼쪽 공격도 자연스럽게 보이게 함
public class HandMirror : MonoBehaviour
{
    private Camera cam;
    private Transform player;

    void Start()
    {
        cam = Camera.main;
        player = transform.parent;
    }

    void LateUpdate()
    {
        if (cam == null || player == null)
            return;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        bool facingLeft = mouseWorld.x < player.position.x;

        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Abs(pos.x) * (facingLeft ? -1f : 1f);
        transform.localPosition = pos;
    }
}
