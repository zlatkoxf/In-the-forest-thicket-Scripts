using UnityEngine;

public class IgnoreOcclusionCulling : MonoBehaviour
{
    void Start() {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f); // Огромный радиус
    }
}
