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
    
    Vector2 playerStartPos;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (!Instance) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        playerStartPos = Player.Instance.transform.position;
    }

    private void OnValidate()
    {
        Time.timeScale = EditorTimeScale;
    }

    private void Update()
    {
        //DEBUG STUFF!!!! TODO:REMOVE FOR FINAL BUILD!!!!
        if (Input.GetKeyDown(KeyCode.R)) Player.Instance.transform.position = playerStartPos;
        if (Input.GetKeyDown(KeyCode.O)) Player.Instance.GetComponent<HealthComponent>().TakeDamage(5f);
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
