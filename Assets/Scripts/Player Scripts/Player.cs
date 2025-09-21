using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    
    private PlayerInput inputActions;
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        inputActions = new PlayerInput();
        inputActions.Enable();
        
        playerCharacter.Initialize();
        playerCamera.Initialize(playerCharacter.GetCameraTarget());
    }

    private void OnDestroy()
    {
        inputActions.Dispose();
    }


    // Update is called once per frame
    void Update()
    {
        var input = inputActions.Gameplay;

        //Get cam input & update its rotation
        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(cameraInput);

        //Get character input & update it
        var characterInput = new CharacterInput 
        { 
            Rotation = playerCamera.transform.rotation, 
            Movement = input.Movement.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame()
        };
        playerCharacter.UpdateInput(characterInput);
    }

    private void LateUpdate()
    {
        playerCamera.UpdatePosition(playerCharacter.GetCameraTarget());
    }
}
