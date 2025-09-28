using System.Collections;
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
    public TextMeshProUGUI popupText;        // Popup feedback text

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
    public bool isActive = false;
    public static bool isDrawingMode = false;

    [Header("Drawing Settings")]
    [SerializeField] private float drawDistance = 2f; // distance in front of eyes
    [SerializeField] private float drawScale = 1.5f;  // controls size of rune

    void Start()
    {
        // Hide canvas at start
        canvasParent.SetActive(false);
        if (popupText != null) popupText.gameObject.SetActive(false);

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
            if (!isActive)
                EnterDrawingMode();
            else
                ExitDrawingMode();
        }

        if (!isActive) return;

        // Start new stroke
        if (Input.GetMouseButtonDown(0))
        {
            Clear();

            // Freeze player/game when drawing starts
            Time.timeScale = 0f;
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
            else
            {
                // Not enough points to just exit
                ExitDrawingMode();
            }
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
            ShowPopup("Unknown Rune!");
            ExitDrawingMode();
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

        if (drawingIndicator != null) drawingIndicator.text = "";
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
                CastSpell("Fireball");
                break;
            case "Circle":
                CastSpell("Push");
                break;
            case "Triangle":
                CastSpell("Teleport");
                break;
            default:
                Debug.Log("No spell bound to " + gestureName);
                break;
        }
    }

    private void CastSpell(string spellName)
    {
        Debug.Log($"{spellName} spell cast!");
        ShowPopup($"{spellName} Cast!");
        ExitDrawingMode();
    }

    private void ShowPopup(string message)
    {
        if (popupText == null) return;

        popupText.text = message;
        popupText.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HidePopupAfterSeconds(2f));
    }

    private IEnumerator HidePopupAfterSeconds(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        popupText.gameObject.SetActive(false);
    }

    void CastFireball() => Debug.Log("Fireball spell cast!");
    void CastPush() => Debug.Log("Push spell cast!");
    void CastTeleport() => Debug.Log("Teleport spell cast!");

    private void EnterDrawingMode()
    {
        isActive = true;
        isDrawingMode = true;
        canvasParent.SetActive(true);
        promptText.SetActive(false);

        // Freeze game
        Time.timeScale = 0f;

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Clear();
    }

    private void ExitDrawingMode()
    {
        isActive = false;
        isDrawingMode = false;
        canvasParent.SetActive(false);

        // Resume game
        Time.timeScale = 1f;

        // Lock cursor again
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        recognizer.Dispose();
        gestureRecorder.Dispose();
    }
}
