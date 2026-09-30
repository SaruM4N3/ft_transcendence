using System.Collections;
using UnityEngine;

// Red ground warning shown during an enemy's attack windup; procedurally drawn as a wedge matching the actual hit check.
public class AttackTelegraph : MonoBehaviour
{
    private const int Segments = 20;
    private const string GroundSortingLayer = "Ground";
    private const float FadeInFraction = 0.25f;
    private const float PulseAmplitude = 0.12f;
    private const float PulseSpeed = 14f;
    private static readonly Color FillColor = new Color(1f, 0.15f, 0.1f, 0.32f);
    private static readonly Color EdgeColor = new Color(1f, 0.2f, 0.15f, 0.9f);

    private static Material cachedMaterial;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Color[] baseColors;

    // Parented to the enemy (if given) so it tracks any drift during the windup, instead of staying frozen in world space.
    public static void Show(Transform anchor, Vector3 position, Vector2 direction, float coneAngleDegrees, float radius, float duration)
    {
        if (radius <= 0f || duration <= 0f || direction.sqrMagnitude < 0.0001f)
            return;

        var go = new GameObject("AttackTelegraph", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.position = position;
        if (anchor != null)
            go.transform.SetParent(anchor, worldPositionStays: true);

        AttackTelegraph telegraph = go.AddComponent<AttackTelegraph>();
        telegraph.Init(direction, coneAngleDegrees, radius, duration);
    }

    private void Init(Vector2 direction, float coneAngleDegrees, float radius, float duration)
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = GetOrBuildMaterial();
        meshRenderer.sortingLayerName = GroundSortingLayer;

        meshFilter.mesh = BuildWedgeMesh(direction, coneAngleDegrees, radius, out baseColors);
        StartCoroutine(Animate(duration));
    }

    private IEnumerator Animate(float duration)
    {
        Mesh mesh = meshFilter.mesh;
        Color[] colors = new Color[baseColors.Length];
        float fadeInDuration = duration * FadeInFraction;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float fadeIn = fadeInDuration > 0f ? Mathf.Clamp01(t / fadeInDuration) : 1f;
            float pulse = 1f + Mathf.Sin(t * PulseSpeed) * PulseAmplitude;
            float alphaMul = fadeIn * pulse;
            for (int i = 0; i < colors.Length; i++)
            {
                Color baseColor = baseColors[i];
                colors[i] = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alphaMul);
            }
            mesh.colors = colors;
            yield return null;
        }
        Destroy(gameObject);
    }

    // Apex at the enemy's position, fanning out to an arc at radius - the same wedge ResolveHit checks against.
    private static Mesh BuildWedgeMesh(Vector2 direction, float coneAngleDegrees, float radius, out Color[] colors)
    {
        float halfAngleRad = coneAngleDegrees * 0.5f * Mathf.Deg2Rad;
        float centerAngleRad = Mathf.Atan2(direction.y, direction.x);

        var vertices = new Vector3[Segments + 2];
        colors = new Color[Segments + 2];
        vertices[0] = Vector3.zero;
        colors[0] = FillColor;

        for (int i = 0; i <= Segments; i++)
        {
            float lerp = (float)i / Segments;
            float angle = centerAngleRad - halfAngleRad + lerp * (2f * halfAngleRad);
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            colors[i + 1] = EdgeColor;
        }

        var triangles = new int[Segments * 3];
        for (int i = 0; i < Segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        var mesh = new Mesh { name = "AttackTelegraphWedge" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetColors(colors);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material GetOrBuildMaterial()
    {
        if (cachedMaterial != null)
            return cachedMaterial;

        cachedMaterial = new Material(Shader.Find("Sprites/Default"));
        return cachedMaterial;
    }
}
