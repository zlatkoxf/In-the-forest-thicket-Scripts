using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ConeGizmos
{

    /// <summary>
    /// Рисует настраиваемый конус с помощью Gizmos.
    /// </summary>
    /// <param name="position">Позиция вершины конуса.</param>
    /// <param name="rotation">Направление конуса (конус смотрит по оси Z / Forward).</param>
    /// <param name="range">Длина (дальность) конуса.</param>
    /// <param name="angle">Полный угол конуса в градусах (от 0 до 360).</param>
    /// <param name="segments">Количество сегментов для отрисовки окружностей (плавность).</param>
    /// <param name="color">Цвет конуса. Если null — используется многоцветная схема по умолчанию.</param>
    /// <param name="fillAlpha">Альфа заливки (0 = без заливки, 1 = полностью непрозрачный).</param>
    public static void DrawCone(Vector3 position, Quaternion rotation, float range, float angle, int segments = 32, Color? color = null, float fillAlpha = 0f)
    {
        if (range <= 0 || angle <= 0) return;

        angle = Mathf.Clamp(angle, 0f, 360f);
        float halfAngle = angle * 0.5f;
        float halfAngleRad = halfAngle * Mathf.Deg2Rad;

        Vector3 forward = rotation * Vector3.forward;

        float baseRadius = range * Mathf.Sin(halfAngleRad);
        float baseDistance = range * Mathf.Cos(halfAngleRad);
        Vector3 baseCenter = position + forward * baseDistance;

        if (fillAlpha > 0f)
        {
            DrawFilledCone(position, rotation, range, halfAngle, segments, (color ?? Color.white), fillAlpha);
        }

        int arcSegments = Mathf.Max(segments, Mathf.CeilToInt(segments * halfAngle / 180f));

        DrawArc(Matrix4x4.TRS(position, rotation, Vector3.one), range, halfAngle, color ?? Color.red, arcSegments);
        DrawArc(Matrix4x4.TRS(position, rotation * Quaternion.AngleAxis(90, Vector3.forward), Vector3.one), range, halfAngle, color ?? Color.yellow, arcSegments);

        if (baseRadius > 0.001f)
        {
            DrawGizmoCircle(baseCenter, rotation, baseRadius, segments, color ?? Color.blue);
        }

        if (angle < 180f)
        {
            float midDistance = (range * 0.5f) * Mathf.Cos(halfAngleRad);
            float midRadius = (range * 0.5f) * Mathf.Sin(halfAngleRad);
            Vector3 midCenter = position + forward * midDistance;
            if (midRadius > 0.001f)
            {
                DrawGizmoCircle(midCenter, rotation, midRadius, segments, color ?? Color.green);
            }
        }
        else if (angle > 180f)
        {
            DrawGizmoCircle(position, rotation, range, segments, color ?? Color.cyan);
        }
    }

    private static void DrawFilledCone(Vector3 position, Quaternion rotation, float range, float halfAngle, int segments, Color color, float alpha)
    {
#if UNITY_EDITOR
        float halfAngleRad = halfAngle * Mathf.Deg2Rad;
        float baseRadius = range * Mathf.Sin(halfAngleRad);
        float baseDistance = range * Mathf.Cos(halfAngleRad);

        Vector3 forward = rotation * Vector3.forward;
        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;

        Vector3 baseCenter = position + forward * baseDistance;

        Vector3[] basePoints = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float theta = (i / (float)segments) * Mathf.PI * 2f;
            basePoints[i] = baseCenter
                + right * (Mathf.Cos(theta) * baseRadius)
                + up * (Mathf.Sin(theta) * baseRadius);
        }

        Handles.color = new Color(color.r, color.g, color.b, alpha);

        for (int i = 0; i < segments; i++)
        {
            Handles.DrawAAConvexPolygon(position, basePoints[i], basePoints[i + 1]);
        }

        for (int i = 0; i < segments; i++)
        {
            Handles.DrawAAConvexPolygon(baseCenter, basePoints[i + 1], basePoints[i]);
        }
#endif
    }

    private static void DrawArc(Matrix4x4 matrix, float radius, float halfAngle, Color color, int segments)
    {
        Gizmos.color = color;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = matrix;

        float radMin = -halfAngle * Mathf.Deg2Rad;
        float radMax = halfAngle * Mathf.Deg2Rad;

        Vector3 leftEdge = new Vector3(Mathf.Sin(radMin), 0, Mathf.Cos(radMin)) * radius;
        Vector3 rightEdge = new Vector3(Mathf.Sin(radMax), 0, Mathf.Cos(radMax)) * radius;

        Gizmos.DrawLine(Vector3.zero, leftEdge);
        Gizmos.DrawLine(Vector3.zero, rightEdge);

        Vector3 lastPoint = leftEdge;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentRad = Mathf.Lerp(radMin, radMax, t);
            Vector3 nextPoint = new Vector3(Mathf.Sin(currentRad), 0, Mathf.Cos(currentRad)) * radius;
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }

        Gizmos.matrix = oldMatrix;
    }

    private static void DrawGizmoCircle(Vector3 center, Quaternion rotation, float radius, int segments, Color color)
    {
        Gizmos.color = color;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);

        Vector3 lastPoint = new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float rad = (i * 360f / segments) * Mathf.Deg2Rad;
            Vector3 nextPoint = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0);
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }

        Gizmos.matrix = oldMatrix;
    }
}
