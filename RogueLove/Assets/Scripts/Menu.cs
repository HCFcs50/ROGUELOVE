using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    [SerializeField]
    GameObject saveSlots;

    [SerializeField]
    private GameObject pauseMenu;

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape) && pauseMenu != null) {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu() {
        // Unpause
        if (GameStateManager.GetState() == GameStateManager.GAMESTATE.PAUSED) {
            GameStateManager.TogglePause();
            GameStateManager.SetState(GameStateManager.GAMESTATE.PLAYING);
            pauseMenu.SetActive(false);
        } 
        // Pause
        else if (GameStateManager.GetState() == GameStateManager.GAMESTATE.PLAYING) {
            GameStateManager.TogglePause();
            GameStateManager.SetState(GameStateManager.GAMESTATE.PAUSED);
            pauseMenu.SetActive(true);
        }
    }

    public void LoadMainMenu() {
        TransitionManager.StartLeaf(0);
    }

    public void PlayButton() {
        //Debug.Log("current level: " + GameStateManager.GetLevel());
        //Debug.Log("current stage: " + GameStateManager.GetStage());

        String pathMap = Application.persistentDataPath + "/map.chris";
        String pathPlayer = Application.persistentDataPath + "/player.franny";

        // Set up SAVED GAME
        if (File.Exists(pathMap) && File.Exists(pathPlayer)) {
            // Load save data to get level number
            MapData data = SaveSystem.LoadMap();

            GameStateManager.SetLevel(data.levelNum);
            GameStateManager.SetStage(data.stageNum);
            saveSlots.SetActive(true);
        } 
        // START NEW GAME
        else {
            GameStateManager.SetSave(false);
            GameStateManager.SetLevel(1);
            GameStateManager.SetStage(1);
            // Load level
            TransitionManager.StartLeaf(1);
        }
    }

    public void SaveSlotButton() {
        GameStateManager.SetSave(true);
        // Load level
        TransitionManager.StartLeaf(GameStateManager.GetStage());
    }

    public void QuitButton() {
        Debug.Log("QUIT!");
        Application.Quit();
    }
}
