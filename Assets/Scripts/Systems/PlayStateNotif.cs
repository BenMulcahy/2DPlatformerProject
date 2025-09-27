using UnityEngine;
using UnityEditor;


#if UNITY_EDITOR

[InitializeOnLoad]
public static class PlayStateNotif
{

    static PlayStateNotif()
    {
        EditorApplication.playModeStateChanged += playModeChanged;
        
    }

    static void playModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode)
        {
            InputManager.Instance.StopRumble();
        }
    }
}

#endif