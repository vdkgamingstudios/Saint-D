using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    //Main Menu Warning
    [Header("Warning")]
    public GameObject Warning;
    public void PlayGame()
    {
        SceneManager.LoadScene("MagicSystemTest");
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

    public void QuitGame()
    {
        Application.Quit();
    }
}
