#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Tools > 무기 애니메이션 설정  을 실행하면
// 각 플레이어 애니메이션 클립에 Sword Transform 커브를 자동으로 추가합니다.
// 실행 후 Animation 창에서 각 키프레임 값을 스프라이트 손 위치에 맞게 미세 조정하세요.
public static class WeaponAnimationSetup
{
    const string SwordPath = "Visuals/Sword";

    // ── 클립별 키프레임 정의 ──────────────────────────────
    // (time, posX, posY, rotZ)
    // rotZ 기준: 0°=오른쪽, 90°=위쪽, -90°=아래쪽
    // 위에서 아래로 내려치는 수직 베기
    static readonly (float t, float x, float y, float z)[] AttackKeys =
    {
        (0f, 0.20f, 1.10f, 110f), // frame 0 : 머리 위로 치켜듦
        (0.083333336f, 0.55f, 0.50f, 10f), // frame 1 : 앞으로 내려치는 중 (히트)
        (0.16666667f, 0.45f, 0.00f, -70f), // frame 2 : 아래로 끝까지 내려침
        (0.25f, 0.45f, 0.00f, -70f), // 마지막 고정
    };

    static readonly (float t, float x, float y, float z)[] IdleKeys =
    {
        (0f, 0.56f, 0.77f, -49.4f),
        (0.5f, 0.56f, 0.77f, -49.4f),
    };

    static readonly (float t, float x, float y, float z)[] WalkKeys =
    {
        (0f, 0.56f, 0.77f, -49.4f),
        (0.125f, 0.56f, 0.72f, -49.4f),
        (0.25f, 0.56f, 0.77f, -49.4f),
        (0.375f, 0.56f, 0.72f, -49.4f),
        (0.5f, 0.56f, 0.77f, -49.4f),
    };

    static readonly (float t, float x, float y, float z)[] DashKeys =
    {
        (0f, 0.65f, 0.60f, 0f),
        (0.25f, 0.65f, 0.60f, 0f),
    };

    static readonly (float t, float x, float y, float z)[] JumpKeys =
    {
        (0f, 0.56f, 0.85f, -60f),
        (0.5f, 0.56f, 0.85f, -60f),
    };

    // ── 1. 커브 추가 ──────────────────────────────────────
    [MenuItem("Tools/무기 애니메이션/1. 커브 추가")]
    static void Run()
    {
        Apply("Assets/Animations/Player/Player_SwordAttack.anim", AttackKeys);
        Apply("Assets/Animations/Player/Player_Idle.anim", IdleKeys);
        Apply("Assets/Animations/Player/Player_SwordIdle.anim", IdleKeys);
        Apply("Assets/Animations/Player/Player_ArrowIdle.anim", IdleKeys);
        Apply("Assets/Animations/Player/Player_Walk.anim", WalkKeys);
        Apply("Assets/Animations/Player/Player_Dash.anim", DashKeys);
        Apply("Assets/Animations/Player/Player_Jump.anim", JumpKeys);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[WeaponAnimationSetup] 커브 추가 완료! Animation 창에서 키프레임을 미세 조정하세요."
        );
    }

    // ── 2. SwordController Animator 비활성화 ──────────────
    // PlayerController 클립이 Sword Transform을 제어하므로
    // Sword 오브젝트의 SwordController Animator는 꺼야 충돌이 없습니다.
    [MenuItem("Tools/무기 애니메이션/2. SwordController 비활성화")]
    static void DisableSwordController()
    {
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Player.prefab"
        );
        if (playerPrefab == null)
        {
            Debug.LogError("[WeaponAnimationSetup] Player.prefab을 찾을 수 없습니다.");
            return;
        }

        using var scope = new PrefabUtility.EditPrefabContentsScope("Assets/Prefabs/Player.prefab");
        var root = scope.prefabContentsRoot;
        var visuals = root.transform.Find("Visuals");
        if (visuals == null)
        {
            Debug.LogError("[WeaponAnimationSetup] Visuals 자식을 찾을 수 없습니다.");
            return;
        }

        var swordTf = visuals.Find("Sword");
        if (swordTf == null)
        {
            Debug.LogError("[WeaponAnimationSetup] Sword 자식을 찾을 수 없습니다.");
            return;
        }

        var anim = swordTf.GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = false;
            Debug.Log("[WeaponAnimationSetup] SwordController Animator 비활성화 완료.");
        }
        else
        {
            Debug.Log("[WeaponAnimationSetup] Sword에 Animator가 없거나 이미 제거됨.");
        }
    }

    // ── 내부 ──────────────────────────────────────────────
    static void Apply(string assetPath, (float t, float x, float y, float z)[] keys)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (clip == null)
        {
            Debug.LogWarning($"[WeaponAnimationSetup] 클립 없음: {assetPath}");
            return;
        }

        var posX = new AnimationCurve();
        var posY = new AnimationCurve();
        var rotZ = new AnimationCurve();

        foreach (var (t, x, y, z) in keys)
        {
            posX.AddKey(new Keyframe(t, x, 0f, 0f));
            posY.AddKey(new Keyframe(t, y, 0f, 0f));
            rotZ.AddKey(new Keyframe(t, z, 0f, 0f));
        }

        // 위치는 Constant(보간 없음) — 스프라이트처럼 딱딱 끊어지게
        SetTangents(posX, AnimationUtility.TangentMode.Constant);
        SetTangents(posY, AnimationUtility.TangentMode.Constant);
        // 회전은 Linear — 부드럽게 스윙
        SetTangents(rotZ, AnimationUtility.TangentMode.Linear);

        var bindPosX = Bind("m_LocalPosition.x");
        var bindPosY = Bind("m_LocalPosition.y");
        var bindRotZ = Bind("localEulerAnglesRaw.z");

        AnimationUtility.SetEditorCurve(clip, bindPosX, posX);
        AnimationUtility.SetEditorCurve(clip, bindPosY, posY);
        AnimationUtility.SetEditorCurve(clip, bindRotZ, rotZ);

        EditorUtility.SetDirty(clip);
        Debug.Log($"[WeaponAnimationSetup] 적용: {assetPath}");
    }

    static EditorCurveBinding Bind(string property) =>
        new EditorCurveBinding
        {
            path = SwordPath,
            type = typeof(Transform),
            propertyName = property,
        };

    static void SetTangents(AnimationCurve curve, AnimationUtility.TangentMode mode)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, mode);
            AnimationUtility.SetKeyRightTangentMode(curve, i, mode);
        }
    }

    // ── 3. Sword 회전값 동기화 ────────────────────────────
    // m_EulerCurves는 GetCurveBindings로 읽히지 않으므로
    // Sword_Attack.anim / Sword_Idle.anim 에서 직접 읽은 값을 하드코딩.
    //
    // Sword_Attack: t=0(-49.4°) t=0.07(40°) t=0.20(-130°) t=0.33(-49.4°)  총 0.33s
    // Sword_Idle:   t=0(-49.4°) t=0.5(-44.4°) t=1.0(-49.4°)               총 1.0s
    static readonly (float t, float rot)[] SwordAttackRot =
    {
        (0f, -49.414f),
        (0.07f, 40f),
        (0.20f, -130f),
        (0.33f, -49.414f),
    };

    static readonly (float t, float rot)[] SwordIdleRot =
    {
        (0f, -49.414f),
        (0.5f, -44.414f),
        (1.0f, -49.414f),
    };

    [MenuItem("Tools/무기 애니메이션/3. Sword 회전값 동기화")]
    static void SyncRotationFromSwordClips()
    {
        ApplyRotation("Assets/Animations/Player/Player_SwordAttack.anim", SwordAttackRot, 0.33f);

        string[] idleTargets =
        {
            "Assets/Animations/Player/Player_Idle.anim",
            "Assets/Animations/Player/Player_SwordIdle.anim",
            "Assets/Animations/Player/Player_ArrowIdle.anim",
            "Assets/Animations/Player/Player_Walk.anim",
        };
        foreach (var target in idleTargets)
            ApplyRotation(target, SwordIdleRot, 1.0f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WeaponAnimationSetup] Sword 회전값 동기화 완료!");
    }

    static void ApplyRotation(string dstPath, (float t, float rot)[] srcKeys, float srcLen)
    {
        var dst = AssetDatabase.LoadAssetAtPath<AnimationClip>(dstPath);
        if (dst == null)
        {
            Debug.LogWarning($"[WeaponAnimationSetup] 클립 없음: {dstPath}");
            return;
        }

        float dstLen = dst.length > 0 ? dst.length : 1f;
        float scale = dstLen / srcLen;

        var curve = new AnimationCurve();
        foreach (var (t, rot) in srcKeys)
            curve.AddKey(new Keyframe(t * scale, rot, 0f, 0f));

        SetTangents(curve, AnimationUtility.TangentMode.Linear);

        AnimationUtility.SetEditorCurve(dst, Bind("localEulerAnglesRaw.z"), curve);
        EditorUtility.SetDirty(dst);
        Debug.Log($"[WeaponAnimationSetup] 회전 적용: {System.IO.Path.GetFileName(dstPath)}");
    }
}
#endif
