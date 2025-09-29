using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuNavigation : MonoBehaviour
{
    //Used for user interfaces that will need changes to be applied
    public GameObject inventoryMenuUI;
    public GameObject journalMenuUI;

    //Warning
    [Header("Warning")]
    public GameObject Warning;

    //[SerializeField] private bool isPaused;
    public static bool isPaused = false;
    public static bool isInventory = false;
    public static bool isJournal = false;

    void Start()
    {
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            isInventory = true;

            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.J))
        {
            isJournal = true;

            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }

        }
    }

    public void Resume()
    {
        if (isInventory)
        {
            inventoryMenuUI.SetActive(false);
        }
        
        if (isJournal)
        {
           journalMenuUI.SetActive(false);
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        if (isInventory)
        {
            inventoryMenuUI.SetActive(true);
        }

        if (isJournal)
        {
            journalMenuUI.SetActive(true);
        }

        Time.timeScale = 0f;
        AudioListener.pause = true;
        isPaused = true;

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public void TurnOnForOneSecond()
    {
        StartCoroutine(TurnOnTemporarily());
    }

    private IEnumerator TurnOnTemporarily()
    {
        Warning.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);
        Warning.SetActive(false);
    }
}
