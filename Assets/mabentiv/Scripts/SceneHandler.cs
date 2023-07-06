using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour
{

    [SerializeField] Animator transition;
    [SerializeField] float transitionTime = 1f;

    public void PlayGame()
    {
        LoadNextLevel();
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void EndGame()
    {
        LoadNextLevel();
    }

    private void LoadNextLevel()
    {
        StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex + 1));
    }

    IEnumerator LoadLevel(int levelIndex)
    {
        transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime);

        SceneManager.LoadScene(levelIndex);


    }

}
