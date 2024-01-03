using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public class EditorInit
    {
        // ensure class initializer is called whenever scripts recompile
        [InitializeOnLoadAttribute]
        public static class PlayModeStateChangedExample
        {
            // register an event handler when the class is initialized
            static PlayModeStateChangedExample()
            {
                EditorApplication.playModeStateChanged += LogPlayModeState;
            }

            private static void LogPlayModeState(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {

                }
                else if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    Wait.DestroyAwaits();
                    Wait.playMode = UPlayStateMode.None;
                }
            }
        }
    }
}

#endif