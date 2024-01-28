using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (!Instance) Instance = this;
        else { Destroy(gameObject); return; }
    }
}
