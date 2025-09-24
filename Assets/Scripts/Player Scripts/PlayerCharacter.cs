using UnityEngine;
using KinematicCharacterController;

public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch
}

public struct CharacterInput 
{
    public Quaternion Rotation;
    public Vector2 Movement;
    public bool Jump;
    public bool JumpSustain;
    public CrouchInput Crouch;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [SerializeField] private Transform cameraTarget;
    [Space]
    [SerializeField] private float walkSpeed = 10f;
    [Space]
    [SerializeField] private float jumpSpeed = 20f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float walkResponse = 25f;
    [SerializeField] private float crouchResponse = 20f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [Range(0f, 1f)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;

    private Stance stance;
    private Quaternion requestedRotation;
    private Vector3 requestedMovement;
    private bool requestedJump;
    private bool requestedSustainedJump;
    private bool requestedCrouch;

    private Collider[] uncrouchOverlapResults;

    public void Initialize()
    {
        stance = Stance.Stand;
        uncrouchOverlapResults = new Collider[8];

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
        requestedSustainedJump = input.JumpSustain;

        requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !requestedCrouch,
            CrouchInput.None => requestedCrouch,
            _ => requestedCrouch
        };
    }

    public void UpdateBody(float deltaTime)
    {
        var currentHeight = motor.Capsule.height;
        var normalizedHeight = currentHeight / standHeight;
        var cameraTargetHeight = currentHeight * (stance is Stance.Stand ? standCameraTargetHeight : crouchCameraTargetHeight);

        var rootTargetScale = new Vector3(1f, normalizedHeight, 1f);

        //Original - cameraTarget.localPosition = new Vector3(0f, cameraTargetHeight, 0f);
        cameraTarget.localPosition = Vector3.Lerp
            (
            a: cameraTarget.localPosition, 
            b: new Vector3(0f, cameraTargetHeight, 0f), 
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
            );

        //Original - root.localScale = rootTargetScale;
        root.localScale = Vector3.Lerp
            (
            a: root.localScale,
            b: rootTargetScale,
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
            );
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

            //Calculate the speed and responsiveness of movement based on the character's stance
            var speed = stance is Stance.Stand ? walkSpeed : crouchSpeed;
            var response = stance is Stance.Stand ? walkResponse : crouchResponse;

            //Smoothly move along the ground
            var targetVelocity = groundedMovement * speed;
            currentVelocity = Vector3.Lerp(a: currentVelocity, b: targetVelocity, t: 1f - Mathf.Exp(-response * deltaTime));
        }
        else
        {
            //Original - currentVelocity += motor.CharacterUp * gravity * deltaTime;
            //Gravity
            var effectiveGravity = gravity;
            if (requestedSustainedJump) 
            { 
                effectiveGravity*= jumpSustainGravity; 
            }
            currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;
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
        //Crouching
        if(requestedCrouch && stance is Stance.Stand)
        {
            stance = Stance.Crouch;
            motor.SetCapsuleDimensions
                (
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset: crouchHeight * 0.5f
                );
        }
    }

    public void PostGroundingUpdate(float deltaTime)
    {


    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        //Not Crouching
        if (!requestedCrouch && stance is not Stance.Stand)
        {
            stance = Stance.Stand;
            motor.SetCapsuleDimensions
                (
                radius: motor.Capsule.radius,
                height: standHeight,
                yOffset: standHeight * 0.5f
                );

            //Seeing if the capsule overlaps with any colliders before allowing them to stand up
            var pos = motor.TransientPosition;
            var rot = motor.TransientRotation;
            var mask = motor.CollidableLayers;
            if(motor.CharacterOverlap(pos, rot,uncrouchOverlapResults, mask, QueryTriggerInteraction.Ignore) > 0)
            {
                //Crouch again
                requestedCrouch = true;
                motor.SetCapsuleDimensions(radius: motor.Capsule.radius, height: crouchHeight, yOffset: crouchHeight * 0.5f);
            }
            else
            {
                stance = Stance.Stand;
            }

        }
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
    public Transform GetCameraTarget() => cameraTarget;
}
