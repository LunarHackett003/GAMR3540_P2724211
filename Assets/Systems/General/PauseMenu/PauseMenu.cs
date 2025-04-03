using UnityEngine;

public class PauseMenu : LunarScript
{
    public static PauseMenu Instance { get; private set; }

    public static bool GamePaused;
    public GameObject menuCanvas;


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetPaused(false);
    }
    public void TogglePause()
    {
        GamePaused = !GamePaused;
        UpdatePause();
    }
    public void SetPaused(bool value)
    {
        GamePaused = value;
        UpdatePause();
    }
    void UpdatePause()
    {
        Cursor.lockState = !GamePaused ? CursorLockMode.Locked : CursorLockMode.None;
        menuCanvas.SetActive(GamePaused);
    }

}
