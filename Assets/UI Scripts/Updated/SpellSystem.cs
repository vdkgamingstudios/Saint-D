using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using PDollarGestureRecognizer;

public class SpellSystem : MonoBehaviour
{
    [Header("Setup")]
    public GameObject drawCanvas; // UI Panel to show/hide
    public Transform linePrefab; // prefab with LineRenderer

    private List<Gesture> trainingSet = new List<Gesture>();
    private List<Point> points = new List<Point>();
    private List<LineRenderer> gestureLines = new List<LineRenderer>();

    private int strokeId = -1;
    private LineRenderer currentLine;
    private int vertexCount = 0;

    private bool isDrawing = false;
    private bool addMode = false; // toggle with J
    private string newGestureName = "";

    void Start()
    {
        // Load default gestures from Resources
        TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/10-stylus-MEDIUM/");
        foreach (TextAsset gestureXml in gesturesXml)
            trainingSet.Add(GestureIO.ReadGestureFromXML(gestureXml.text));

        // Load user gestures from persistentDataPath
        string[] filePaths = Directory.GetFiles(Application.persistentDataPath, "*.xml");
        foreach (string filePath in filePaths)
            trainingSet.Add(GestureIO.ReadGestureFromFile(filePath));

        Debug.Log($"Loaded {trainingSet.Count} gestures");
        drawCanvas.SetActive(false); // start hidden
    }

    void Update()
    {
        // Toggle draw mode with Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            isDrawing = !isDrawing;
            drawCanvas.SetActive(isDrawing);

            if (!isDrawing) ClearDrawing(); // reset when closing
        }

        // Toggle add gesture mode with J
        if (Input.GetKeyDown(KeyCode.J))
        {
            addMode = !addMode;
            Debug.Log("Add mode: " + (addMode ? "ON" : "OFF"));
        }

        if (!isDrawing) return;

        Vector3 mousePos = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            strokeId++;
            CreateNewLine();
        }

        if (Input.GetMouseButton(0))
        {
            points.Add(new Point(mousePos.x, -mousePos.y, strokeId));
            currentLine.positionCount = ++vertexCount;
            currentLine.SetPosition(vertexCount - 1, Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10)));
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Right after finishing, classify
            if (!addMode && points.Count > 0)
            {
                RecognizeGesture();
                ClearDrawing();
            }
            else if (addMode && points.Count > 0)
            {
                SaveGesture("NewSpell"); // temporary name
                ClearDrawing();
            }
        }
    }

    void CreateNewLine()
    {
        Transform lineObj = Instantiate(linePrefab, transform);
        currentLine = lineObj.GetComponent<LineRenderer>();
        currentLine.positionCount = 0;
        vertexCount = 0;
        gestureLines.Add(currentLine);
    }

    void ClearDrawing()
    {
        foreach (var line in gestureLines) Destroy(line.gameObject);
        gestureLines.Clear();
        points.Clear();
        strokeId = -1;
    }

    void RecognizeGesture()
    {
        Gesture candidate = new Gesture(points.ToArray());
        Result result = PointCloudRecognizer.Classify(candidate, trainingSet.ToArray());

        Debug.Log($"Recognized: {result.GestureClass} ({result.Score})");

        // Cast spells based on name
        if (result.GestureClass == "Fireball") CastFireball();
        if (result.GestureClass == "Shield") CastShield();
    }

    void SaveGesture(string gestureName)
    {
        string fileName = $"{Application.persistentDataPath}/{gestureName}-{DateTime.Now.Ticks}.xml";
        GestureIO.WriteGesture(points.ToArray(), gestureName, fileName);
        trainingSet.Add(new Gesture(points.ToArray(), gestureName));

        Debug.Log($"Saved new gesture {gestureName} at {fileName}");
    }

    // Example spell methods
    void CastFireball() => Debug.Log("Fireball spell cast!");
    void CastShield() => Debug.Log("Shield spell cast!");
}
