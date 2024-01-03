using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace VAwait
{
    public class VWaitComponent : MonoBehaviour
    {
        public static bool workerIsRunning { get; private set; }
        static VWaitComponent mono { get; set; }
        static ConcurrentQueue<(long frameIn, Action func)> queue;
        static List<Action> backup;
        void OnEnable()
        {
            queue = new();
            DontDestroyOnLoad(this.gameObject);
        }
        // Start is called before the first frame update
        void Start()
        {
            if (mono == null)
            {
                mono = this;
            }
            else
            {
                GameObject.Destroy(this);
            }
        }

        IEnumerator InstanceCoroutineSeconds(float duration, SignalAwaiter<bool> signal, CancellationTokenSource cts)
        {
            yield return new WaitForSeconds(duration);

            if (!cts.IsCancellationRequested)
            {
                signal.TrySetResult(true);
            }

            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutineFrame(SignalAwaiter<bool> signal, CancellationTokenSource cts)
        {
            yield return null;

            if (!cts.IsCancellationRequested)
            {
                signal.TrySetResult(true);    
            }

            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutine(IEnumerator coroutine, SignalAwaiter<bool> signal, CancellationTokenSource cts)
        {
            yield return coroutine;

            if (!cts.IsCancellationRequested)
            {
                signal.TrySetResult(true);    
            }

            Wait.ReturnAwaiterToPool(signal);

        }
        public void TriggerFrameCoroutine(SignalAwaiter<bool> signal, CancellationTokenSource cts)
        {
            StartCoroutine(InstanceCoroutineFrame(signal, cts));
        }
        public void TriggerSecondsCoroutine(ref float duration, SignalAwaiter<bool> signal, CancellationTokenSource cts)
        {
            StartCoroutine(InstanceCoroutineSeconds(duration, signal, cts));
        }
        public void TriggerCoroutine(IEnumerator coroutine, SignalAwaiter<bool> signal, CancellationTokenSource cts)
        {
            StartCoroutine(InstanceCoroutine(coroutine, signal, cts));
        }
        public void CancelCoroutines()
        {
            StopAllCoroutines();
        }
        void OnApplicationQuit()
        {
            Wait.DestroyAwaits();
        }
    }
}