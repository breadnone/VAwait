using System.Collections;
using UnityEngine;
using System.Threading;
using System;

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

        IEnumerator InstanceCoroutineSeconds(float duration, SignalAwaiter signal)
        {
            yield return new WaitForSeconds(duration);
            signal.TrySetResult(true);
            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutineFrame(SignalAwaiter signal)
        {
            yield return null;
            signal.TrySetResult(true);
            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutine(IEnumerator coroutine, SignalAwaiter signal)
        {
            yield return coroutine;
            signal.TrySetResult(true);    
            Wait.ReturnAwaiterToPool(signal);

        }
        public void TriggerFrameCoroutine(SignalAwaiter signal)
        {
            StartCoroutine(InstanceCoroutineFrame(signal));
        }
        public void TriggerSecondsCoroutine(ref float duration, SignalAwaiter signal)
        {
            StartCoroutine(InstanceCoroutineSeconds(duration, signal));
        }
        public void TriggerCoroutine(IEnumerator coroutine, SignalAwaiter signal)
        {
            StartCoroutine(InstanceCoroutine(coroutine, signal));
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