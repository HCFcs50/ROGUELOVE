using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    [SerializeField]
    GameObject saveSlots;

    [SerializeField]
    private GameObject pauseMenu;

    void Start() {
        //saveSlots = GameObject.FindGameObjectWithTag("SaveSlotMenu");
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape) && pauseMenu != null) {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu() {
        // Unpause
        if (GameStateManager.getState() == GameStateManager.GAMESTATE.PAUSED) {
            GameStateManager.TogglePause();
            GameStateManager.setState(GameStateManager.GAMESTATE.PLAYING);
            pauseMenu.SetActive(false);
        } 
        // Pause
        else {
            GameStateManager.TogglePause();
            GameStateManager.setState(GameStateManager.GAMESTATE.PAUSED);
            pauseMenu.SetActive(true);
        }
    }

    public void LoadMainMenu() {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameStateManager.sceneList.loadScene(0));
    }

    public void PlayButton() {
        saveSlots.SetActive(true);
    }

    public void SaveSlotButton() {
        SceneManager.LoadScene("EmptyLevel");
    }

    public void QuitButton() {
        Debug.Log("QUIT!");
        Application.Quit();
    }
}
