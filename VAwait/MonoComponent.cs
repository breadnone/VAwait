using System.Collections;
using UnityEngine;
using System.Threading;
using System;
using System.Collections.Generic;

namespace VAwait
{
    [AddComponentMenu("")]
    public class VWaitComponent : MonoBehaviour
    {
        static VWaitComponent mono { get; set; }
        WaitForFixedUpdate waitFixed;
        WaitForEndOfFrame endOfFrame;
        void Awake()
        {
            waitFixed = new WaitForFixedUpdate();
            endOfFrame = new WaitForEndOfFrame();
            DontDestroyOnLoad(this.gameObject);
        }

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
        IEnumerator InstanceSeconds(SignalAwaiter signal, WaitForSeconds wait)
        {
            yield return wait;
            signal.TrySetResult(true);
            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceEndFrame(SignalAwaiter signal)
        {
            yield return endOfFrame;
            signal.TrySetResult(true);
            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutineFrame(SignalAwaiter signal)
        {
            yield return null;
            signal.TrySetResult(true);
            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutineFrameReusable(SignalAwaiterReusable signal)
        {
            yield return null;
            signal.TrySetResult(true);
        }
        IEnumerator InstanceCoroutineFixedUpdate(SignalAwaiter signal)
        {
            yield return waitFixed;
            signal.TrySetResult(true);
            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutine(IEnumerator coroutine, SignalAwaiter signal)
        {
            yield return coroutine;
            signal.TrySetResult(true);    
            Wait.ReturnAwaiterToPool(signal);
        }
        public void TriggerFixedUpdateCoroutine(SignalAwaiter signal)
        {
            StartCoroutine(InstanceCoroutineFixedUpdate(signal));
        }
        public void TriggerFrameCoroutine(SignalAwaiter signal)
        {
            StartCoroutine(InstanceCoroutineFrame(signal));
        }
        public void TriggerSecondsCoroutine(SignalAwaiter signal, float duration)
        {
            StartCoroutine(InstanceSeconds(signal, new WaitForSeconds(duration)));
        }
        public void TriggerEndFrame(SignalAwaiter signal)
        {
            StartCoroutine(InstanceEndFrame(signal));
        }
        public void TriggerFrameCoroutineReusable(SignalAwaiterReusable signal)
        {
            StartCoroutine(InstanceCoroutineFrameReusable(signal));
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
            CancelCoroutines();
        }
    }
}