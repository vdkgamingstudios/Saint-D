using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    [Space]
    [SerializeField] private PostProcessVolume volume;
    [SerializeField] private StanceVignette stanceVignette;
    
    private PlayerInput inputActions;
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        inputActions = new PlayerInput();
        inputActions.Enable();
        
        playerCharacter.Initialize();
        playerCamera.Initialize(playerCharacter.GetCameraTarget());

        cameraSpring.Initialize();
        cameraLean.Initialize();

        stanceVignette.Initialize(volume.profile);
    }

    private void OnDestroy()
    {
        inputActions.Dispose();
    }


    // Update is called once per frame
    void Update()
    {
        //Prevent movement, input, and camera updates while paused
        if (PauseMenu.isPaused) return;
        if (RuneRecognizer.isDrawingMode) return;

        var input = inputActions.Gameplay;
        var deltaTime = Time.deltaTime;

        //Get cam input & update its rotation
        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(cameraInput);

        //Get character input & update it
        var characterInput = new CharacterInput 
        { 
            Rotation = playerCamera.transform.rotation, 
            Movement = input.Movement.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Crouch = input.Crouch.WasPressedThisFrame() ? CrouchInput.Toggle : CrouchInput.None
        };
        playerCharacter.UpdateInput(characterInput);
        playerCharacter.UpdateBody(deltaTime);
    }

    private void LateUpdate()
    {
        //Also skip camera/visual updates while paused
        if (PauseMenu.isPaused) return;
        if (RuneRecognizer.isDrawingMode) return;

        var deltaTime = Time.deltaTime;
        var cameraTarget = playerCharacter.GetCameraTarget();
        var state = playerCharacter.GetState();

        playerCamera.UpdatePosition(cameraTarget);
        cameraSpring.UpdateSpring(deltaTime, cameraTarget.up);
        cameraLean.UpdateLean(deltaTime,state.Stance is Stance.Slide,state.Acceleration,cameraTarget.up);

        stanceVignette.UpdateVignette(deltaTime, state.Stance);
    }
}
