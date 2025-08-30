using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PDollarGestureRecognizer; // from the Asset Store package
using TMPro;
using System.IO;

public class RuneCreation : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject canvasParent;   // The parent object for your RawImage (toggle this)
    public RawImage drawCanvas;
    public TextMeshProUGUI drawingIndicator;
    public GameObject promptText;

    [Header("Drawing Settings")]
    public int textureSize = 512;
    public Color drawColor = Color.black;
    public int brushSize = 4;

    private Texture2D texture;
    private List<Point> currentStroke = new List<Point>();
    private List<Gesture> templates = new List<Gesture>();
    private int strokeId = 0;

    //Conditions
    private bool isActive = false;
    public static bool isDrawingMode = false;

    void Start()
    {
        // Make sure canvas starts hidden
        canvasParent.SetActive(false);

        // Create texture
        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        ClearTexture();
        drawCanvas.texture = texture;

        // Load all gestures from the StreamingAssets/PDollar/ directory
        string[] gestureFiles = System.IO.Directory.GetFiles(Application.streamingAssetsPath, "*.xml");
        foreach (string file in gestureFiles)
        {
            templates.Add(GestureIO.ReadGestureFromFile(file));
        }

        //Debug.Log("Loaded " + templates.Count + " gesture templates.");
    }

    void Update()
    {
        // Toggle canvas with Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            isActive = !isActive;
            canvasParent.SetActive(isActive);
            promptText.SetActive(false);
            isDrawingMode = isActive;  // Freeze player when active condition

            if (isActive)
                ClearTexture();
        }

        if (!isActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            currentStroke.Clear();
            strokeId = 0;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                drawCanvas.rectTransform,
                Input.mousePosition,
                null,
                out localPos))
            {
                Vector2 pivoted = localPos + drawCanvas.rectTransform.sizeDelta / 2f;
                int x = Mathf.RoundToInt(pivoted.x / drawCanvas.rectTransform.sizeDelta.x * textureSize);
                int y = Mathf.RoundToInt(pivoted.y / drawCanvas.rectTransform.sizeDelta.y * textureSize);

                DrawCircle(x, y);
                currentStroke.Add(new Point(x, y, strokeId));
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            RecognizeRune();

            // Hide canvas after recognition
            //isActive = false;
            //canvasParent.SetActive(false);
            ClearTexture();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearTexture();
            currentStroke.Clear();
        }

        //To show drawing is happening for a rune
        //if (drawingIndicator != null)
        //{
        //    drawingIndicator.gameObject.SetActive(isActive);
        //}
    }

    void DrawCircle(int cx, int cy)
    {
        for (int x = -brushSize; x <= brushSize; x++)
        {
            for (int y = -brushSize; y <= brushSize; y++)
            {
                if (x * x + y * y <= brushSize * brushSize)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                    {
                        texture.SetPixel(px, py, drawColor);
                    }
                }
            }
        }
        texture.Apply();
    }

    void ClearTexture()
    {
        Color[] fill = new Color[textureSize * textureSize];
        for (int i = 0; i < fill.Length; i++) fill[i] = Color.white;
        texture.SetPixels(fill);
        texture.Apply();
    }

    void RecognizeRune()
    {
        if (currentStroke.Count == 0) return;

        Gesture g = new Gesture(currentStroke.ToArray());
        Result r = PointCloudRecognizer.Classify(g, templates.ToArray());

        //Debug.Log("Recognized Rune: " + r.GestureClass + " (score: " + r.Score + ")");
        //drawingIndicator.text = "Recognized Rune: " + r.GestureClass + " (score: " + r.Score + ")";
        //Debug.Log("UI Updated -> " + drawingIndicator.text);

        switch (r.GestureClass)
        {
            case "Line":
                drawingIndicator.text = "Line";
                CastFireball();
                break;
            case "Circle":
                drawingIndicator.text = "Circle";
                CastPush();
                break;
            case "Triangle":
                drawingIndicator.text = "Triangle";
                CastTeleport();
                break;
            default:
                drawingIndicator.text = "Unknown rune";
                Debug.Log("Unknown rune.");
                break;
        }

        //drawingIndicator.text = "Recognized Rune: " + r.GestureClass + " (score: " + r.Score + ")";
    }

    void CastFireball() => Debug.Log("Fireball spell cast!");
    void CastPush() => Debug.Log("Push (wind) spell cast!");
    void CastTeleport() => Debug.Log("Teleport spell cast!");
}
