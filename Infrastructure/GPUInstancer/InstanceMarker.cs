using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class InstanceMarker : MonoBehaviour
{
    [Tooltip("Не рисовать ближе этого расстояния (для импосторов). 0 = без ограничения")]
    public float minDistance = 0f;

    [Tooltip("Не рисовать дальше этого расстояния. 0 = без ограничения")]
    public float maxDistance = 0f;

    [Tooltip("Получать тени. Для импосторов обычно отключают")]
    public bool receiveShadows = true;

    void Start()
    {
        GetComponent<MeshRenderer>().enabled = false;
    }
}
