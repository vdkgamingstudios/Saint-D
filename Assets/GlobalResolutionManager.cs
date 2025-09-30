using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalResolutionManager : MonoBehaviour
{
    public static GlobalResolutionManager Instance;

    [SerializeField] private bool fullscreen = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // persists across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        SetResolution();
    }

    void SetResolution()
    {
        int width = Display.main.systemWidth;
        int height = Display.main.systemHeight;

        Screen.SetResolution(width, height, fullscreen);
        QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);

        Debug.Log($"Forced resolution {width}x{height} @ {QualitySettings.names[QualitySettings.names.Length - 1]} quality");
    }
}
