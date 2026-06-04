using UnityEngine;

// 웨이브 트리거 존에 부착돼 플레이어 진입을 SpawnManager로 포워딩한다.
// 트리거 존 자신은 충돌을 발생시키지 않으며(IsTrigger=true), 같은 GameObject의
// 모든 Collider2D는 트리거로 강제 변환된다.
public class WaveTriggerForwarder : MonoBehaviour
{
    public SpawnManager target;

    void Awake()
    {
        foreach (var col in GetComponents<Collider2D>())
            col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (target != null)
            target.NotifyWaveTrigger(other);
    }
}
