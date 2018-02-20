using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class ThreadUtilsEditor {
    static ThreadUtilsEditor()
    {
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += PlayModeStateChanged;
    }

    #region - Callbacks -
    private static void PlayModeStateChanged(PlayModeStateChange change)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
        {

        }
    }

    public static void OnApplicationQuit()
    {

    }

    static void Update()
    {
        if (Application.isPlaying) return;

        ThreadUtils.JobScheduler.Instance.Progress();
    }
    #endregion - Callbacks -
}
