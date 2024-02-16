using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Required within each scene for pathfinding, spawn locations etc. 
/// </summary>

[RequireComponent(typeof(AstarPath))]
public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance;


    Vector2 playerStartPos;

    void Awake()
    {
        //Ensure only 1 scene manager exisits
        if (!Instance) Instance = this;
        else Destroy(this);
    }

    void Start()
    {
        playerStartPos = Player.Instance.transform.position;
    }

    public void RestartScene()
    {
        //TODO: Implement
        Player.Instance.transform.position = playerStartPos;
    }
}
