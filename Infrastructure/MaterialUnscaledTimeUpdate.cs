using UnityEngine;

public class MaterialUnscaledTimeUpdate : MonoBehaviour
{
    [SerializeField] private Material targetMaterial;
    private float customTime;

    void Update()
    {
        customTime += Time.unscaledDeltaTime;
        targetMaterial.SetFloat("_UnscaledTime", customTime);
    }
}
