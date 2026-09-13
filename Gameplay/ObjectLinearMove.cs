using UnityEngine;

public class ObjectLinearMove : MonoBehaviour
{
    [SerializeField] private bool _useUnscaledTime = false;
    [Space]
    [SerializeField] private Transform _targetTransform;
    [SerializeField] private bool _updateTargetPosition = true;
    [SerializeField] private Vector3 _targetPosition = Vector3.zero;
    [SerializeField] private float _moveSpeed = 8f;

    public Transform TargetTransform{ get => _targetTransform; set => _targetTransform = value; }
    public Vector3 TargetPosition{ get => _targetPosition; set => _targetPosition = value; }
    public float MoveSpeed{ get => _moveSpeed; set => _moveSpeed = value; }

    void Update()
    {
        if (_targetTransform != null && _updateTargetPosition) _targetPosition = _targetTransform.position;
        if (transform.position == _targetPosition || _targetPosition == Vector3.zero) return;
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _moveSpeed * (_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime));
    }

    void OnDrawGizmos()
    {
        Vector3 pos = transform.position;
        Gizmos.color = Color.greenYellow;
        Gizmos.DrawSphere(pos, 0.1f);
        Gizmos.DrawLine(pos, _targetPosition);
        Gizmos.DrawSphere(_targetPosition, 0.1f);
    }
}
