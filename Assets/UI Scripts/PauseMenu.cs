using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;

    //Main Menu Warning
    [Header("Warning")]
    public GameObject Warning;

    //[SerializeField] private bool isPaused;
    public static bool isPaused = false;

    void Start()
    {
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.S))
        {
            Resume();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        isPaused = true;

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public void ExitButton()
    {
        Application.Quit();
    }

    public void ReturnButton()
    {
        //Return to the main menu
        SceneManager.LoadScene("MainMenu");
    }

    public void GameSettings()
    {
        //Attach to settings button to the game

    }

    public void TurnOnForOneSecond()
    {
        StartCoroutine(TurnOnTemporarily());
    }

    private IEnumerator TurnOnTemporarily()
    {
        Warning.SetActive(true);
        yield return new WaitForSeconds(1f);
        Warning.SetActive(false);
    }
}
