using System.Collections.Generic;
using UnityEngine;

public class HitboxVisualizer : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.F1;
    public bool showHitboxes = true;

    [Header("Colors")]
    public Color playerHitColor = new Color(0f, 1f, 0f, 0.5f);
    public Color playerAttackColor = new Color(1f, 0.3f, 0f, 0.6f);
    public Color enemyHitboxColor = new Color(1f, 0f, 0f, 0.5f);
    public Color enemyDetectionColor = new Color(1f, 1f, 0f, 0.25f);
    public Color enemyAttackRangeColor = new Color(1f, 0.2f, 0.2f, 0.35f);
    public Color projectileColor = new Color(0.5f, 0f, 1f, 0.6f);
    public Color hurtboxColor = new Color(0f, 0.5f, 1f, 0.4f);

    private Material lineMat;

    static HitboxVisualizer instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showHitboxes = !showHitboxes;
    }

    void CreateMaterial()
    {
        if (lineMat != null)
            return;
        var shader = Shader.Find("Hidden/Internal-Colored");
        lineMat = new Material(shader);
        lineMat.hideFlags = HideFlags.HideAndDontSave;
        lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMat.SetInt("_ZWrite", 0);
        lineMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    void OnPostRender()
    {
        if (!showHitboxes)
            return;

        CreateMaterial();
        lineMat.SetPass(0);

        GL.PushMatrix();
        GL.LoadProjectionMatrix(Camera.current.projectionMatrix);
        GL.modelview = Camera.current.worldToCameraMatrix;

        DrawPlayerWeapons();
        DrawMeleeHitboxes();
        DrawEnemyRanges();
        DrawProjectiles();
        DrawHurtboxes();

        GL.PopMatrix();
    }

    void DrawPlayerWeapons()
    {
        foreach (var pw in FindObjectsByType<PlayerWeapon>(FindObjectsSortMode.None))
        {
            Transform root = pw.transform.root;
            Transform vis = root.Find("Visuals");
            bool facingLeft = vis != null && vis.localScale.x < 0f;
            float facing = facingLeft ? -1f : 1f;
            Vector2 origin = (Vector2)root.position;
            Vector2 tipPos = origin + new Vector2(facing * pw.attackRadius, 0.2f);

            GLDrawCircle(origin, pw.attackRadius, new Color(1f, 1f, 0f, 0.2f), 48);
            GLDrawCircleFilled(tipPos, pw.hitRadius,
                pw.Swinging ? playerAttackColor : new Color(1f, 0.5f, 0f, 0.3f), 32);
            GLDrawCircle(tipPos, pw.hitRadius,
                pw.Swinging ? Color.red : new Color(1f, 0.5f, 0f, 0.7f), 32);

            GL.Begin(GL.LINES);
            GL.Color(Color.white);
            GL.Vertex3(origin.x, origin.y, 0f);
            GL.Vertex3(tipPos.x, tipPos.y, 0f);
            GL.End();
        }
    }

    void DrawMeleeHitboxes()
    {
        foreach (var mh in FindObjectsByType<MeleeHitbox>(FindObjectsSortMode.None))
        {
            var col = mh.GetComponent<Collider2D>();
            if (col == null)
                continue;

            var b = col.bounds;
            Color c = col.enabled ? enemyHitboxColor : new Color(0.6f, 0.6f, 0.6f, 0.15f);
            GLDrawRectFilled(b.center, b.size, c);
            c.a = Mathf.Min(c.a + 0.4f, 1f);
            GLDrawRect(b.center, b.size, c);
        }
    }

    void DrawEnemyRanges()
    {
        foreach (var ec in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            Vector2 pos = ec.transform.position;
            GLDrawCircle(pos, ec.detectionRange, enemyDetectionColor, 48);
            GLDrawCircle(pos, ec.attackRange, enemyAttackRangeColor, 32);

            if (ec.isRanged)
                GLDrawCircle(pos, ec.safeDistance, new Color(0f, 1f, 1f, 0.3f), 32);
        }
    }

    void DrawProjectiles()
    {
        foreach (var p in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            var col = p.GetComponent<CircleCollider2D>();
            if (col == null)
                continue;

            Vector2 pos = (Vector2)p.transform.position + col.offset;
            float r = col.radius * Mathf.Max(p.transform.lossyScale.x, p.transform.lossyScale.y);
            GLDrawCircleFilled(pos, r, projectileColor, 24);
            GLDrawCircle(pos, r, new Color(0.7f, 0f, 1f, 0.9f), 24);
        }
    }

    void DrawHurtboxes()
    {
        foreach (var ph in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            var col = ph.GetComponent<Collider2D>();
            if (col == null)
                col = ph.GetComponentInChildren<Collider2D>();
            if (col == null)
                continue;

            var b = col.bounds;
            GLDrawRectFilled(b.center, b.size, hurtboxColor);
            GLDrawRect(b.center, b.size, new Color(0f, 0.5f, 1f, 0.8f));
        }

        foreach (var ec in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            var col = ec.GetComponent<Collider2D>();
            if (col == null || !col.enabled)
                continue;

            var b = col.bounds;
            Color c = new Color(0f, 1f, 0.5f, 0.25f);
            GLDrawRectFilled(b.center, b.size, c);
            GLDrawRect(b.center, b.size, new Color(0f, 1f, 0.5f, 0.7f));
        }
    }

    void GLDrawCircle(Vector2 center, float radius, Color color, int segments)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);
        float step = 2f * Mathf.PI / segments;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i * step;
            float a2 = (i + 1) * step;
            GL.Vertex3(center.x + Mathf.Cos(a1) * radius, center.y + Mathf.Sin(a1) * radius, 0f);
            GL.Vertex3(center.x + Mathf.Cos(a2) * radius, center.y + Mathf.Sin(a2) * radius, 0f);
        }
        GL.End();
    }

    void GLDrawCircleFilled(Vector2 center, float radius, Color color, int segments)
    {
        GL.Begin(GL.TRIANGLES);
        GL.Color(color);
        float step = 2f * Mathf.PI / segments;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i * step;
            float a2 = (i + 1) * step;
            GL.Vertex3(center.x, center.y, 0f);
            GL.Vertex3(center.x + Mathf.Cos(a1) * radius, center.y + Mathf.Sin(a1) * radius, 0f);
            GL.Vertex3(center.x + Mathf.Cos(a2) * radius, center.y + Mathf.Sin(a2) * radius, 0f);
        }
        GL.End();
    }

    void GLDrawRect(Vector2 center, Vector2 size, Color color)
    {
        Vector2 half = size * 0.5f;
        GL.Begin(GL.LINES);
        GL.Color(color);
        // bottom
        GL.Vertex3(center.x - half.x, center.y - half.y, 0f);
        GL.Vertex3(center.x + half.x, center.y - half.y, 0f);
        // right
        GL.Vertex3(center.x + half.x, center.y - half.y, 0f);
        GL.Vertex3(center.x + half.x, center.y + half.y, 0f);
        // top
        GL.Vertex3(center.x + half.x, center.y + half.y, 0f);
        GL.Vertex3(center.x - half.x, center.y + half.y, 0f);
        // left
        GL.Vertex3(center.x - half.x, center.y + half.y, 0f);
        GL.Vertex3(center.x - half.x, center.y - half.y, 0f);
        GL.End();
    }

    void GLDrawRectFilled(Vector2 center, Vector2 size, Color color)
    {
        Vector2 half = size * 0.5f;
        GL.Begin(GL.TRIANGLES);
        GL.Color(color);
        GL.Vertex3(center.x - half.x, center.y - half.y, 0f);
        GL.Vertex3(center.x + half.x, center.y - half.y, 0f);
        GL.Vertex3(center.x + half.x, center.y + half.y, 0f);

        GL.Vertex3(center.x - half.x, center.y - half.y, 0f);
        GL.Vertex3(center.x + half.x, center.y + half.y, 0f);
        GL.Vertex3(center.x - half.x, center.y + half.y, 0f);
        GL.End();
    }

    void OnGUI()
    {
        if (!showHitboxes)
            return;

        var style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        float y = 10f;
        GUI.Label(new Rect(10, y, 300, 25), $"[F1] Hitbox Visualizer: ON", style);
        y += 20f;

        style.fontSize = 11;
        style.fontStyle = FontStyle.Normal;

        DrawLegendEntry(ref y, style, playerAttackColor, "Player Attack (Swing)");
        DrawLegendEntry(ref y, style, enemyHitboxColor, "Enemy Melee Hitbox");
        DrawLegendEntry(ref y, style, enemyDetectionColor, "Enemy Detection Range");
        DrawLegendEntry(ref y, style, enemyAttackRangeColor, "Enemy Attack Range");
        DrawLegendEntry(ref y, style, projectileColor, "Projectile");
        DrawLegendEntry(ref y, style, hurtboxColor, "Player Hurtbox");
        DrawLegendEntry(ref y, style, new Color(0f, 1f, 0.5f, 0.7f), "Enemy Hurtbox");
    }

    void DrawLegendEntry(ref float y, GUIStyle style, Color color, string label)
    {
        var prevColor = GUI.color;
        GUI.color = color;
        GUI.Label(new Rect(12, y, 14, 14), "■");
        GUI.color = prevColor;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(28, y, 250, 20), label, style);
        y += 16f;
    }
}
