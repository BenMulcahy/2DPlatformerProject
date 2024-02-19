using System;
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
    public List<EnemyBase> EnemiesInScene = new List<EnemyBase>();

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
        PopulateEnemyList();
    }

    private void OnEnable()
    {
        EnemyBase.onEnemyDeath += OnEnemyDying;
    }

    private void OnEnemyDying(EnemyBase enemy)
    {
        Debug.Log(enemy.name + " Died!");
        if (EnemiesInScene.Contains(enemy)) EnemiesInScene.Remove(enemy);
    }

    public void RestartScene()
    {
        //TODO: Implement
        Player.Instance.transform.position = playerStartPos;
    }

    void PopulateEnemyList()
    {
        foreach (EnemyBase enemy in FindObjectsByType(typeof(EnemyBase),FindObjectsSortMode.None))
        {
            EnemiesInScene.Add(enemy);
        }
    }
}
