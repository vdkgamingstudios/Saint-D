using System.Collections.Generic;
using TMPro;
using UnistrokeGestureRecognition; // From the unistroke asset pack
using UnistrokeGestureRecognition.Example;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

public class RuneRecognizer : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject canvasParent;          // The panel shown/hidden with Q
    public TextMeshProUGUI drawingIndicator; // Displays recognition result
    public GameObject promptText;            // "Press Q to draw" etc.

    [Header("Recognizer Settings")]
    [SerializeField] private List<ExampleGesturePattern> patterns; // Assign in Inspector
    [SerializeField, Range(0.6f, 1f)] private float minimumScore = 0.8f;
    [SerializeField] private PathDrawerBase pathDrawer; // Assign Unistroke path drawer prefab

    [Header("Optional Controllers")]
    [SerializeField] private NameController nameController;        // Standard Unity UI text
    [SerializeField] private TMPNameController tmpNameController;  // TextMeshPro version

    private GestureRecorder gestureRecorder;
    private GestureRecognizer<ExampleGesturePattern> recognizer;
    private JobHandle? recognizeJob;

    // State
    private bool isActive = false;
    public static bool isDrawingMode = false;

    [Header("Drawing Settings")]
    [SerializeField] private float drawDistance = 2f; // distance in front of eyes
    [SerializeField] private float drawScale = 1.5f;  // controls size of rune

    void Start()
    {
        // Hide canvas at start
        canvasParent.SetActive(false);

        // Initialize Unistroke recorder + recognizer
        gestureRecorder = new GestureRecorder(256, 0.1f);
        //gestureRecorder = new GestureRecorder(512, 0.02f);
        recognizer = new GestureRecognizer<ExampleGesturePattern>(patterns, 128);

        pathDrawer.Clear();
    }

    void Update()
    {
        // Toggle rune canvas with Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            isActive = !isActive;
            canvasParent.SetActive(isActive);
            promptText.SetActive(false);
            isDrawingMode = isActive;

            if (isActive)
                Clear();
        }

        if (!isActive) return;

        // Start new stroke
        if (Input.GetMouseButtonDown(0))
        {
            Clear();
        }

        // Record points while holding mouse
        if (Input.GetMouseButton(0))
        {
            gestureRecorder.RecordPoint(Input.mousePosition);

            // Get normalized mouse position (-1..1 range)
            float normX = (Input.mousePosition.x / Screen.width - 0.5f) * 2f;
            float normY = (Input.mousePosition.y / Screen.height - 0.5f) * 2f;

            // Map normalized mouse to drawing plane in front of camera
            Vector3 localPos = new Vector3(normX * drawScale, normY * drawScale, drawDistance);

            // Convert to world space so line renderer stays in front of the camera
            Vector3 worldPos = Camera.main.transform.TransformPoint(localPos);

            pathDrawer.AddPoint(worldPos);
        }

        // Finish stroke
        if (Input.GetMouseButtonUp(0))
        {
            if (gestureRecorder.Length > 30)
                RecognizeRecordedGesture();
        }
    }

    private void LateUpdate()
    {
        if (!recognizeJob.HasValue) return;

        recognizeJob.Value.Complete();
        var result = recognizer.Result;

        Debug.Log($"{result.Pattern.Name}: {result.Score}");

        if (result.Score >= minimumScore)
        {
            //drawingIndicator.text = result.Pattern.Name;
            //tmpNameController.Set(result.Pattern.Name);
            ShowName(result.Pattern.Name);
            TriggerSpell(result.Pattern.Name);
        }
        else
        {
            ShowName("Unknown rune");
        }

        recognizeJob = null;
    }

    private void RecognizeRecordedGesture()
    {
        recognizeJob = recognizer.ScheduleRecognition(gestureRecorder.Path);
    }

    private void Clear()
    {
        if (nameController != null) nameController.Clear();
        if (tmpNameController != null) tmpNameController.Clear();

        drawingIndicator.text = "";
        gestureRecorder.Reset();
        pathDrawer.Clear();
    }

    private void ShowName(string name)
    {
        if (nameController != null) nameController.Set(name);
        if (tmpNameController != null) tmpNameController.Set(name);

        // fallback if neither controller is assigned
        if (drawingIndicator != null)
            drawingIndicator.text = name;
    }

    private void TriggerSpell(string gestureName)
    {
        switch (gestureName)
        {
            case "Line":
                CastFireball();
                break;
            case "Circle":
                CastPush();
                break;
            case "Triangle":
                CastTeleport();
                break;
            default:
                Debug.Log("No spell bound to " + gestureName);
                break;
        }
    }

    void CastFireball() => Debug.Log("Fireball spell cast!");
    void CastPush() => Debug.Log("Push spell cast!");
    void CastTeleport() => Debug.Log("Teleport spell cast!");

    private void OnDestroy()
    {
        recognizer.Dispose();
        gestureRecorder.Dispose();
    }
}
