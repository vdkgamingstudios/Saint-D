using UnityEngine;
using KinematicCharacterController;

public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch, Slide
}

public struct CharacterState
{
    public bool Grounded;
    public Stance Stance;
    public Vector3 Velocity;
    public Vector3 Acceleration;
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
    [SerializeField] private float coyoteTime = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float slideStartSpeed = 25f;
    [SerializeField] private float slideEndSpeed = 15f;
    [SerializeField] private float slideFriction = 0.8f;
    [SerializeField] private float slideSteerAcceleration = 5f; 
    [SerializeField] private float slideGravity = -90f;
    [Space]
    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float walkResponse = 25f;
    [SerializeField] private float crouchResponse = 20f;
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 70f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [Range(0f, 1f)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;

    private CharacterState state;
    private CharacterState lastState;
    private CharacterState tempState;

    private Stance stance;
    private Quaternion requestedRotation;
    private Vector3 requestedMovement;
    private bool requestedJump;
    private bool requestedSustainedJump;
    private bool requestedCrouch;
    private bool requestedCrouchInAir;

    private float timeSinceUngrounded;
    private float timeSinceJumpRequest;
    private bool ungroundedDueToJump;

    private Collider[] uncrouchOverlapResults;

    public void Initialize()
    {
        state.Stance = Stance.Stand;
        lastState = state;
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

        var wasRequestingJump = requestedJump;
        requestedJump = requestedJump || input.Jump;

        if(requestedJump && !wasRequestingJump)
        {
            timeSinceJumpRequest = 0f;
        }
        requestedSustainedJump = input.JumpSustain;

        var wasRequestingCrouch = requestedCrouch;

        requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !requestedCrouch,
            CrouchInput.None => requestedCrouch,
            _ => requestedCrouch
        };

        if(requestedCrouch && !wasRequestingCrouch)
        {
            requestedCrouchInAir = !state.Grounded;
        }
        else if(!requestedCrouch && wasRequestingCrouch)
        {
            requestedCrouchInAir = false;
        }
    }

    public void UpdateBody(float deltaTime)
    {
        var currentHeight = motor.Capsule.height;
        var normalizedHeight = currentHeight / standHeight;
        var cameraTargetHeight = currentHeight * (state.Stance is Stance.Stand ? standCameraTargetHeight : crouchCameraTargetHeight);

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
        state.Acceleration = Vector3.zero;

        if (motor.GroundingStatus.IsStableOnGround)
        {
            timeSinceUngrounded = 0f;
            ungroundedDueToJump = false;

            //Snaps the requested movement direction to the angle of the surface that the character is currently working on.
            var groundedMovement = motor.GetDirectionTangentToSurface
            (
                direction: requestedMovement,
                surfaceNormal: motor.GroundingStatus.GroundNormal
            ) * requestedMovement.magnitude;

            //Start Sliding
            {
                var moving = groundedMovement.sqrMagnitude > 0f;
                var crouching = state.Stance is Stance.Crouch;
                var wasStanding = lastState.Stance is Stance.Stand;
                var wasInAir = !lastState.Grounded;
                if (moving && crouching && (wasStanding || wasInAir))
                {
                    state.Stance = Stance.Slide;

                    //When landing on stable ground the character motor projects the velocity onto a flat ground plane as per KinematicCharacterMotor.HandleVelocityProjection()
                    //Reproject the last frames (falling) velocity onto the ground normal to slide
                    if (wasInAir)
                    {
                        currentVelocity = Vector3.ProjectOnPlane(vector: lastState.Velocity, planeNormal: motor.GroundingStatus.GroundNormal);
                    }

                    var effectiveSlideStartSpeed = slideStartSpeed;
                    if (!lastState.Grounded && !requestedCrouchInAir)
                    {
                        effectiveSlideStartSpeed = 0f;
                        requestedCrouchInAir = false;
                    }
                    var slideSpeed = Mathf.Max(effectiveSlideStartSpeed, currentVelocity.magnitude);
                    currentVelocity = motor.GetDirectionTangentToSurface(direction: currentVelocity, surfaceNormal: motor.GroundingStatus.GroundNormal) * slideSpeed;
                }
            }

            //Move
            if(state.Stance is Stance.Stand or Stance.Crouch) 
            {
                //Calculate the speed and responsiveness of movement based on the character's stance
                var speed = state.Stance is Stance.Stand ? walkSpeed : crouchSpeed;
                var response = state.Stance is Stance.Stand ? walkResponse : crouchResponse;

                //Smoothly move along the ground
                var targetVelocity = groundedMovement * speed;
                var moveVelocity = Vector3.Lerp(a: currentVelocity, b: targetVelocity, t: 1f - Mathf.Exp(-response * deltaTime));
                state.Acceleration = moveVelocity - currentVelocity;
                currentVelocity = moveVelocity;
            }
            //Sliding Continued.
            else
            {
                //Friction
                currentVelocity -= currentVelocity * (slideFriction * deltaTime);

                //Slope
                {
                    var force = Vector3.ProjectOnPlane(vector: -motor.CharacterUp, planeNormal: motor.GroundingStatus.GroundNormal) * slideGravity;

                    currentVelocity -= force * deltaTime;
                }

                //Steering.
                {
                    //Target velocity is the player's movement direction at the current speed.
                    var currentSpeed = currentVelocity.magnitude;
                    var targetVelocity = groundedMovement * currentSpeed;
                    var steerVelocity = currentVelocity;
                    var steerForce = (targetVelocity - steerVelocity) * slideSteerAcceleration * deltaTime;

                    //Adding steer force and clamping speed to prevent slide speed from increasing due to direct movement input
                    steerVelocity += steerForce;
                    steerVelocity = Vector3.ClampMagnitude(steerVelocity, currentSpeed);

                    state.Acceleration = (steerVelocity - currentVelocity) / deltaTime;
                    currentVelocity = steerVelocity;
                }

                //Stop
                if(currentVelocity.magnitude < slideEndSpeed)
                {
                    state.Stance = Stance.Crouch;
                }
            }
        }
        else
        {
            timeSinceUngrounded += deltaTime;

            //Move
            if (requestedMovement.sqrMagnitude > 0f) 
            {
                //Requested movement projected onto movement plane. Magnitude preserved. 
                var planarMovement = Vector3.ProjectOnPlane(vector: requestedMovement, planeNormal: motor.CharacterUp) * requestedMovement.magnitude;

                //Current velocity on movement plane.
                var currentPlanarVelocity = Vector3.ProjectOnPlane(vector: currentVelocity, planeNormal: motor.CharacterUp);

                //Calculate the force of movement.
                var movementForce = planarMovement * airAcceleration * deltaTime;

                //If moving slower than the max air speed, treat movement force as a simple steering force
                if (currentPlanarVelocity.magnitude < airSpeed)
                {
                    //Add it to the current planar velocity for a target velocity.
                    var targetPlanarVelocity = currentPlanarVelocity + movementForce;

                    //Limit target velocity to air speed.
                    targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);

                    //Steer towards target velocity.
                    movementForce = targetPlanarVelocity - currentPlanarVelocity;
                }
                //Nerf the movement force when it is in the direction of the current planar velocity to prevent accelerating further beyong the max air speed.
                else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0f) 
                {
                    //Project movement force onto the plane whose normal is the current planar velocity.
                    var constrainedMovementForce = Vector3.ProjectOnPlane(vector: movementForce, planeNormal: currentPlanarVelocity.normalized);

                    movementForce = constrainedMovementForce;
                }

                //Prevent air-climbing steep slopes
                if (motor.GroundingStatus.FoundAnyGround)
                {
                    //If moving in the same direction as the result of velocity.
                    if(Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                    {
                        //calculate obstruction normal.
                        var obstructionNormal = Vector3.Cross(motor.CharacterUp, motor.GroundingStatus.GroundNormal).normalized;

                        //Project movement force onto obstruction plane
                        movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal);
                    }
                }

                currentVelocity += movementForce;
            }

            //Original - currentVelocity += motor.CharacterUp * gravity * deltaTime;
            //Gravity
            var effectiveGravity = gravity;
            var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            if (requestedSustainedJump && verticalSpeed > 0f) 
            { 
                effectiveGravity *= jumpSustainGravity; 
            }
            currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;
        }

        if (requestedJump)
        {
            var grounded = motor.GroundingStatus.IsStableOnGround;
            var canCoyoteJump = timeSinceUngrounded < coyoteTime;

            if (grounded || canCoyoteJump)
            {
                //Refresh jump and crouch stances.
                requestedJump = false;
                requestedCrouch = false;
                requestedCrouchInAir = false;

                //Unstick from ground
                motor.ForceUnground(time: 0f);
                ungroundedDueToJump = true;
                //Set Minimum vertical Speed to jumpspeed
                var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
                //Add dif in current and target vertical Speed to character jump
                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
            }
            else
            {
                timeSinceJumpRequest += deltaTime;

                //Defers the jump request untl coyote time has passed
                var canJumpLater = timeSinceJumpRequest < coyoteTime;
                requestedJump = canJumpLater;
            }
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
        tempState = state;
        //Crouching
        if(requestedCrouch && state.Stance is Stance.Stand)
        {
            state.Stance = Stance.Crouch;
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
        if (!motor.GroundingStatus.IsStableOnGround && state.Stance is Stance.Slide) 
        {
            state.Stance = Stance.Crouch;
        }
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        //Not Crouching
        if (!requestedCrouch && state.Stance is not Stance.Stand)
        {
            state.Stance = Stance.Stand;
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
                state.Stance = Stance.Stand;
            }

        }

        //Update state to reflect relevant motor properties.
        state.Grounded = motor.GroundingStatus.IsStableOnGround;
        state.Velocity = motor.Velocity;

        //Update the last state to store the character state at the beginning of this character updates. 
        lastState = tempState;
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
    public CharacterState GetState() => state;
    public CharacterState GetLastState() => lastState;
    
}
