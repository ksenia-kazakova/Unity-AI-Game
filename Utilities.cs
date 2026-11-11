using UnityEngine;
using UnityEngine.SceneManagement;

public static class Utilities
{
    public static void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}