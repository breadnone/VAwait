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

    public class EditorDeltaTM
    {
        public static CancellationTokenSource EditorTimeToken;
        private static Stopwatch timeProp;
        public static bool EditorWorkerIsRunning { get; private set; }
        private static float time;
        public static float Time
        {
            get
            {
                lastRequested = EditorApplication.timeSinceStartup;
                
                if(!EditorWorkerIsRunning)
                {
                    StartStopEditorTime(true);
                }

                return time;
            }
        }

        private static double oneFrame;
        
        private static double lastRequested;
        
        private static async Task EditorTimeWorker()
        {
            lastRequested = EditorApplication.timeSinceStartup;
            EditorTimeToken = new CancellationTokenSource();
            EditorWorkerIsRunning = true;
            
            if(timeProp == null)
            {
                timeProp = new Stopwatch();
            }

            while (EditorWorkerIsRunning)
            {
                if (EditorTimeToken.IsCancellationRequested)
                {
                    timeProp.Stop();
                    break;
                }

                timeProp.Restart();
                await Task.Delay(TimeSpan.FromMilliseconds(oneFrame), EditorTimeToken.Token);
                time = (float)timeProp.Elapsed.TotalSeconds;
                 
                if((lastRequested + 30) < EditorApplication.timeSinceStartup)
                {
                    break;
                }
            }

            StartStopEditorTime(false);
        }
        public static async void StartStopEditorTime(bool state)
        {
            if (state)
            {
                var refValue = Screen.currentResolution.refreshRateRatio.value;
                oneFrame = (1d / refValue) * 1000;

                if (EditorWorkerIsRunning)
                    return;

                await EditorTimeWorker();
            }
            else
            {
                if (EditorWorkerIsRunning)
                {
                    EditorWorkerIsRunning = false;
                }

                if (EditorTimeToken != null)
                {
                    EditorTimeToken.Cancel();
                    EditorTimeToken.Dispose();
                    EditorTimeToken = null;
                }

                lastRequested = 0;
            }
        }
    }
}

#endif