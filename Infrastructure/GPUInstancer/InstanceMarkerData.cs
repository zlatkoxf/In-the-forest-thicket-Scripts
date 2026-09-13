using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InstanceMarkerData", menuName = "GPUInstancer/Marker Data")]
public class InstanceMarkerData : ScriptableObject
{
    [System.Serializable]
    public struct MarkerEntry
    {
        public string gameObjectName;
        public Mesh mesh;
        public Material material;
        public Matrix4x4 transform;
        public float minDistance;
        public float maxDistance;
        public bool receiveShadows;
    }

    public List<MarkerEntry> entries = new List<MarkerEntry>();
}
