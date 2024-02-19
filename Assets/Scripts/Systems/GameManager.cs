using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Delegates/Events
    public delegate void OnGamePause();
    public static event OnGamePause onGamePause;
    #endregion

    public static GameManager Instance { get; private set; }
    [SerializeField][Range(0,5)] float EditorTimeScale = 1f;
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (!Instance) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void OnValidate()
    {
        Time.timeScale = EditorTimeScale;
    }

    private void Update()
    {
        /* DEBUGGING */
    #if (UNITY_EDITOR)
        if (Input.GetKeyDown(KeyCode.R)) SceneManager.Instance.RestartScene();
        if (Input.GetKeyDown(KeyCode.O)) Player.Instance.GetComponent<HealthComponent>().TakeDamage(5f);
        Player.Instance.GetComponent<HealthComponent>().bCanTakeDamage = false;
    #endif
    }


    public void PauseGame()
    {
        onGamePause?.Invoke();
        Debug.Log("Pause!");
    }

    public void QuitGame()
    {
        Debug.LogWarning("Game Quit Called from Game Manager");
        Application.Quit();
    }

}
