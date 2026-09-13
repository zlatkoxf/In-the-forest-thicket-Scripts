using UnityEngine;
using UnityEditor;
using System.IO;

public class UVTexturePainter : EditorWindow
{
    [MenuItem("Tools/UV Texture Painter")]
    static void Open()
    {
        GetWindow<UVTexturePainter>("UV Painter");
    }

    Texture2D targetTexture;
    bool brushEnabled;
    Color brushColor = Color.red;
    float brushRadius = 10f;
    LayerMask paintLayer;
    Vector2 lastUV;
    bool hasLastUV;

    void OnGUI()
    {
        EditorGUILayout.LabelField("UV Texture Painter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetTexture = (Texture2D)EditorGUILayout.ObjectField("Target Texture", targetTexture, typeof(Texture2D), false);

        EditorGUILayout.Space();
        brushColor = EditorGUILayout.ColorField("Brush Color", brushColor);
        brushRadius = EditorGUILayout.Slider("Brush Radius", brushRadius, 1f, 50f);
        paintLayer = LayerMaskField("Paint Layer", paintLayer);

        EditorGUILayout.Space();
        GUI.backgroundColor = brushEnabled ? Color.red : Color.green;
        if (GUILayout.Button(brushEnabled ? "BRUSH: ON (click to disable)" : "BRUSH: OFF (click to enable)", GUILayout.Height(30)))
        {
            brushEnabled = !brushEnabled;
            hasLastUV = false;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();
        if (targetTexture != null)
        {
            if (GUILayout.Button("Save Texture to Disk"))
                SaveTexture();
        }

        if (targetTexture == null)
        {
            EditorGUILayout.HelpBox("Assign a texture to paint on.", MessageType.Warning);
        }
        if (!brushEnabled)
        {
            EditorGUILayout.HelpBox("Enable the brush and click-drag on objects in the Scene view.", MessageType.Info);
        }
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (!brushEnabled || targetTexture == null) return;

        Event e = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlId);

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            hasLastUV = false;
            e.Use();
            return;
        }

        if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;
        if (e.button != 0) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, Mathf.Infinity, paintLayer)) return;

        MeshFilter mf = hit.collider.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Mesh mesh = mf.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        if (uvs == null || uvs.Length == 0) return;

        Vector3 localHit = hit.transform.InverseTransformPoint(hit.point);

        Vector2 uv = Vector2.zero;
        bool found = false;

        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            int[] tris = mesh.GetTriangles(s);
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v0 = vertices[tris[i]];
                Vector3 v1 = vertices[tris[i + 1]];
                Vector3 v2 = vertices[tris[i + 2]];

                Vector3 bary = Barycentric(localHit, v0, v1, v2);
                if (bary.x >= 0 && bary.y >= 0 && bary.z >= 0)
                {
                    uv = uvs[tris[i]] * bary.x + uvs[tris[i + 1]] * bary.y + uvs[tris[i + 2]] * bary.z;
                    found = true;
                    break;
                }
            }
            if (found) break;
        }

        if (!found) return;

        Color[] pixels = targetTexture.GetPixels();
        int texW = targetTexture.width;
        int texH = targetTexture.height;

        if (hasLastUV)
        {
            float dist = Vector2.Distance(lastUV, uv);
            float stepUV = brushRadius / texW;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / stepUV));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                PaintOnPixels(pixels, texW, texH, Vector2.Lerp(lastUV, uv, t));
            }
        }
        else
        {
            PaintOnPixels(pixels, texW, texH, uv);
        }

        lastUV = uv;
        hasLastUV = true;

        targetTexture.SetPixels(pixels);
        targetTexture.Apply();
        sceneView.Repaint();

        e.Use();
    }

    void PaintOnPixels(Color[] pixels, int texW, int texH, Vector2 uv)
    {
        int radius = Mathf.RoundToInt(brushRadius);
        int cx = Mathf.RoundToInt(uv.x * texW);
        int cy = Mathf.RoundToInt(uv.y * texH);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                float dist = Mathf.Sqrt(x * x + y * y);
                if (dist > brushRadius) continue;

                int px = (cx + x + texW) % texW;
                int py = (cy + y + texH) % texH;
                if (py < 0 || py >= texH) continue;

                float alpha = 1f - (dist / brushRadius);
                int idx = py * texW + px;
                pixels[idx] = Color.Lerp(pixels[idx], brushColor, alpha);
            }
        }
    }

    void SaveTexture()
    {
        string path = AssetDatabase.GetAssetPath(targetTexture);
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("Error", "Texture is not a project asset.", "OK");
            return;
        }

        byte[] bytes = targetTexture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        EditorUtility.DisplayDialog("Saved", "Texture saved to:\n" + path, "OK");
    }

    Vector3 Barycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        if (Mathf.Abs(denom) < 1e-8f) return new Vector3(-1, -1, -1);
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1f - v - w;
        return new Vector3(u, v, w);
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        brushEnabled = false;
    }

    void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        string[] layers = UnityEditorInternal.InternalEditorUtility.layers;
        int[] layerNumbers = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            layerNumbers[i] = LayerMask.NameToLayer(layers[i]);

        int maskValue = 0;
        for (int i = 0; i < layerNumbers.Length; i++)
        {
            if (((1 << layerNumbers[i]) & layerMask.value) != 0)
                maskValue |= (1 << i);
        }

        maskValue = EditorGUILayout.MaskField(label, maskValue, layers);

        int result = 0;
        for (int i = 0; i < layerNumbers.Length; i++)
        {
            if ((maskValue & (1 << i)) != 0)
                result |= (1 << layerNumbers[i]);
        }

        layerMask.value = result;
        return layerMask;
    }
}
