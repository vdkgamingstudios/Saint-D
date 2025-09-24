using System.Collections.Generic;
using UnityEngine;

public struct CameraInput
{
    public Vector2 Look;
}
public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float sensitivity = 1.0f;
    
    private Vector3 eulerAngles;
    
    public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.eulerAngles = eulerAngles = target.eulerAngles; //original - transform.rotation = target.rotation;
    }

    public void UpdateRotation(CameraInput input)
    {
        eulerAngles += new Vector3(-input.Look.y, input.Look.x) * sensitivity;
        transform.eulerAngles = eulerAngles;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }
}
