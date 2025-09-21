using UnityEngine;
using KinematicCharacterController;

public struct CharacterInput 
{
    public Quaternion Rotation;
    public Vector2 Movement;
    public bool Jump;
    public bool Crouch;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform camerTarget;
    [Space]
    [SerializeField] private float walkSpeed = 10f;
    [Space]
    [SerializeField] private float jumpSpeed = 20f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float crouchSpeed = 5f;


    private Quaternion requestedRotation;
    private Vector3 requestedMovement;
    private bool requestedJump;

    public void Initialize()
    {
        motor.CharacterController = this;
    }

    public void UpdateInput (CharacterInput input)
    {
        requestedRotation = input.Rotation;
        //Take 2D input vector and creates 3D movement Vector on XZ Plane
        requestedMovement = new Vector3 (input.Movement.x, 0f, input.Movement.y);
        //Clamps Diagonal movemnt to 1 to stop fast movement
        requestedMovement = Vector3.ClampMagnitude (requestedMovement, 1f);
        //Orentate input to be relative to camera direction
        requestedMovement = input.Rotation * requestedMovement;

        requestedJump = requestedJump || input.Jump;
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (motor.GroundingStatus.IsStableOnGround)
        {
            var groundedMovement = motor.GetDirectionTangentToSurface
        (
            direction: requestedMovement,
            surfaceNormal: motor.GroundingStatus.GroundNormal
        ) * requestedMovement.magnitude;

            currentVelocity = groundedMovement * walkSpeed;
        }
        else
        {
            currentVelocity += motor.CharacterUp * gravity * deltaTime;
        }

        if (requestedJump)
        {
            requestedJump = false;
            //Unstick from ground
            motor.ForceUnground(time: 0f);
            //Set Minimum vertical Speed to jumpspeed
            var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
            //Add dif in current and target vertical Speed to character jump
            currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
        }
    }
    
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) 
    {
        var forward = Vector3.ProjectOnPlane
            (
                requestedRotation * Vector3.forward,
                motor.CharacterUp
            );
        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward,motor.CharacterUp);
    }
    
    public void BeforeCharacterUpdate(float deltaTime)
    {

    }

    public void PostGroundingUpdate(float deltaTime)
    {


    }

    public void AfterCharacterUpdate(float deltaTime)
    {

    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    { 
    
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {

    }

    public bool IsColliderValidForCollisions(Collider coll) => true;

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {

    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) 
    { 
    
    }
    public Transform GetCameraTarget() => camerTarget;
}
