using UnityEngine;

public class GameBehavior : MonoBehaviour
{
    public bool showWinScreen = false;
    public bool showLossScreen = false;

    public string labelText = "";

    void Start()
    {
        Time.timeScale = 1f;
    }

    public void WinGame()
    {
        showWinScreen = true;
        labelText = "You won.";

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoseGame()
    {
        showLossScreen = true;
        labelText = "You were caught.";

        Time.timeScale = 0f;

        // Показываем и разблокируем курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        if (showWinScreen)
        {
            GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 100, 300, 200), labelText);

            if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2, 200, 50), labelText))
            {
                Utilities.RestartLevel();
            }
        }

        if (showLossScreen)
        {
            GUI.Box(
                new Rect(Screen.width / 2 - 150, Screen.height / 2 - 100, 300, 200), labelText);

            if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2, 200, 50), labelText))
            {
                Utilities.RestartLevel();
            }
        }
    }
}