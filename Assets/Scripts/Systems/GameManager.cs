using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Delegates/Events
    public delegate void OnGamePause();
    public static event OnGamePause onGamePause;
    #endregion

    public static GameManager Instance { get; private set; }
    [SerializeField][Range(0,5)] float EditorTimeScale = 1f;
    public bool bIsTimeFrozen { get; private set; } = false;
    
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
    #endif
    }

    #region Timescale based hitstop
    public void DoHitstop(float duration)
    {
        //Debug.Log("Freeze");
        StartCoroutine(HitstopFreeze(duration));
    }

    IEnumerator HitstopFreeze(float duration)
    {
        bIsTimeFrozen = true;
        var defTimeScale = Time.timeScale;
        Time.timeScale = 0;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = defTimeScale;
        bIsTimeFrozen = false;
    }
    #endregion

    public void PauseGame()
    {
        onGamePause?.Invoke();
        Debug.Log("Pause!");
    }

    public void QuitGame()
    {
        Debug.LogWarning("Game Quit Called from Game Manager");
        CameraManager.Instance.EndCameraShake();
        Application.Quit();
    }
}
