using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Sprint;
    public bool Jump;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    public event Action OnJumped;
    public event Action<float> OnLanded;

    [Header("Components")]
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;

    [Header("Movement Speed Factor")]
    [SerializeField] private float speedFactor = 1;
    [Header("Movement Settings")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float acceleration = 30f; // �������� ������� �� �����
    [SerializeField] private float airAcceleration = 5f; // �������� �������� � �������
    [SerializeField] private float jumpForce = 2f;

    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedSprint;
    private bool _requestedJump;
    private Vector3 _moveVector = Vector3.zero;
    private float moveSpeed;

    private bool _wasGrounded;

    public void Initialize()
    {
        motor.CharacterController = this;
        cameraTarget.transform.parent = null;

        moveSpeed = walkSpeed;
    }

    private void LateUpdate()
    {
        cameraTarget.position = transform.position + new Vector3(0, 1.65f, 0);
    }

    public void UpdateInput(CharacterInput input)
    {
        _requestedRotation = input.Rotation;
        _moveVector.Set(input.Move.x, 0f, input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_moveVector, 1f);

        Vector3 horizontalForward = Vector3.ProjectOnPlane(input.Rotation * Vector3.forward, Vector3.up).normalized;
        Vector3 horizontalRight = Vector3.ProjectOnPlane(input.Rotation * Vector3.right, Vector3.up).normalized;

        _requestedMovement = horizontalForward * _requestedMovement.z + horizontalRight * _requestedMovement.x;
        _requestedSprint = input.Sprint;
        _requestedJump = _requestedJump || input.Jump;
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (FloatingPointPositionCheck()) // ��������� ������.
        {
            currentVelocity = Vector3.zero;
            motor.SetPosition(Vector3.zero);
        }

        moveSpeed = _requestedSprint ? runSpeed : walkSpeed;

        if (motor.GroundingStatus.IsStableOnGround) HandleGroundMovement(ref currentVelocity, deltaTime);
        else HandleAirMovement(ref currentVelocity, deltaTime);

        if (_requestedJump)
        {
            _requestedJump = false;
            if (motor.GroundingStatus.FoundAnyGround)
            {
                motor.ForceUnground(time: 0f);
                Vector3 jumpDirection;
                if (motor.GroundingStatus.IsStableOnGround) jumpDirection = motor.CharacterUp;
                else jumpDirection = motor.GroundingStatus.GroundNormal;
                float currentSpeedInJumpDirection = Vector3.Dot(currentVelocity, jumpDirection);
                float targetSpeedInJumpDirection = Mathf.Max(currentSpeedInJumpDirection, jumpForce * speedFactor);
                currentVelocity += jumpDirection * (targetSpeedInJumpDirection - currentSpeedInJumpDirection);
                OnJumped?.Invoke();
            }
        }
    }
    private void HandleGroundMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        var groundedMovement = motor.GetDirectionTangentToSurface
        (
            direction: _requestedMovement,
            surfaceNormal: motor.GroundingStatus.GroundNormal
        ) * _requestedMovement.magnitude;

        var targetVelocity = groundedMovement * moveSpeed * speedFactor;
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * speedFactor * deltaTime);
    }
    private void HandleAirMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        float verticalDot = Vector3.Dot(currentVelocity, motor.CharacterUp);

        Vector3 verticalVelocity = motor.CharacterUp * verticalDot;
        Vector3 currentHorizontalVelocity = currentVelocity - verticalVelocity;
        Vector3 targetHorizontalVelocity = Vector3.zero;

        if (_requestedMovement.sqrMagnitude > 0f)
        {
            Vector3 horizontalMovement = Vector3.ProjectOnPlane(_requestedMovement, motor.CharacterUp).normalized;
            targetHorizontalVelocity = horizontalMovement * _requestedMovement.magnitude * moveSpeed * speedFactor;
        }

        currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetHorizontalVelocity, airAcceleration * speedFactor * deltaTime);
        currentVelocity = currentHorizontalVelocity + verticalVelocity;
        currentVelocity += motor.CharacterUp * gravity * (speedFactor * speedFactor) * deltaTime;
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        var forward = Vector3.ProjectOnPlane
        (
            _requestedRotation * Vector3.forward,
            motor.CharacterUp
        );
        if (forward != Vector3.zero) currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
    }

#region Остальные методы KCC
    public void BeforeCharacterUpdate(float deltaTime) {}
    public void AfterCharacterUpdate(float deltaTime) {}
    public void PostGroundingUpdate(float deltaTime)
    {
        bool isGrounded = motor.GroundingStatus.IsStableOnGround;
        if (!_wasGrounded && isGrounded)
            if (!motor.GroundingStatus.SnappingPrevented)
                OnLanded?.Invoke(1f);
        _wasGrounded = isGrounded;
    }
    public bool IsColliderValidForCollisions(Collider coll) { return true; }
    private HitInfo _collisionHitInfo;
    public ref readonly HitInfo CollisionHitInfo => ref _collisionHitInfo;
    public struct HitInfo
    {
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public PhysicsMaterial physicMaterial;
        public HitInfo(Vector3 point, Vector3 normal, PhysicsMaterial material)
        {
            hitNormal = normal;
            hitPoint = point;
            physicMaterial = material;
        }
        public void Deconstruct(out Vector3 point, out Vector3 normal, out PhysicsMaterial material)
        {
            normal = hitNormal;
            point = hitPoint;
            material = physicMaterial;
        }
    }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        _collisionHitInfo = new HitInfo(hitPoint, hitNormal, hitCollider.sharedMaterial);
    }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {}
    public void OnDiscreteCollisionDetected(Collider hitCollider) {}
#endregion

    private bool FloatingPointPositionCheck()
    {
        if (Vector3.Distance(Vector3.zero, motor.transform.position) >= 10000f) return true;
        else return false;
    }

    //private Renderer _lastSurfaceRenderer;
    // private void CheckSurfaceMaterial()
    // {
    //     string materialName = "";
    //     Collider groundCollider = motor.GroundingStatus.GroundCollider;

    //     if (groundCollider != null)
    //     {
    //         _lastSurfaceRenderer = groundCollider.GetComponent<Renderer>();
    //         if (_lastSurfaceRenderer != null)
    //         {
    //             Material groundMaterial = _lastSurfaceRenderer.sharedMaterial;
    //             materialName = groundMaterial.name;
    //         }
    //         //Debug.Log($"�� �����������: ���������� ��������: {materialName}");
    //     }
    // }
    // public Renderer GetSurfaceRenderer() => _lastSurfaceRenderer;

    public bool SafeSetPosition(Vector3 targetPos)
    {
        Vector3 capsuleTop = targetPos + motor.CharacterUp * motor.Capsule.height;
        Vector3 capsuleBottom = targetPos;
        if (!Physics.CheckCapsule(capsuleTop, capsuleBottom, motor.Capsule.radius))
            { SetPosition(targetPos); return true; }
        return false;
    }
    public void SetPosition(Vector3 targetPos)
    {
        motor.ForceUnground();
        motor.SetPosition(targetPos);
    }

    public Transform GetCameraTarget() => cameraTarget;
    public Vector3 GetVelocity() => motor.Velocity;
    public CharacterGroundingReport GetGroundingStatus() => motor.GroundingStatus;

    // void OnDrawGizmos()
    // {
    //     Gizmos.DrawSphere(_collisionHitInfo.hitPoint, 0.02f);
    // }
}
