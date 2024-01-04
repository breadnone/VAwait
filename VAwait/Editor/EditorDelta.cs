using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using UnityEditor;
using System.Runtime.CompilerServices;

namespace EditorUtilV
{
    ///This is NOT ACCURATE! Just rough estimation and will not run all the time.
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