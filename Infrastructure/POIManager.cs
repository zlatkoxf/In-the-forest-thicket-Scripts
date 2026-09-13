using System.Collections.Generic;
using UnityEngine;

public class POIManager : MonoBehaviour
{
    [SerializeField] private List<POIPoint> _pOIPoints = new List<POIPoint>();
    [System.Serializable]
    public struct POIPoint
    {
        public string key;
        public Transform transform;
    }

    private Dictionary<string, Transform> pOIPointsDict = new Dictionary<string, Transform>();
    private HashSet<string> unvisitedPoints;

    public static POIManager Instance { get; private set; }

    void Start()
    {
        if (Instance != null && Instance != this)
        { Destroy(gameObject); return; }
        Instance = this;

        BuildPOIPointsDictionary();
        unvisitedPoints = new HashSet<string>(pOIPointsDict.Keys);
        //foreach (var key in pOIPointsDict.Keys) unvisitedPoints.Add(key);
    }

    public Dictionary<string, Transform> GetComingUnvisitedPoints(Vector3 target, float distance)
    {
        Dictionary<string, Transform> unvisitedDict = new Dictionary<string, Transform>();
        foreach (string key in unvisitedPoints)
            if (pOIPointsDict.TryGetValue(key, out Transform poiTransform))
                unvisitedDict.Add(key, poiTransform);
        return GetComingPoints(unvisitedDict, target, distance);
    }

    private void BuildPOIPointsDictionary()
    {
        pOIPointsDict.Clear();
        foreach (var item in _pOIPoints)
        if (!string.IsNullOrEmpty(item.key) && item.transform != null && !pOIPointsDict.ContainsKey(item.key))
            pOIPointsDict.Add(item.key, item.transform);
    }

    public void UpdateVisitedPoints(Vector3 target, float distance)
    {
        List<string> pointsToIdetify = new List<string>();
        foreach (string key in unvisitedPoints)
        {   Transform waypointTransform = pOIPointsDict[key];
            float distanceSqr = (target - waypointTransform.position).sqrMagnitude;
            if (distanceSqr <= distance * distance) pointsToIdetify.Add(key); }
        foreach (string key in pointsToIdetify)
        {
            unvisitedPoints.Remove(key);
            Debug.Log("[POIManager] Игрок посетил точку " + key);
        }
    }

    public Dictionary<string, Transform> GetComingPoints(Dictionary<string, Transform> pointsDict, Vector3 target, float distance)
    {
        Dictionary<string, Transform> result = new Dictionary<string, Transform>();
        foreach (var dict in pointsDict)
        {   float distanceSqr = (target - dict.Value.position).sqrMagnitude;
            if (distanceSqr <= distance * distance)
                result.Add(dict.Key, dict.Value); }
        return result;
    }

    public List<Vector3> GetAllPOIPositions()
    {
        List<Vector3> result = new List<Vector3>();
        foreach (var dict in pOIPointsDict) result.Add(dict.Value.position);
        return result;
    }
    public Vector3? GetPOIPosition(string key)
    {
        if (pOIPointsDict.TryGetValue(key, out Transform transform))
            return transform.position;
        Debug.LogError($"POI position with key '{key}' not found!");
        return null;
    }
}
