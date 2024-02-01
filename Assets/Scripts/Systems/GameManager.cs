using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
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
}
