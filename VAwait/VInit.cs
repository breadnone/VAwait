using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Diagnostics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VAwait
{
    public static class VInit
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Start()
        {
            Wait.playMode = UPlayStateMode.PlayMode;
            Wait.StartAwait();
        }
    }
}

#if UNITY_EDITOR
namespace VAwait.Editor
{
    public class EditorInitVAwait
    {
        // ensure class initializer is called whenever scripts recompile
        [InitializeOnLoadAttribute]
        public static class PlayModeStateChangedExample
        {
            // register an event handler when the class is initialized
            static PlayModeStateChangedExample()
            {
                EditorApplication.playModeStateChanged += LogPlayModeState;

/*
                if(PlayerLoopUpdate.playerLoopUtil == null)
                {
                    PlayerLoopUpdate.playerLoopUtil = new PlayerLoopUpdate();
                }
                
                EditorApplication.update += PlayerLoopUpdate.playerLoopUtil.EditModeRunner;
                */
            }

            private static void LogPlayModeState(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    //EditorDeltaTM.StartStopEditorTime(false);
                }
                else if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    Wait.DestroyAwaits();
                    //EditorDeltaTM.StartStopEditorTime(true);
                    Wait.playMode = UPlayStateMode.None;
                }
            }
        }
    }
}

#endif