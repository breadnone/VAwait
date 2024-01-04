using System.Collections;
using UnityEngine;
using System.Threading;

namespace VAwait
{
    public class VWaitComponent : MonoBehaviour
    {
        static VWaitComponent mono { get; set; }
        void OnEnable()
        {
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

        IEnumerator InstanceCoroutineSeconds(float duration, SignalAwaiter signal, CancellationTokenSource cts)
        {
            yield return new WaitForSeconds(duration);

            if (!cts.IsCancellationRequested)
            {
                signal.TrySetResult(true);
            }

            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutineFrame(SignalAwaiter signal, CancellationTokenSource cts)
        {
            yield return null;

            if (!cts.IsCancellationRequested)
            {
                signal.TrySetResult(true);    
            }

            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutine(IEnumerator coroutine, SignalAwaiter signal, CancellationTokenSource cts)
        {
            yield return coroutine;

            if (!cts.IsCancellationRequested)
            {
                signal.TrySetResult(true);    
            }

            Wait.ReturnAwaiterToPool(signal);

        }
        public void TriggerFrameCoroutine(SignalAwaiter signal, CancellationTokenSource cts)
        {
            StartCoroutine(InstanceCoroutineFrame(signal, cts));
        }
        public void TriggerSecondsCoroutine(ref float duration, SignalAwaiter signal, CancellationTokenSource cts)
        {
            StartCoroutine(InstanceCoroutineSeconds(duration, signal, cts));
        }
        public void TriggerCoroutine(IEnumerator coroutine, SignalAwaiter signal, CancellationTokenSource cts)
        {
            StartCoroutine(InstanceCoroutine(coroutine, signal, cts));
        }
        public void CancelCoroutines()
        {
            StopAllCoroutines();
        }
        public void CancelCoroutine(IEnumerator ienumerator)
        {
            StopCoroutine(ienumerator);
        }
        void OnApplicationQuit()
        {
            Wait.DestroyAwaits();
        }
    }
}